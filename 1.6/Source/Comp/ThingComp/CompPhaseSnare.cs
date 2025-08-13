namespace Rhynia.Overpower;

public class CompProperties_PhaseSnareCore : CompProperties
{
    public CompProperties_PhaseSnareCore() => compClass = typeof(CompPhaseSnareCore);
}

public class CompPhaseSnareCore : ThingComp
{
    private int _ticker;
    private int _lastProcessedCount;
    private bool _enabled;

    private GameComponent_PhaseSnare _container = null!;

    public bool IsEnabled => _enabled;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        var component = GameComponent_PhaseSnare.Instance;
        if (component is null)
        {
            Log.Error("PhaseSnareContainer component not found in game. This should not happen.");
            parent.Destroy();
            return;
        }

        Messages.Message(
            "RhyniaOverpower_PhaseSnareCore_Msg_Setup".Translate(),
            MessageTypeDefOf.NeutralEvent
        );

        _container = component;
        _container.SetInstance(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);

        Messages.Message(
            "RhyniaOverpower_PhaseSnareCore_Msg_Stop".Translate(),
            MessageTypeDefOf.NeutralEvent
        );

        _container.RemoveInstance();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _enabled, "enabled", true);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;
        yield return new Command_Toggle
        {
            defaultLabel = TranslationExtension.TranslateAsEnable(true),
            icon = Designator_PhaseSnare.PhaseSnareIcon,
            isActive = () => _enabled,
            toggleAction = () => _enabled = !_enabled,
        };
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

        if (parent.Spawned is false || parent.Map is null)
            return;

        if (_ticker <= 0)
        {
            Action();
            _ticker = 60;
        }
        else
        {
            _ticker--;
        }
    }

    private void Action()
    {
        if (!_enabled)
            return;

        var pawns = _container.PopPawns();

        if (pawns.NullOrEmpty())
        {
            _lastProcessedCount = 0;
            return;
        }

        Debug($"CompPhaseSnareCore has {pawns.Count} pawns to process.", this);

        var processedCount = 0;
        foreach (var pawn in pawns)
        {
            if (pawn.Map is null || !pawn.Spawned)
            {
                pawn.RemoveDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);
                Warn($"Pawn {pawn} is not spawned or has no map. Skipping teleport.", this);
                continue;
            }

            if (pawn.Map != parent.Map)
            {
                pawn.ExitMap(false, Rot4.Invalid);
                pawn.SpawnToThing(parent);
                pawn.AddDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);
            }

            pawn.stances?.stunner.StunFor(300, null, false, false);
            pawn.jobs?.StopAll(false, false);

            pawn.Position = parent.Position;
            pawn.Notify_Teleported();

            if (pawn.Downed || pawn.Dead)
                pawn.RemoveDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);

            processedCount++;
        }

        _lastProcessedCount = processedCount;
        Debug($"Processed {processedCount} pawns in this tick.", this);
    }
}

public class CompProperties_PhaseSnareBeacon : CompProperties
{
    public CompProperties_PhaseSnareBeacon() => compClass = typeof(CompPhaseSnareBeacon);
}

public class CompPhaseSnareBeacon : ThingComp
{
    private int _ticker = 30; // A little bit later than the main comp
    private bool _enabled = true;
    private bool _captureHostile = false;
    private bool _captureFogged = false;

    private GameComponent_PhaseSnare _container = null!;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        var component = GameComponent_PhaseSnare.Instance;
        if (component is null)
        {
            Log.Error("PhaseSnare component not found in game. This should not happen.");
            parent.Destroy();
            return;
        }

        _container = component;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;
        yield return new Command_Toggle
        {
            defaultLabel = TranslationExtension.TranslateAsEnable(true),
            icon = Designator_PhaseSnare.PhaseSnareIcon,
            isActive = () => _enabled,
            toggleAction = () => _enabled = !_enabled,
        };
        if (_enabled)
        {
            yield return new Command_Toggle
            {
                defaultLabel = "RhyniaOverpower_PhaseSnareBeacon_Gizmo_CaptureHostile".Translate(),
                icon = TexCommand.FireAtWill,
                isActive = () => _captureHostile,
                toggleAction = () => _captureHostile = !_captureHostile,
            };
            if (_captureHostile)
                yield return new Command_Toggle
                {
                    defaultLabel =
                        "RhyniaOverpower_PhaseSnareBeacon_Gizmo_CaptureFogged".Translate(),
                    icon = TexCommand.FireAtWill,
                    isActive = () => _captureFogged,
                    toggleAction = () => _captureFogged = !_captureFogged,
                };
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _enabled, "enabled", true);
        Scribe_Values.Look(ref _captureHostile, "captureHostile", false);
        Scribe_Values.Look(ref _captureFogged, "captureFogged", false);
    }

    public override void CompTick()
    {
        base.CompTick();

        if (parent.Spawned is false || parent.Map is null)
            return;

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

        if (!_container.IsValid)
        {
            Warn($"PhaseSnareCore is not valid, disabling this beacon", this);
            this.ThrowMote("RhyniaOverpower_PhaseSnareBeacon_InvalidCore".Translate());
            _enabled = false;
            return;
        }
        else if (!_container.IsEnabled)
            return;

        var pawns = parent.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn);
        if (pawns.NullOrEmpty())
            return;

        var validPawns = pawns
            .OfType<Pawn>()
            .Where(p =>
                p is { Spawned: true, Dead: false, Downed: false }
                && p.Faction?.IsPlayer is not true
            )
            .ToHashSet();

        if (_captureHostile)
            foreach (var p in validPawns)
            {
                var shouldCapture =
                    (
                        p.Faction?.HostileTo(Faction.OfPlayer) is true
                        && p is { IsPrisonerOfColony: false, IsSlaveOfColony: false }
                    ) || p is { IsAnimal: true, InAggroMentalState: true };
                if (shouldCapture)
                {
                    if (!_captureFogged && p.Position.Fogged(p.Map))
                        continue;
                    p.AddDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);
                }
            }

        var processPawns = validPawns.Where(p =>
            p.HasDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation)
        );

        if (processPawns.EnumerableNullOrEmpty())
            return;

        _container.PushPawns(processPawns);
    }
}

public class GameComponent_PhaseSnare : GameComponent
{
    public GameComponent_PhaseSnare(Game game)
    {
        Debug($"Initialized on {game.Info.RealPlayTimeInteracting}.", this);
    }

    private readonly AtomicContainerNullable<CompPhaseSnareCore> _instance = new();
    private readonly AtomicContainer<HashSet<Pawn>> _pendingPawns = new([]);

    public bool IsValid => _instance.Value?.parent is { Spawned: true, Map: not null };
    public bool IsEnabled => _instance.Value?.IsEnabled ?? false;

    public override void LoadedGame()
    {
        base.LoadedGame();
        Debug("Clearing instance for check", this);
        _instance.Value = null;
    }

    public void SetInstance(CompPhaseSnareCore comp)
    {
        if (comp is null)
            throw new ArgumentNullException(nameof(comp));
        if (_instance.Value is not null)
            throw new InvalidOperationException("Instance already set.");
        _instance.Value = comp;
        Debug($"Instance set to {comp}.", this);
    }

    public void RemoveInstance(bool tryDestroy = false)
    {
        Debug("Removing instance.", this);

        if (tryDestroy)
            _instance.Value?.parent.Destroy();

        _instance.Value = null;
    }

    public void PushPawns(IEnumerable<Pawn> pawns)
    {
        if (pawns.EnumerableNullOrEmpty())
            return;
        _pendingPawns.SynchronizedAction((set) => set.UnionWith(pawns));
    }

    public List<Pawn> PopPawns()
    {
        return _pendingPawns.SynchronizedAction(
            (set) =>
            {
                if (set.Count == 0)
                    return [];
                var list = set.ToList();
                set.Clear();
                return list;
            }
        );
    }

    public static GameComponent_PhaseSnare Instance =>
        Current.Game.GetComponent<GameComponent_PhaseSnare>();
}
