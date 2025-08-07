namespace Rhynia.Overpower;

[StaticConstructorOnStartup]
public class Mod_Overpower(ModContentPack mod) : Mod(mod)
{
    static Mod_Overpower()
    {
        Out.Info("Mod Rhynia Overpower initialized.");
    }
}
