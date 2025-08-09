namespace Rhynia.Overpower.Util;

internal static class Out
{
    private const string Label = "Rhynia.Overpower";

    public static void 曼(string err) => Error($"曼！什么罐头我说：{err}");

    public static void Debug(string message) => Label.DebugLabeled(message);

    public static void Debug(string message, Thing where) => Label.DebugLabeled(message, where);

    public static void Debug(string message, ThingComp where) => Label.DebugLabeled(message, where);

    public static void Info(string message) => Label.InfoLabeled(message);

    public static void Info(string message, Thing where) => Label.InfoLabeled(message, where);

    public static void Info(string message, ThingComp where) => Label.InfoLabeled(message, where);

    public static void Warning(string message) => Label.WarningLabeled(message);

    public static void Warning(string message, Thing where) => Label.WarningLabeled(message, where);

    public static void Warning(string message, ThingComp where) =>
        Label.WarningLabeled(message, where);

    public static void Error(string message) => Label.ErrorLabeled(message);

    public static void Error(string message, Thing where) => Label.ErrorLabeled(message, where);

    public static void Error(string message, ThingComp where) => Label.ErrorLabeled(message, where);
}
