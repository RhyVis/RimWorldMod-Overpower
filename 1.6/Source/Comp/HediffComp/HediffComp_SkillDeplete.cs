namespace Rhynia.Overpower;

public class HediffCompProperties_SkillDeplete : HediffCompProperties
{
    public int checkInterval = 5_000;
    public int depleteAmount = 5_000;

    public HediffCompProperties_SkillDeplete() => compClass = typeof(HediffComp_SkillDeplete);
}

public class HediffComp_SkillDeplete : HediffComp
{
    private HediffCompProperties_SkillDeplete Props => (HediffCompProperties_SkillDeplete)props;
    private int _ticksUntilNextDeplete;

    public override void CompPostMake()
    {
        base.CompPostMake();
        _ticksUntilNextDeplete = Props.checkInterval;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref _ticksUntilNextDeplete, "ticksUntilNextDeplete");
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        if (_ticksUntilNextDeplete > 0)
        {
            _ticksUntilNextDeplete--;
            return;
        }

        Action();

        _ticksUntilNextDeplete = Props.checkInterval;
    }

    private void Action()
    {
        if (parent.pawn.skills?.skills.RandomElement() is not { } skill)
            return;

        if (!skill.DepleteSkillLevel(Props.depleteAmount))
            return;

        MoteMaker.ThrowText(
            parent.pawn.TrueCenter() + new Vector3(0.5f, 0.5f, 0.5f),
            parent.pawn.Map,
            "RhyniaOverpower_HediffComp_SkillDeplete_Mote".Translate(
                skill.def.label,
                Props.depleteAmount
            )
        );
    }
}
