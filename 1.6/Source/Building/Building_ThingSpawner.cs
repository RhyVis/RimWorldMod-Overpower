using AdaptiveStorage;
using StorageBuilding = AdaptiveStorage.ThingClass;

namespace Rhynia.Overpower;

public class Building_ThingSpawner : StorageBuilding
{
    private bool _active = true;
    private int _ticker = 1250;
    private List<ThingDef> _thingDefs = [];
    private int _spawnThingIndex;

    private ThingDef SpawnThingDef => _thingDefs[_spawnThingIndex];
    private List<FloatMenuOption> FloatMenuOptions =>
        [
            .. _thingDefs.Select(def => new FloatMenuOption(
                def.LabelCap,
                () => HandleChangeThingDef(_thingDefs.IndexOf(def)),
                def
            )),
        ];
    private int StackLimit => SpawnThingDef.stackLimit;

    protected override void OnSpawn(Map map, SpawnMode spawnMode)
    {
        base.OnSpawn(map, spawnMode);

        var modExt = def.GetModExtension<DefModExt_ThingSpawner>();
        if (modExt is null || modExt.thingDefs.NullOrEmpty())
        {
            Out.曼($"No thing defs defined for {def.defName}");
            Destroy();
            return;
        }

        _thingDefs = modExt.thingDefs;

        if (SpawnThingDef is null)
        {
            _spawnThingIndex = 0;
            if (SpawnThingDef is null)
            {
                Out.曼($"No valid thing defs found for {def.defName}");
                Destroy();
                return;
            }
        }
        else if (def.building.maxItemsInCell > 1)
        {
            Out.曼($"Only single item stacks are supported for {def.defName}");
            Destroy();
            return;
        }

        settings.filter.SetDisallowAll();
        settings.filter.SetAllow(SpawnThingDef, true);
        Out.Debug($"SpawnThingDef: {SpawnThingDef.defName}/{SpawnThingDef.label}");
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
        yield return new Command_Action
        {
            defaultLabel = "RhyniaOverpower_Building_SpawnThing_Gizmo1_Label".Translate(),
            defaultDesc = "RhyniaOverpower_Building_SpawnThing_Gizmo1_Desc".Translate(
                SpawnThingDef.label
            ),
            icon = SpawnThingDef.uiIcon,
            action = delegate
            {
                _ticker = 1250;
                DoSpawn();
            },
        };
        yield return new Command_Action
        {
            defaultLabel = SpawnThingDef.LabelCap,
            defaultDesc = "RhyniaOverpower_Building_SpawnThing_Gizmo2_Desc".Translate(
                SpawnThingDef.label
            ),
            icon = SpawnThingDef.uiIcon,
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
        Scribe_Values.Look(ref _spawnThingIndex, "spawnThingIndex", 0);
    }

    public override void TickRare()
    {
        base.TickRare();
        if (!_active || !Spawned || Map is null)
            return;

        _ticker -= 250;

        if (_ticker > 0)
            return;

        DoSpawn();

        _ticker = 1250;
    }

    private void HandleChangeThingDef(int index)
    {
        if (index < 0 || index >= _thingDefs.Count)
        {
            Out.曼($"Invalid thing def index {index} for {def.defName}");
            return;
        }

        if (_spawnThingIndex == index)
            return;

        settings.filter.SetDisallowAll();
        settings.filter.SetAllow(_thingDefs[index], true);

        _spawnThingIndex = index;
    }

    private void DoSpawn()
    {
        var slot = GetSlotGroup();
        var exist = slot.HeldThings.FirstOrDefault();
        if (exist?.def != SpawnThingDef)
        {
            // Not the right thing or no thing exists
            exist?.Destroy();

            var adder = ThingMaker.MakeThing(SpawnThingDef);
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
