namespace Rhynia.Overpower;

public abstract class PlaceWorker_MapOneOnly : PlaceWorker
{
    public abstract ThingDef LimitedDef { get; }

    public override AcceptanceReport AllowsPlacing(
        BuildableDef checkingDef,
        IntVec3 loc,
        Rot4 rot,
        Map map,
        Thing? thingToIgnore = null,
        Thing? thing = null
    )
    {
        if (!base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore, thing).Accepted)
            return false;

        if (map.listerThings.ThingsOfDef(LimitedDef).Any())
            return "RhyniaOverpower_PlaceWorker_CheckMapOneOnly".Translate(LimitedDef.LabelCap);

        return true;
    }
}

public class PlaceWorker_MapOneOnly_WealthConvert : PlaceWorker_MapOneOnly
{
    public override ThingDef LimitedDef => DefOf_Overpower.Rhy_WealthConvertEx;
}
