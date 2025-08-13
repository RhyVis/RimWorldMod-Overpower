namespace Rhynia.Overpower;

[StaticConstructorOnStartup]
public class Designator_PhaseSnare : Designator
{
    public static readonly Texture2D PhaseSnareIcon = ContentFinder<Texture2D>.Get(
        "RhyniaOverpower/UI/PhaseSnareDesignation"
    );

    public Designator_PhaseSnare()
    {
        defaultLabel = "RhyniaOverpower_Designation_PhaseSnare_Label".Translate();
        defaultDesc = "RhyniaOverpower_Designation_PhaseSnare_Desc".Translate();
        icon = PhaseSnareIcon;
        soundSucceeded = SoundDefOf.Click;
        useMouseIcon = true;
    }

    protected override DesignationDef Designation => DefOf_Overpower.Rhy_PhaseSnareDesignation;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Orders;

    public override AcceptanceReport CanDesignateCell(IntVec3 loc)
    {
        if (!loc.InBounds(Map) || loc.Fogged(Map))
            return false;

        if (
            !Map.listerThings.ThingsOfDef(DefOf_Overpower.Rhy_PhaseSnare_Beacon).Any()
            || !GameComponent_PhaseSnare.Instance.IsValid
        )
            return "RhyniaOverpower_Designation_PhaseSnare_Msg_Invalid".Translate();

        var pawnFirst = loc.GetFirstPawn(Map);
        if (pawnFirst is null)
            return "RhyniaOverpower_Designation_PhaseSnare_Msg_MustPawn".Translate();

        var tryReport = CanDesignateThing(pawnFirst);
        return tryReport.Accepted ? true : tryReport;
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        if (Map.designationManager.DesignationOn(t, Designation) is not null)
            return false;
        return t is Pawn p && IsValidTargetPawn(p);
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        c.GetThingList(Map)
            .ForEach(t =>
            {
                if (t is Pawn p)
                    DesignateThing(p);
            });
    }

    public override void DesignateThing(Thing t)
    {
        Map.designationManager.RemoveAllDesignationsOn(t);
        Map.designationManager.AddDesignation(new(t, Designation));
    }

    private static bool IsValidTargetPawn(Pawn pawn) =>
        pawn.Faction != Faction.OfPlayer
        && !pawn.InBed()
        && !pawn.IsPrisonerOfColony
        && !pawn.IsSlaveOfColony;
}
