namespace Rhynia.Overpower;

[StaticConstructorOnStartup]
public class Building_HediffGiver : Building
{
    private static readonly Texture2D PrisonerIcon = ContentFinder<Texture2D>.Get(
        "UI/Commands/ForPrisoners"
    );
    private static readonly Texture2D SlaveIcon = ContentFinder<Texture2D>.Get(
        "UI/Commands/ForSlaves"
    );

    private DefModExt_HediffGiver _props = null!;

    private int _workingType;

    private TaggedString _cachedInspectStr2;
    private TaggedString _cachedInspectStr3;

    private TaggedString GizmoLabel =>
        _workingType switch
        {
            0 => "RhyniaOverpower_HediffGiver_Gizmo_LabelA".Translate(),
            1 => "RhyniaOverpower_HediffGiver_Gizmo_LabelB".Translate(),
            2 => "RhyniaOverpower_HediffGiver_Gizmo_LabelC".Translate(),
            3 => "RhyniaOverpower_HediffGiver_Gizmo_LabelD".Translate(),
            4 => "RhyniaOverpower_HediffGiver_Gizmo_LabelE".Translate(),
            5 => "RhyniaOverpower_HediffGiver_Gizmo_LabelF".Translate(),
            _ => "ERR",
        };

    private Texture2D GizmoIcon =>
        _workingType switch
        {
            0 => TexCommand.ClearPrioritizedWork,
            1 => TexCommand.ForbidOff,
            2 => TexCommand.Attack,
            3 => TexCommand.FireAtWill,
            4 => PrisonerIcon,
            5 => SlaveIcon,
            _ => TexCommand.CannotShoot,
        };

    public override Color DrawColor => _props.color;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        _props = def.GetModExtension<DefModExt_HediffGiver>();
        if (_props.hediffDef is null)
        {
            Error($"Got null hediffDef in Building_HediffGiver", this);
            Destroy();
            return;
        }

        _cachedInspectStr2 = "RhyniaOverpower_HediffGiver_Inspect2".Translate(_props.radius);
        _cachedInspectStr3 = "RhyniaOverpower_HediffGiver_Inspect3".Translate(
            _props.hediffDef.label
        );

        Notify_ColorChanged();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
        yield return new Command_Action
        {
            defaultLabel = GizmoLabel,
            defaultDesc = "RhyniaOverpower_HediffGiver_Gizmo_Desc".Translate(),
            icon = GizmoIcon,
            action = () => _workingType = (_workingType + 1) % 6,
        };
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _workingType, "workingType");
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        if (_props is { radius: > 0.1f })
            GenDraw.DrawRadiusRing(Position, _props.radius);
    }

    public override string GetInspectString()
    {
        var builder = new StringBuilder(base.GetInspectString());
        builder.AppendLine("RhyniaOverpower_HediffGiver_Inspect1".Translate(GizmoLabel));
        builder.AppendLine(_cachedInspectStr2);
        builder.AppendLine(_cachedInspectStr3);
        return builder.ToString().TrimEnd();
    }

    protected override void Tick()
    {
        base.Tick();
        if (
            _workingType == 0
            || !this.IsHashIntervalTick(_props.checkInterval)
            || this is { Spawned: false } or { Map: null }
        )
            return;
        Action();
    }

    private void Action()
    {
        if (_workingType < 1 || _workingType > 5)
        {
            Warn($"Unexpected working type in Action() {_workingType}", this);
            _workingType = 0;
            return;
        }

        var pawns = this.FindPawnsAliveInRange(_props.radius);
        var valid = _workingType switch
        {
            1 => pawns,
            2 => pawns.Where(pawn => pawn.Faction?.HostileTo(Faction.OfPlayer) is true),
            3 => pawns.Where(pawn => pawn.Faction is null or { IsPlayer: false }),
            4 => pawns.Where(pawn => pawn.IsPrisoner),
            5 => pawns.Where(pawn => pawn.IsSlave),
            _ => throw new NotImplementedException($"Unexpected working type {_workingType}"),
        };

        foreach (var pawn in valid.ToList())
            pawn.ApplyHediffWithStat(_props.hediffDef, _props.stats, _props.severityAdjust);
    }

    static Building_HediffGiver()
    {
        using var _ = TimingScope.Start(
            (t) => Debug($"Finished processing hediff giver defs in {t.Milliseconds} ms")
        );

        const string DefNamePrefix = "Rhy_HediffGiver_";

        var pending = DefDatabase<ThingDef>
            .AllDefs.Where(def =>
                def.defName.StartsWith(DefNamePrefix)
                && def.HasModExtension<DefModExt_HediffGiver>()
            )
            .ToList();
        if (pending.Count == 0)
            return;

        Debug($"Found {pending.Count} hediff giver defs to process");

        foreach (var def in pending)
        {
            var ext = def.GetModExtension<DefModExt_HediffGiver>();
            def.label = "RhyniaOverpower_HediffGiver_Def_Label".Translate(ext.hediffDef.label);
            var descBuilder = new StringBuilder(
                "RhyniaOverpower_HediffGiver_Def_DescSeg1".Translate(ext.hediffDef.label)
            );
            if (ext.stats.Count > 0)
                descBuilder.Append(
                    "RhyniaOverpower_HediffGiver_Def_DescSeg2".Translate(
                        ext.stats.Select(stat => stat.label).ToCommaList()
                    )
                );
            def.description = descBuilder.ToString().Trim();
        }
    }
}
