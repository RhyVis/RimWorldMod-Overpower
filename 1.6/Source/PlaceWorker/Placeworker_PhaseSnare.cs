namespace Rhynia.Overpower;

public class PlaceWorker_PhaseSnareCore : PlaceWorker
{
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

        var container = GameComponent_PhaseSnare.Instance;

        if (container.IsValid)
        {
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                Error("Force removing existing PhaseSnareCore.");
                container.RemoveInstance(true);
                return true;
            }
            return "RhyniaOverpower_PlaceWorker_CheckPhaseSnareCore".Translate();
        }

        return true;
    }
}

public class PlaceWorker_PhaseSnareBeacon : PlaceWorker
{
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

        if (map.listerThings.ThingsOfDef(DefOf_Overpower.Rhy_PhaseSnare_Beacon).Any())
            return "RhyniaOverpower_PlaceWorker_CheckPhaseSnareBeacon1".Translate();

        if (!GameComponent_PhaseSnare.Instance.IsValid)
            return "RhyniaOverpower_PlaceWorker_CheckPhaseSnareBeacon2".Translate();

        return true;
    }
}
