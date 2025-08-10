namespace Rhynia.Overpower.Util;

internal static class Out
{
    private const string Label = "Rhynia.Overpower";

    public static void 曼(string err) => Error($"曼！什么罐头我说：{err}");

    public static void Debug(string message) => Label.DebugLabeled(message);

    public static void Debug(string message, Thing o) => Label.DebugLabeled(message, o);

    public static void Debug(string message, ThingComp o) => Label.DebugLabeled(message, o);

    public static void Info(string message) => Label.InfoLabeled(message);

    public static void Info(string message, Thing o) => Label.InfoLabeled(message, o);

    public static void Info(string message, ThingComp o) => Label.InfoLabeled(message, o);

    public static void Warning(string message) => Label.WarningLabeled(message);

    public static void Warning(string message, Thing o) => Label.WarningLabeled(message, o);

    public static void Warning(string message, ThingComp o) => Label.WarningLabeled(message, o);

    public static void Error(string message) => Label.ErrorLabeled(message);

    public static void Error(string message, Thing o) => Label.ErrorLabeled(message, o);

    public static void Error(string message, ThingComp o) => Label.ErrorLabeled(message, o);
}
