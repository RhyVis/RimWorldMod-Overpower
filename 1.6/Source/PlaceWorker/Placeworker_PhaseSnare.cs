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

        if (PhaseSnareContainer.IsValid)
        {
            if (Prefs.DevMode && ModSettings_Baseline.DebugMode)
            {
                Out.曼("Force removing existing PhaseSnareCore.");
                PhaseSnareContainer.Instance!.Destroy();
                PhaseSnareContainer.Instance = null;
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

        if (!PhaseSnareContainer.IsValid)
            return "RhyniaOverpower_PlaceWorker_CheckPhaseSnareBeacon2".Translate();

        return true;
    }
}
