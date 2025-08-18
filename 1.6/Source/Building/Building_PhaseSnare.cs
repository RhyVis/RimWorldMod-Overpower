namespace Rhynia.Overpower;

public class Building_PhaseSnareCore : Building
{
    private int _ticker;
    private int _lastProcessedCount;
    private bool _enabled = true;

    private GameComponent_PhaseSnare _container = null!;

    public bool IsEnabled => _enabled;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        Debug($"Spawning PhaseSnareCore {def.defName}", this);
        base.SpawnSetup(map, respawningAfterLoad);

        var component = GameComponent_PhaseSnare.Instance;
        if (component is null)
        {
            Log.Error("PhaseSnare component not found in game");
            Destroy();
            return;
        }

        if (!respawningAfterLoad)
            Messages.Message(
                "RhyniaOverpower_PhaseSnareCore_Msg_Setup".Translate(),
                MessageTypeDefOf.NeutralEvent
            );

        _container = component;
        _container.SetInstance(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);

        Messages.Message(
            "RhyniaOverpower_PhaseSnareCore_Msg_Stop".Translate(),
            MessageTypeDefOf.NeutralEvent
        );

        _container.RemoveInstance();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _enabled, "enabled", true);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
        yield return new Command_Toggle
        {
            defaultLabel = TranslationExtension.TranslateAsEnable(true),
            icon = Designator_PhaseSnare.PhaseSnareIcon,
            isActive = () => _enabled,
            toggleAction = () => _enabled = !_enabled,
        };
    }

    public override string GetInspectString()
    {
        var sb = new StringBuilder(base.GetInspectString());
        sb.AppendLineIfNotEmpty();
        sb.AppendLine(
            "RhyniaOverpower_PhaseSnareCore_Inspect_LastProcessed".Translate(_lastProcessedCount)
        );
        return sb.ToString().TrimEnd();
    }

    protected override void Tick()
    {
        base.Tick();

        if (!Spawned || Map is null)
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

        Debug($"PhaseSnareCore has {pawns.Count} pawns to process", this);

        var processedCount = 0;
        foreach (var pawn in pawns)
        {
            if (pawn is { Spawned: false } or { Map: null })
            {
                pawn.RemoveDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);
                Warn($"Pawn {pawn} is not spawned or has no map. Skipping teleport", this);
                continue;
            }

            if (pawn.Map.uniqueID != Map.uniqueID)
            {
                Debug($"Teleport pawn from {pawn.Map.uniqueID} to {Map.uniqueID}", this);
                pawn.ExitMap(false, Rot4.Invalid);
                pawn.SpawnToThing(this);
                pawn.AddDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);
            }

            pawn.stances?.stunner.StunFor(300, null, false, false);
            pawn.jobs?.StopAll(false, false);

            pawn.Position = Position;
            pawn.Notify_Teleported();

            if (pawn.Downed || pawn.Dead)
                pawn.RemoveDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);

            processedCount++;
        }

        _lastProcessedCount = processedCount;
        Debug($"Processed {processedCount} pawns in this tick", this);
    }
}

[StaticConstructorOnStartup]
public class Building_PhaseSnareBeacon : Building
{
    private static readonly Color ColorActive = new(0.365f, 0.886f, 0.906f);
    private static readonly Color ColorInactive = new(0.980f, 0.549f, 0.549f);
    private static readonly Texture2D IconBatch = ContentFinder<Texture2D>.Get(
        "UI/Commands/CopySettings"
    );

    private int _ticker = 30; // A little bit later than the main comp
    private bool _enabled = true;
    private bool _captureHostile = false;
    private bool _captureFogged = false;

    private List<FloatMenuOption> _optionsQuick = [];
    private bool _cachedCoreEnable = false;

    private GameComponent_PhaseSnare _container = null!;

    private bool Enabled => _enabled && _cachedCoreEnable;

    public override Color DrawColor => Enabled && _cachedCoreEnable ? ColorActive : ColorInactive;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        var component = GameComponent_PhaseSnare.Instance;
        if (component is null)
        {
            Log.Error("PhaseSnare component not found in game");
            Destroy();
            return;
        }

        _container = component;

        var (isValid, isEnabled) = _container.IsValidAndEnabled;

        if (!isValid)
        {
            Log.Error("PhaseSnareCore is not valid, disabling this beacon");
            Destroy();
            return;
        }

        _cachedCoreEnable = isEnabled;

        _optionsQuick =
        [
            new(
                "RhyniaOverpower_PhaseSnareBeacon_OptQuick_WildAnimal".Translate(),
                OptQuickWildAnimal
            ),
        ];
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
        yield return new Command_Toggle
        {
            defaultLabel = TranslationExtension.TranslateAsEnable(true),
            icon = Designator_PhaseSnare.PhaseSnareIcon,
            isActive = () => _enabled,
            toggleAction = () =>
            {
                _enabled = !_enabled;
                Notify_ColorChanged();
            },
        };
        if (Enabled)
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
            yield return new Command_Action
            {
                defaultLabel = "RhyniaOverpower_PhaseSnareBeacon_OptQuick".Translate(),
                icon = IconBatch,
                action = () => FloatMenuHelper.SpawnMenu(_optionsQuick),
            };
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _enabled, "enabled", true);
        Scribe_Values.Look(ref _captureHostile, "captureHostile", false);
        Scribe_Values.Look(ref _captureFogged, "captureFogged", false);
    }

    protected override void Tick()
    {
        base.Tick();

        if (!Spawned || Map is null)
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
        var (isValid, isEnabled) = _container.IsValidAndEnabled;

        if (!isValid)
        {
            Warn($"PhaseSnareCore is not valid, disabling this beacon", this);
            this.ThrowMote("RhyniaOverpower_PhaseSnareBeacon_InvalidCore".Translate());
            _enabled = false;
            _cachedCoreEnable = false;
            Notify_ColorChanged();
            return;
        }

        _cachedCoreEnable = isEnabled;
        Notify_ColorChanged();

        if (!Enabled)
            return;

        var pawns = Map.mapPawns.AllPawnsSpawned;
        if (pawns is null or { Count: 0 })
            return;

        var validPawns = pawns
            .Where(p => p is { Dead: false, Downed: false, Faction: null or { IsPlayer: false } })
            .ToHashSet();

        if (_captureHostile)
            foreach (var pawn in validPawns)
            {
                var shouldCapture =
                    (
                        pawn.Faction?.HostileTo(Faction.OfPlayer) is true
                        && pawn is { IsPrisonerOfColony: false, IsSlaveOfColony: false }
                    ) || (pawn.AnimalOrWildMan() && pawn.InAggroMentalState);
                if (shouldCapture)
                {
                    if (!_captureFogged && pawn.Position.Fogged(pawn.Map))
                        continue;
                    pawn.AddDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);
                }
            }

        var processPawns = validPawns.Where(p =>
            p.HasDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation)
        );

        if (processPawns.EnumerableNullOrEmpty())
            return;

        _container.PushPawns(processPawns);
    }

    private void OptQuickWildAnimal()
    {
        var pawns = Map.mapPawns.AllPawnsSpawned;
        if (pawns is null or { Count: 0 })
            return;

        var wildAnimals = pawns
            .Where(p => p.AnimalOrWildMan() && p is { Faction: null, Dead: false })
            .ToHashSet();
        foreach (var animal in wildAnimals)
            animal.AddDesignation(DefOf_Overpower.Rhy_PhaseSnareDesignation);
    }
}

public class GameComponent_PhaseSnare : GameComponent
{
    public GameComponent_PhaseSnare(Game game)
    {
        Debug($"Initialized on {game.Info.RealPlayTimeInteracting}.", this);
    }

    private readonly AtomicContainerNullable<Building_PhaseSnareCore> _instanceCore = new();
    private readonly AtomicContainer<HashSet<Pawn>> _pendingPawns = new([]);

    public bool IsValid => _instanceCore.Value is { Spawned: true, Map: not null };
    public bool IsEnabled => _instanceCore.Value?.IsEnabled ?? false;
    public (bool, bool) IsValidAndEnabled
    {
        get
        {
            var instance = _instanceCore.Value;
            return (instance is { Spawned: true, Map: not null }, instance?.IsEnabled ?? false);
        }
    }

    public void SetInstance(Building_PhaseSnareCore thing)
    {
        if (thing is null)
            throw new ArgumentNullException(nameof(thing));
        if (_instanceCore.Value is not null)
            throw new InvalidOperationException($"Instance already set to {_instanceCore.Value}");
        _instanceCore.Value = thing;
        Debug($"Instance set to {thing}", this);
    }

    public void RemoveInstance(bool tryDestroy = false)
    {
        Debug("Removing instance", this);

        if (tryDestroy)
            _instanceCore.Value?.Destroy();

        _instanceCore.Value = null;
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
