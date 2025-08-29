namespace Rhynia.Overpower;

[StaticConstructorOnStartup]
public class Building_ThingSpawnerEx : Building
{
    public static readonly List<ThingDef> ProductDefs;
    private static readonly Texture2D Icon = ThingDefOf.ComponentSpacer.uiIcon;

    private CompForbiddable _compForbiddable = null!;

    // State: Not in dictionary = Disabled, >0 = Enabled
    public Dictionary<ThingDef, int> Quantities = [];

    private IEnumerable<KeyValuePair<ThingDef, int>> PendingThings =>
        Quantities.Where(kvp => kvp.Value > 0);

    private bool Forbidden => _compForbiddable?.Forbidden ?? true;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        _compForbiddable = GetComp<CompForbiddable>();
        if (_compForbiddable is null)
        {
            Error("CompForbiddable is missing!", this);
            Destroy();
        }

        foreach (var remove in Quantities.Where(kvp => kvp.Key is null || kvp.Value <= 0).ToList())
            Quantities.Remove(remove.Key);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref Quantities, "quantities", LookMode.Def, LookMode.Value);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;

        yield return new Command_Action
        {
            action = () => Find.WindowStack.Add(new UI.SpawnerListWindow(this)),
            defaultLabel = "RhyniaOverpower_ThingSpawnerEx_GizmoWindow_Label".Translate(),
            defaultDesc = "RhyniaOverpower_ThingSpawnerEx_GizmoWindow_Desc".Translate(),
            icon = Icon,
        };
    }

    public override void TickRare()
    {
        base.TickRare();
        if (Forbidden || !Spawned || Map is null || !PendingThings.Any())
            return;
        foreach (var (def, quantity) in PendingThings)
            if (!DoSpawn(def, quantity))
            {
                Warn(
                    $"Failed to spawn {def.defName}({def.LabelCap}) -> {quantity}, disabling",
                    this
                );
                _compForbiddable.Forbidden = true;
                break;
            }
    }

    private bool DoSpawn(ThingDef def, int quantity)
    {
        try
        {
            var existCount = GetExistingCount(def);
            if (existCount >= quantity)
                return true;

            var thing = ThingMaker.MakeThing(def);
            thing.stackCount = Mathf.Min(quantity - existCount, def.stackLimit);
            if (GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near))
            {
                Debug(
                    $"Spawned {def.defName}({def.LabelCap}) x{thing.stackCount} at {thing.Position}",
                    this
                );
                return true;
            }
            else
                return false;
        }
        catch (Exception ex)
        {
            Error($"Failed to spawn {def.defName}: {ex}", this);
            return false;
        }
    }

    private int GetExistingCount(ThingDef def) =>
        Map?.listerThings.ThingsOfDef(def).Sum(thing => thing.stackCount) ?? 0;

    public void Notify_StateChanged(ThingDef def, int quantity)
    {
        Debug(
            $"Thing {def.defName}({def.LabelCap}) state changed: Quantity={quantity} (Enabled={quantity > 0})",
            this
        );
    }

    static Building_ThingSpawnerEx()
    {
        using var _ = TimingScope.Start(
            (t) => Debug($"Initialized ThingSpawnerEx in {t.TotalMilliseconds} ms")
        );

        ProductDefs =
        [
            .. DefDatabase<ThingDef>.AllDefs.Where(def =>
                def
                    is {
                        category: ThingCategory.Item,
                        IsPlant: false,
                        IsCorpse: false,
                        IsFilth: false,
                        BaseMarketValue: > 0
                    } d
                && !d.label.NullOrEmpty()
                && !d.HasComp<CompQuality>()
            ),
        ];

        Debug($"Loaded {ProductDefs.Count} spawnable ThingDefs");
    }
}
