namespace Rhynia.Overpower;

#nullable disable

[DefOf]
public static class DefOf_Overpower
{
    public static DesignationDef Rhy_PhaseSnareDesignation;

    public static ThingDef Rhy_PhaseSnare_Beacon;

    public static WorldObjectDef Rhy_AsteroidPlatformWorldObject;

    static DefOf_Overpower() => DefOfHelper.EnsureInitializedInCtor(typeof(DefOf_Overpower));
}
