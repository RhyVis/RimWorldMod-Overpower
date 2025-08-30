using RimWorld.Planet;

namespace Rhynia.Overpower;

public class NoIncidentSpaceMapParent : SpaceMapParent
{
    public override IEnumerable<IncidentTargetTagDef> IncidentTargetTags()
    {
        foreach (var it in base.IncidentTargetTags())
            if (it != IncidentTargetTagDefOf.Map_PlayerHome)
                yield return it;
    }
}
