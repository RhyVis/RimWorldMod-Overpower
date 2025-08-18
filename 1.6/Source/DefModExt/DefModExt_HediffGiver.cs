namespace Rhynia.Overpower;

#nullable disable

public class DefModExt_HediffGiver : DefModExtension
{
    public int checkInterval = 600;
    public float radius = 2.9f;
    public float severityAdjust = 1.0f;

    public bool isNegative = false;

    [Unsaved]
    public Color color = Color.white;

    public HediffDef hediffDef;
    public List<StatDef> stats = [];

    public override void ResolveReferences(Def parentDef)
    {
        color = isNegative ? Color.red : Color.green;
    }
}
