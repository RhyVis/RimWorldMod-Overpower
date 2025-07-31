namespace Rhynia.Overpower;

[DefOf]
public static class DefOf_Overpower
{
    public static DesignationDef Rhy_PhaseSnareDesignation = null!;

    public static ThingDef Rhy_PhaseSnare_Beacon = null!;

    static DefOf_Overpower() => DefOfHelper.EnsureInitializedInCtor(typeof(DefOf_Overpower));
}
