namespace Rhynia.Overpower;

public class ThingSpawnDef : Def
{
    public List<ThingDef> spawnableDefs = null!;
    public ThingDef defaultDef = null!;

    public static ThingSpawnDef Named(string defName = "Rhy_ThingSpawn") =>
        DefDatabase<ThingSpawnDef>.GetNamed(defName);
}
