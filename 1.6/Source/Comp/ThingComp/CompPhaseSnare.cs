namespace Rhynia.Overpower;

public static class PhaseSnareContainer
{
    private static readonly object _lock = new();
    private static Thing? _instance;
    public static Thing? Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance;
            }
        }
        set
        {
            lock (_lock)
            {
                if (_instance is not null)
                {
                    Out.Warning(
                        "FieldFocusContainer.Instance is being overwritten. Previous instance will be lost."
                    );
                }
                _instance = value;
            }
        }
    }

    public static bool IsValid => Instance is { Spawned: true, Map: not null };

    private static readonly object _setLock = new();
    private static readonly HashSet<Pawn> _pendingPawns = [];

    public static void AddPawn(Pawn pawn)
    {
        lock (_setLock)
        {
            if (pawn is not null)
                _pendingPawns.Add(pawn);
        }
    }

    public static void AddPawns(IEnumerable<Pawn> pawns)
    {
        lock (_setLock)
        {
            foreach (var pawn in pawns)
                if (pawn is not null)
                    _pendingPawns.Add(pawn);
        }
    }

    public static List<Pawn> GetPendingPawns()
    {
        lock (_setLock)
        {
            if (_pendingPawns.Count == 0)
                return [];

            var list = _pendingPawns.ToList();
            _pendingPawns.Clear();
            return list;
        }
    }

    public static int PendingPawnCount
    {
        get
        {
            lock (_setLock)
            {
                return _pendingPawns.Count;
            }
        }
    }
}

public class CompProperties_PhaseSnareCore : CompProperties
{
    public CompProperties_PhaseSnareCore() => compClass = typeof(CompPhaseSnareCore);
}

public class CompPhaseSnareCore : ThingComp
{
    private int _ticker;
    private int _lastProcessedCount;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        PhaseSnareContainer.Instance = parent;
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        PhaseSnareContainer.Instance = null;
    }

    public override string CompInspectStringExtra()
    {
        var sb = new StringBuilder(base.CompInspectStringExtra());
        sb.AppendLineIfNotEmpty();
        sb.AppendLine(
            "RhyniaOverpower_PhaseSnareCore_Inspect_LastProcessed".Translate(_lastProcessedCount)
        );
        return sb.ToString().TrimEnd();
    }

    public override void CompTick()
    {
        base.CompTick();
        if (_ticker <= 0)
        {
            DoTick();
            _ticker = 60;
        }
        else
        {
            --_ticker;
        }
    }

    private void DoTick()
    {
        var pawns = PhaseSnareContainer.GetPendingPawns();

        if (pawns.NullOrEmpty())
        {
            _lastProcessedCount = 0;
            return;
        }

        Out.Debug($"PhaseSnareContainer has {pawns.Count} pawns to process.");

        var processedCount = 0;
        foreach (var pawn in pawns)
        {
            pawn.stances?.stunner.StunFor(300, null, false, false);
            pawn.jobs?.StopAll(false, false);

            if (pawn.Map is null || !pawn.Spawned)
            {
                pawn.RemoveDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);
                Out.Warning($"Pawn {pawn} is not spawned or has no map. Skipping teleport.");
                continue;
            }

            if (pawn.Map != parent.Map)
            {
                pawn.ExitMap(false, Rot4.Invalid);
                pawn.SpawnToThing(parent);
                pawn.AddDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);
            }

            pawn.Position = parent.Position;
            pawn.Notify_Teleported();

            if (pawn.Downed || pawn.Dead)
                pawn.RemoveDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);

            processedCount++;
        }

        _lastProcessedCount = processedCount;
        Out.Debug($"Processed {processedCount} pawns in this tick.");
    }
}

public class CompProperties_PhaseSnareBeacon : CompProperties
{
    public CompProperties_PhaseSnareBeacon() => compClass = typeof(CompPhaseSnareBeacon);
}

public class CompPhaseSnareBeacon : ThingComp
{
    private int _ticker = 30; // A little bit later than the main comp
    private int _removeCheck = 0;
    private bool _enabled = true;
    private bool _captureHostile = false;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;
        yield return new Command_Toggle
        {
            defaultLabel = TranslationExtension.TranslateAsEnable(true),
            icon = TexCommand.DesirePower,
            isActive = () => _enabled,
            toggleAction = () => _enabled = !_enabled,
        };
        yield return new Command_Toggle
        {
            defaultLabel = "RhyniaOverpower_PhaseSnareBeacon_Gizmo_CaptureHostile".Translate(),
            icon = TexCommand.FireAtWill,
            isActive = () => _captureHostile,
            toggleAction = () => _captureHostile = !_captureHostile,
        };
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _enabled, "enabled", true);
        Scribe_Values.Look(ref _captureHostile, "captureHostile", false);
    }

    public override void CompTick()
    {
        base.CompTick();

        if (_ticker <= 0)
        {
            DoTick();
            _ticker = 60;
        }
        else
        {
            _ticker--;
        }
    }

    private void DoTick()
    {
        if (!_enabled)
            return;

        if (!PhaseSnareContainer.IsValid)
        {
            if (_removeCheck > 10)
            {
                parent.Destroy();
                return;
            }
            _removeCheck++;
            Out.Warning(
                $"PhaseSnareContainer is not valid. Waiting for it to become valid or for {10 - _removeCheck} more ticks."
            );
            this.ThrowMote("RhyniaOverpower_PhaseSnareBeacon_InvalidCore".Translate(_removeCheck));
            return;
        }
        else
        {
            _removeCheck = 0;
        }

        var pawns = parent.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn);
        if (pawns.NullOrEmpty())
            return;

        var validPawns = pawns
            .OfType<Pawn>()
            .Where(p =>
                p is { Spawned: true, Dead: false, Downed: false }
                && (p.Faction is null || !p.Faction.IsPlayer)
            )
            .ToHashSet();

        if (_captureHostile)
            foreach (var p in validPawns)
                if (
                    (
                        p?.Faction.HostileTo(Faction.OfPlayer) == true
                        && p is { IsPrisonerOfColony: false, IsSlaveOfColony: false }
                    ) || p is { IsAnimal: true, InAggroMentalState: true }
                )
                    p.AddDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);

        var processPawns = validPawns.Where(p =>
            p.HasDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation)
        );

        if (processPawns.EnumerableNullOrEmpty())
            return;

        PhaseSnareContainer.AddPawns(processPawns);
    }
}
