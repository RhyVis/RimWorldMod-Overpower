namespace Rhynia.Overpower;

public class Building_WealthConvertEx : Building, IStoreSettingsParent
{
    private StorageSettings _storageSetting = null!;

    private bool _autoConvert = true;
    private bool _autoPlace = true;
    private long _storeValue;
    private ThingDef _convertDef = ThingDefOf.Silver;

    private static readonly List<ThingDef> _convertibleDefs =
    [
        ThingDefOf.Silver,
        ThingDefOf.Gold,
        ThingDefOf.Steel,
        ThingDefOf.Plasteel,
        ThingDefOf.Uranium,
        ThingDefOf.ComponentIndustrial,
        ThingDefOf.ComponentSpacer,
    ];
    private List<FloatMenuOption> ConvertibleOptions =>
        [
            .. _convertibleDefs.Select(def => new FloatMenuOption(
                def.LabelCap,
                () => _convertDef = def,
                def
            )),
        ];

    public override void PostMake()
    {
        base.PostMake();
        _storageSetting = new(this);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        _convertDef ??= ThingDefOf.Silver;
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        base.Destroy(mode);
        TryPlace();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _autoConvert, "autoConvert", true);
        Scribe_Values.Look(ref _autoPlace, "autoPlace", true);
        Scribe_Values.Look(ref _storeValue, "storeValue");
        Scribe_Defs.Look(ref _convertDef, "convertDef");
        Scribe_Deep.Look(ref _storageSetting, "convertFilter", this);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var item in base.GetGizmos())
            yield return item;
        yield return new Command_Toggle
        {
            defaultLabel = "RhyniaOverpower_WealthConvert_Gizmo1_Label".Translate(),
            defaultDesc = "RhyniaOverpower_WealthConvert_Gizmo1_Desc".Translate(),
            icon = TexCommand.DesirePower,
            isActive = () => _autoConvert,
            toggleAction = () => _autoConvert = !_autoConvert,
        };
        yield return new Command_Toggle
        {
            defaultLabel = "RhyniaOverpower_WealthConvert_Gizmo2_Label".Translate(),
            defaultDesc = "RhyniaOverpower_WealthConvert_Gizmo2_Desc".Translate(),
            icon = TexCommand.DesirePower,
            isActive = () => _autoPlace,
            toggleAction = () => _autoPlace = !_autoPlace,
        };
        yield return new Command_Action
        {
            defaultLabel = "RhyniaOverpower_WealthConvert_Gizmo3_Label".Translate(),
            defaultDesc = "RhyniaOverpower_WealthConvert_Gizmo3_Desc".Translate(),
            icon = TexCommand.ForbidOff,
            action = TryConvert,
        };
        yield return new Command_Action
        {
            defaultLabel = "RhyniaOverpower_WealthConvert_Gizmo4_Label".Translate(),
            defaultDesc = "RhyniaOverpower_WealthConvert_Gizmo4_Desc".Translate(),
            icon = TexCommand.ForbidOff,
            action = () => TryPlace(true),
        };
        yield return new Command_Action
        {
            defaultLabel = "RhyniaOverpower_WealthConvert_Gizmo5_Label".Translate(
                _convertDef.LabelCap
            ),
            defaultDesc = "RhyniaOverpower_WealthConvert_Gizmo5_Desc".Translate(),
            icon = _convertDef.uiIcon,
            action = () => FloatMenuHelper.SpawnMenu(ConvertibleOptions),
        };
    }

    public override string GetInspectString()
    {
        var sb = new StringBuilder(base.GetInspectString());
        sb.AppendInNewLine("RhyniaOverpower_WealthConvert_Info".Translate(_storeValue));
        return sb.ToString().TrimEnd();
    }

    public override void TickRare()
    {
        base.TickRare();

        if (!Spawned || Map == null)
            return;

        if (_autoConvert)
            TryConvert();
        if (_autoPlace)
            TryPlace();
    }

    private void TryConvert()
    {
        foreach (
            var thing in Map
                .listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver)
                .AsParallel()
                .Where(t =>
                    !t.IsForbidden(Faction)
                    && _storageSetting.filter.Allows(t)
                    && t is not Building
                    && t.def != _convertDef
                )
                .ToList()
        )
            if (thing is { Spawned: true, Destroyed: false })
            {
                _storeValue += (long)(thing.MarketValue * thing.stackCount);
                thing.Destroy();
            }
    }

    private void TryPlace(bool calledByPlayer = false)
    {
        if (_storeValue <= 0)
            return;
        if (!calledByPlayer && !_autoPlace)
            return;

        var def = _convertDef.BaseMarketValue >= 1 ? _convertDef : ThingDefOf.Silver;
        var value = def.BaseMarketValue;
        var stackLimit = def.stackLimit;

        while (_storeValue > 0)
        {
            var stack = ThingMaker.MakeThing(def);
            var stackLimitValue = (int)(stackLimit * value);
            if (_storeValue > stackLimitValue)
            {
                stack.stackCount = stackLimit;
                if (GenPlace.TryPlaceThing(stack, Position, Map, ThingPlaceMode.Near))
                {
                    _storeValue -= stackLimitValue;
                }
                else
                {
                    Error(
                        $"Failed to place {def.defName} ({def.LabelCap}) {stackLimit} at {Position}"
                    );
                    this.ThrowMote(
                        "RhyniaOverpower_WealthConvert_MoteFailure".Translate(_storeValue)
                    );
                    _autoPlace = false;
                    break;
                }
            }
            else
            {
                var stackCount = (int)(_storeValue / value);
                stack.stackCount = stackCount;
                if (GenPlace.TryPlaceThing(stack, Position, Map, ThingPlaceMode.Near))
                {
                    _storeValue = 0;
                }
                else
                {
                    Error(
                        $"Failed to place {def.defName} ({def.LabelCap}) {stackCount} at {Position}"
                    );
                    this.ThrowMote(
                        "RhyniaOverpower_WealthConvert_MoteFailure".Translate(_storeValue)
                    );
                    _autoPlace = false;
                    break;
                }
            }
        }
    }

    public bool StorageTabVisible => true;

    public StorageSettings GetStoreSettings() => _storageSetting;

    public StorageSettings GetParentStoreSettings() => null!;

    public void Notify_SettingsChanged() { }
}
