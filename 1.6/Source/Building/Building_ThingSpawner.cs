using AdaptiveStorage;
using StorageBuilding = AdaptiveStorage.ThingClass;

namespace Rhynia.Overpower;

public class Building_ThingSpawner : StorageBuilding
{
    private static readonly Lazy<List<ThingDef>> _thingDefs = new(
        () => ThingSpawnDef.Named().spawnableDefs ?? []
    );
    private static readonly Lazy<ThingDef> _defaultDef = new(
        () => ThingSpawnDef.Named().defaultDef
    );

    private static readonly string _filterLabel1 = "CommandCopyZoneSettingsLabel".Translate();
    private static readonly string _filterLabel2 = "CommandPasteZoneSettingsLabel".Translate();
    private static readonly string _filterLabel3 = "LinkStorageSettings".Translate();

    private bool _initialized;
    private bool _active = true;
    private int _ticker = 1250;
    private ThingDef _spawnDef = null!;
    private int StackLimit => _spawnDef.stackLimit;
    private List<FloatMenuOption> FloatMenuOptions =>
        [
            .. _thingDefs.Value.Select(def => new FloatMenuOption(
                def.LabelCap,
                () => ChangeThingDef(def),
                def
            )),
        ];

    protected override void OnSpawn(Map map, SpawnMode spawnMode)
    {
        base.OnSpawn(map, spawnMode);

        if (_thingDefs.Value.NullOrEmpty() || _defaultDef.Value is null)
        {
            Error("Valid 'ThingSpawnDef' is missing", this);
            Destroy();
            return;
        }

        if (_spawnDef is null)
        {
            Debug($"Resetting _spawnDef to default {_defaultDef}", this);
            _spawnDef = _defaultDef.Value;
        }
        else if (def.building.maxItemsInCell > 1)
        {
            Error($"Only single item stacks are supported for {def.defName}", this);
            Destroy();
            return;
        }

        settings.filter.SetDisallowAll();
        settings.filter.SetAllow(_spawnDef, true);
        settings.Priority = StoragePriority.Critical;

        Debug($"SpawnThingDef: {_spawnDef.defName}/{_spawnDef.label}", this);
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
            defaultLabel = "RhyniaOverpower_Building_SpawnThing_Gizmo1_Label".Translate(),
            defaultDesc = "RhyniaOverpower_Building_SpawnThing_Gizmo1_Desc".Translate(
                _spawnDef.label
            ),
            icon = _spawnDef.uiIcon,
            action = delegate
            {
                _ticker = 1250;
                DoSpawn();
            },
        };
        yield return new Command_Action
        {
            defaultLabel = _spawnDef.LabelCap,
            defaultDesc = "RhyniaOverpower_Building_SpawnThing_Gizmo2_Desc".Translate(
                _spawnDef.label
            ),
            icon = _spawnDef.uiIcon,
            action = delegate
            {
                Find.WindowStack.Add(
                    new FloatMenu(
                        FloatMenuOptions,
                        "RhyniaOverpower_Building_SpawnThing_Gizmo2_Menu".Translate()
                    )
                );
                _ticker = 1250;
            },
        };
        yield return new Command_Toggle
        {
            defaultLabel = "RhyniaOverpower_Building_SpawnThing_Gizmo3_Label".Translate(),
            defaultDesc = "RhyniaOverpower_Building_SpawnThing_Gizmo3_Desc".Translate(),
            icon = TexCommand.DesirePower,
            isActive = () => _active,
            toggleAction = delegate
            {
                _active = !_active;
            },
        };
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _active, "active", true);
        Scribe_Defs.Look(ref _spawnDef, "spawnDef");
    }

    public override void TickRare()
    {
        base.TickRare();
        if (!_active || !Spawned || Map is null)
            return;

        if (!_initialized)
        {
            settings.filter.SetDisallowAll();
            settings.filter.SetAllow(_spawnDef, true);
            settings.Priority = StoragePriority.Critical;
            _initialized = true;
        }

        _ticker -= 250;

        if (_ticker > 0)
            return;

        DoSpawn();

        _ticker = 1250;
    }

    private void ChangeThingDef(ThingDef def)
    {
        if (_spawnDef == def)
            return;

        GetSlotGroup().HeldThings.FirstOrDefault()?.Destroy();

        settings.filter.SetDisallowAll();
        settings.filter.SetAllow(def, true);
        settings.Priority = StoragePriority.Critical;

        _spawnDef = def;
    }

    private void DoSpawn()
    {
        var slot = GetSlotGroup();
        var exist = slot.HeldThings.FirstOrDefault();
        if (exist is null || exist?.def != _spawnDef)
        {
            // Not the right thing or no thing exists
            exist?.Destroy();

            var adder = ThingMaker.MakeThing(_spawnDef);
            adder.stackCount = StackLimit;
            GenPlace.TryPlaceThing(adder, Position, Map, ThingPlaceMode.Direct);
            this.ThrowMote("RhyniaOverpower_Building_SpawnThing_Mote2".Translate());

            return;
        }

        if (exist.stackCount >= StackLimit)
            return; // No need to spawn more

        this.ThrowMote(
            "RhyniaOverpower_Building_SpawnThing_Mote1".Translate(StackLimit - exist.stackCount)
        );
        exist.stackCount = StackLimit;
    }
}
