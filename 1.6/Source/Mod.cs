namespace Rhynia.Overpower;

public class Mod_Overpower(ModContentPack mod) : Mod(mod);

[StaticConstructorOnStartup]
public static class Mod_Init
{
    static Mod_Init()
    {
        Out.Info("Mod Rhynia Overpower initialized.");
    }
}
