namespace Rhynia.Overpower;

public class GenStep_AsteroidPlatform : GenStep
{
    public override int SeedPart => 366881;

    public override void Generate(Map map, GenStepParams parms)
    {
        if (!ModLister.CheckOdyssey("Asteroid"))
            return;
        Action(map, parms);
        map.OrbitalDebris = OrbitalDebrisDefOf.Manmade;
    }

    private void Action(Map map, GenStepParams parms)
    {
        var center = map.Center;
        const float radius = 4.9f;
        Debug(
            $"Starting generation on {map.uniqueID} with parms {parms}, center {center}, radius {radius}"
        );

        using var __ = TimingScope.Start(
            (t) => Debug($"Finished generation on {map.uniqueID} in {t.TotalMilliseconds} ms")
        );
        using var _ = map.pathing.DisableIncrementalScope();

        var platform = new List<IntVec3>();
        foreach (var c in GenRadial.RadialCellsAround(center, radius, true))
        {
            map.terrainGrid.SetTerrain(c, TerrainDefOf.OrbitalPlatform);
            platform.Add(c);
        }
        foreach (var c in map.AllCells.Where(c => !platform.Contains(c)))
        {
            map.terrainGrid.SetTerrain(c, TerrainDefOf.Space);
            map.roofGrid.SetRoof(c, null);
            foreach (var thing in c.GetThingList(map))
                thing.Destroy();
        }
    }
}
