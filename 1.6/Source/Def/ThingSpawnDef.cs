namespace Rhynia.Overpower;

#nullable disable

public class ThingSpawnDef : Def
{
    public List<ThingDef> spawnableDefs;
    public ThingDef defaultDef;

    public static ThingSpawnDef Named(string defName) =>
        DefDatabase<ThingSpawnDef>.GetNamed(defName);
}
