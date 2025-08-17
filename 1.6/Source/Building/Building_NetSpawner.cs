namespace Rhynia.Overpower;

[StaticConstructorOnStartup]
public class Building_NetSpawner : Building
{
    private Color _color = Color.clear;

    public override Color DrawColor => _color;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        var comp = GetComp<CompNetSpawner>();
        if (comp is null)
        {
            Warn("CompNetSpawner is null, cannot initialize", this);
            Destroy();
            return;
        }

        var oColor = comp.PipeNet.def.overlayOptions.overlayColor;
        if (oColor != Color.clear)
            _color = oColor;
        else if (comp.PipeNet.def.resource is { } resource)
            _color = resource.color;

        if (_color == Color.clear)
        {
            Warn("NetSpawner color is clear, cannot initialize", this);
            Destroy();
        }

        Debug(
            $"Initialized Building_NetSpawner with color {_color} and PipeNet {comp.PipeNet?.def?.defName ?? "null"}",
            this
        );
    }

    private const string DefNamePrefix = "Rhy_NetSpawner_";

    static Building_NetSpawner()
    {
        using var _ = TimingScope.Start(
            (elapsed) => Debug($"Finished processing net spawner defs in {elapsed.Milliseconds} ms")
        );

        var pending = DefDatabase<ThingDef>
            .AllDefs.Where(def =>
                def.defName.StartsWith(DefNamePrefix) && def.HasComp<CompNetSpawner>()
            )
            .ToList();
        if (pending.Count == 0)
            return;

        foreach (var def in pending)
        {
            var prop = def.GetCompProperties<CompProperties_NetSpawner>();
            var name = prop?.Resource.name ?? "<MISSING>";
            def.label = "RhyniaOverpower_NetSpawner_Def_Label".Translate(name);
            def.description = "RhyniaOverpower_NetSpawner_Def_Desc".Translate(name);
        }
    }
}
