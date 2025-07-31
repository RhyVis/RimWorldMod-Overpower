namespace Rhynia.Overpower.Util;

public static class Out
{
    private const string Label = "Rhynia.Overpower";

    public static void 曼(string err) => Error($"曼！什么罐头我说：{err}");

    public static void Debug(string message) => Label.DebugLabeled(message);

    public static void Info(string message) => Label.InfoLabeled(message);

    public static void Warning(string message) => Label.WarningLabeled(message);

    public static void Error(string message) => Label.ErrorLabeled(message);
}
