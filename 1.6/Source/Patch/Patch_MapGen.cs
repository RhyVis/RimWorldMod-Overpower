namespace Rhynia.Overpower.Patch;

[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GenerateMap))]
internal static class Patch_MapGen
{
    static void Postfix(Map __result)
    {
        if (__result.Parent is NoIncidentSpaceMapParent)
            __result.wasSpawnedViaGravShipLanding = false;
    }
}
