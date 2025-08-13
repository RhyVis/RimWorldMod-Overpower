namespace Rhynia.Overpower;

[StaticConstructorOnStartup]
public class Mod_Overpower(ModContentPack mod) : Mod(mod)
{
    static Mod_Overpower() => Info("Mod initialized.");
}

[LoggerLabel("Rhynia.Overpower")]
internal struct LogLabel;
