using AdaptiveStorage;
using StorageBuilding = AdaptiveStorage.ThingClass;

namespace Rhynia.Overpower;

[StaticConstructorOnStartup]
public class Building_ThingSpawner : StorageBuilding
{
    private static readonly string _filterLabel1 = "CommandCopyZoneSettingsLabel".Translate();
    private static readonly string _filterLabel2 = "CommandPasteZoneSettingsLabel".Translate();
    private static readonly string _filterLabel3 = "LinkStorageSettings".Translate();

    private static Color ActiveColor = new(102 / 255, 1, 102 / 255);
    public override Color DrawColor => _active ? ActiveColor : Color.clear;
    public override Color DrawColorTwo => Color.cyan;

    private DefModExt_ThingSpawner _spawnerDef = null!;
    private List<ThingDef> ThingDefs => _spawnerDef.spawnableDefs;
    private ThingDef DefaultThingDef => _spawnerDef.defaultDef;

    private bool _initialized;
    private bool _active = true;
    private int _ticker = 1250;

    private List<FloatMenuOption> _options = [];
    private ThingDef _spawnTargetDef = null!;
    private int StackLimit => _spawnTargetDef.stackLimit;

    protected override void OnSpawn(Map map, SpawnMode spawnMode)
    {
        base.OnSpawn(map, spawnMode);

        _spawnerDef = def.GetModExtension<DefModExt_ThingSpawner>();
        if (_spawnerDef is null || !_spawnerDef.valid)
        {
            Error($"No valid 'DefModExt_ThingSpawner' found for {def.defName}", this);
            Destroy();
            return;
        }

        if (ThingDefs.NullOrEmpty() || DefaultThingDef is null)
        {
            Error(
                $"Valid 'ThingSpawnDef.spawnableDefs' or 'ThingSpawnDef.defaultDef' is missing for {def.defName}",
                this
            );
            Destroy();
            return;
        }

        _options =
        [
            .. ThingDefs.Select(def => new FloatMenuOption(
                def.LabelCap,
                () => ChangeThingDef(def),
                def
            )),
        ];

        if (_spawnTargetDef is null)
        {
            Debug($"Resetting _spawnDef to default {DefaultThingDef}", this);
            _spawnTargetDef = DefaultThingDef;
        }
        else if (def.building.maxItemsInCell > 1)
        {
            Error($"Only single item stacks are supported for {def.defName}", this);
            Destroy();
            return;
        }

        Notify_ColorChanged();

        SetDef(_spawnTargetDef);

        Debug($"SpawnThingDef: {_spawnTargetDef.defName}/{_spawnTargetDef.label}", this);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            if (
                gizmo is Command_Action action
                && (
                    action.defaultLabel == _filterLabel1
                    || action.defaultLabel == _filterLabel2
                    || action.defaultLabel == _filterLabel3
                )
            )
                continue;
            yield return gizmo;
        }
        yield return new Command_Action
        {
            defaultLabel = "RhyniaOverpower_ThingSpawner_Gizmo1_Label".Translate(),
            defaultDesc = "RhyniaOverpower_ThingSpawner_Gizmo1_Desc".Translate(
                _spawnTargetDef.label
            ),
            icon = _spawnTargetDef.uiIcon,
            action = () =>
            {
                _ticker = 1250;
                DoSpawn();
            },
        };
        yield return new Command_Action
        {
            defaultLabel = _spawnTargetDef.LabelCap,
            defaultDesc = "RhyniaOverpower_ThingSpawner_Gizmo2_Desc".Translate(
                _spawnTargetDef.label
            ),
            icon = _spawnTargetDef.uiIcon,
            action = () =>
            {
                FloatMenuHelper.SpawnMenuTitled(
                    "RhyniaOverpower_ThingSpawner_Gizmo2_Menu".Translate(),
                    _options
                );
                _ticker = 1250;
            },
        };
        yield return new Command_Toggle
        {
            defaultLabel = "RhyniaOverpower_ThingSpawner_Gizmo3_Label".Translate(),
            defaultDesc = "RhyniaOverpower_ThingSpawner_Gizmo3_Desc".Translate(),
            icon = TexCommand.DesirePower,
            isActive = () => _active,
            toggleAction = () =>
            {
                _active = !_active;
                Notify_ColorChanged();
            },
        };
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _active, "active", true);
        Scribe_Defs.Look(ref _spawnTargetDef, "spawnDef");
    }

    public override void TickRare()
    {
        base.TickRare();
        if (!_active || !Spawned || Map is null)
            return;

        if (!_initialized)
        {
            SetDef(_spawnTargetDef);
            _initialized = true;
            Debug($"Initialized with spawnDef: {_spawnTargetDef.defName}, bugs off, please!", this);
        }

        if (_ticker <= 0)
        {
            DoSpawn();
            _ticker = 1250;
        }
        else
            _ticker -= 250;
    }

    private void ChangeThingDef(ThingDef def)
    {
        if (_spawnTargetDef == def)
            return;

        GetSlotGroup().HeldThings.FirstOrDefault()?.Destroy();
        SetDef(def);

        _spawnTargetDef = def;
    }

    private void DoSpawn()
    {
        var slot = GetSlotGroup();
        var exist = slot.HeldThings.FirstOrDefault();
        if (exist is null || exist?.def != _spawnTargetDef)
        {
            // Not the right thing or no thing exists
            exist?.Destroy();

            var adder = ThingMaker.MakeThing(_spawnTargetDef);
            adder.stackCount = StackLimit;
            GenPlace.TryPlaceThing(adder, Position, Map, ThingPlaceMode.Direct);
            this.ThrowMote("RhyniaOverpower_ThingSpawner_Mote2".Translate());

            Debug($"Spawning new {adder.def.defName}", this);

            return;
        }

        if (exist.stackCount >= StackLimit)
            return; // No need to spawn more

        this.ThrowMote(
            "RhyniaOverpower_ThingSpawner_Mote1".Translate(StackLimit - exist.stackCount)
        );
        Debug(
            $"Increasing stack of {exist.def.defName} from {exist.stackCount} to {StackLimit}",
            this
        );
        exist.stackCount = StackLimit;
    }

    private void SetDef(ThingDef def)
    {
        settings.filter.SetDisallowAll();
        settings.filter.SetAllow(def, true);
        settings.Priority = StoragePriority.Critical;
    }

    static Building_ThingSpawner()
    {
        using var _ = TimingScope.Start(
            (t) => Debug($"Finished processing Building_ThingSpawner defs in {t.Milliseconds} ms")
        );

        const string DefNamePrefix = "Rhy_ThingSpawner_";

        var pending = DefDatabase<ThingDef>
            .AllDefs.Where(def =>
                def.defName.StartsWith(DefNamePrefix)
                && def.HasModExtension<DefModExt_ThingSpawner>()
            )
            .ToList();
        if (pending.Count == 0)
            return;

        foreach (var def in pending)
        {
            var ext = def.GetModExtension<DefModExt_ThingSpawner>();
            if (ext is null || !ext.valid)
            {
                Error($"Invalid 'DefModExt_ThingSpawner' found for {def.defName}", def);
                continue;
            }

            var name = ext.name.NullOrEmpty() ? def.defName.Replace(DefNamePrefix, "") : ext.name;

            def.label = "RhyniaOverpower_ThingSpawner_Def_Label".Translate(name);
            def.description = "RhyniaOverpower_ThingSpawner_Def_Desc".Translate(name);
        }
    }
}
