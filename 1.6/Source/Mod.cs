namespace Rhynia.Overpower;

[StaticConstructorOnStartup]
public class Mod_Overpower(ModContentPack mod) : Mod(mod)
{
    static readonly Harmony harmony = new("Rhynia.Mod.Overpower");

    static Mod_Overpower()
    {
        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            Error("Failed to apply Harmony patches: " + ex);
        }
        Info("Mod initialized.");
    }
}

[LoggerLabel("Rhynia.Overpower")]
internal struct LogLabel;
