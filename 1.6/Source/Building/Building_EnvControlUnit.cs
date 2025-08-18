using Verse.Sound;

namespace Rhynia.Overpower;

[StaticConstructorOnStartup]
public class Building_EnvControlUnit : Building
{
    private const float OffsetMinus1 = -1f;
    private const float OffsetMinus10 = -10f;
    private const float OffsetPlus1 = 1f;
    private const float OffsetPlus10 = 10f;

    private static readonly Texture2D IconLower = ContentFinder<Texture2D>.Get(
        "UI/Commands/TempLower"
    );
    private static readonly Texture2D IconRaise = ContentFinder<Texture2D>.Get(
        "UI/Commands/TempRaise"
    );
    private static readonly Texture2D IconReset = ContentFinder<Texture2D>.Get(
        "UI/Commands/TempReset"
    );

    private CompFlickable _compFlickable = null!;

    private float _temperature = -10000f;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        _compFlickable = GetComp<CompFlickable>();

        if (_compFlickable is null)
        {
            Error($"Spawned without CompFlickable", this);
            Destroy();
            return;
        }

        if (_temperature < -2000f)
            _temperature = 21f;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _temperature, "controlTemperature");
    }

    public override string GetInspectString()
    {
        var builder = new StringBuilder(base.GetInspectString());
        builder.Append($"{"TargetTemperature".Translate()}: ");
        builder.AppendLine(_temperature.ToStringTemperature("F0"));
        return builder.ToString().TrimEnd();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
        yield return new Command_Action()
        {
            defaultLabel = RoundOffset(OffsetMinus1).ToStringTemperatureOffset("F0"),
            defaultDesc = "CommandLowerTempDesc".Translate(),
            hotKey = KeyBindingDefOf.Misc5,
            icon = IconReset,
            action = () => ChangeTargetTemperature(OffsetMinus1),
        };
        yield return new Command_Action()
        {
            defaultLabel = RoundOffset(OffsetMinus10).ToStringTemperatureOffset("F0"),
            defaultDesc = "CommandLowerTempDesc".Translate(),
            hotKey = KeyBindingDefOf.Misc4,
            icon = IconLower,
            action = () => ChangeTargetTemperature(OffsetMinus10),
        };
        yield return new Command_Action()
        {
            defaultLabel = "CommandResetTemp".Translate(),
            defaultDesc = "CommandResetTempDesc".Translate(),
            hotKey = KeyBindingDefOf.Misc1,
            icon = IconReset,
            action = ResetTemperature,
        };
        yield return new Command_Action()
        {
            defaultLabel = $"+{RoundOffset(OffsetPlus1).ToStringTemperatureOffset("F0")}",
            defaultDesc = "CommandRaiseTempDesc".Translate(),
            hotKey = KeyBindingDefOf.Misc2,
            icon = IconRaise,
            action = () => ChangeTargetTemperature(OffsetPlus1),
        };
        yield return new Command_Action()
        {
            defaultLabel = $"+{RoundOffset(OffsetPlus10).ToStringTemperatureOffset("F0")}",
            defaultDesc = "CommandRaiseTempDesc".Translate(),
            hotKey = KeyBindingDefOf.Misc3,
            icon = IconRaise,
            action = () => ChangeTargetTemperature(OffsetPlus10),
        };
    }

    public override void TickRare()
    {
        base.TickRare();
        if (!_compFlickable.SwitchIsOn || !Spawned || this.GetRoom() is not { } room)
            return;
        room.Temperature = _temperature;
    }

    private void ResetTemperature()
    {
        _temperature = 21f;
        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        ThrowTemperature();
    }

    private void ChangeTargetTemperature(float offset)
    {
        SoundDefOf.DragSlider.PlayOneShotOnCamera();
        var newTemperature = _temperature + offset;
        _temperature = Mathf.Clamp(newTemperature, -273.15f, 1000f);
        ThrowTemperature();
    }

    private void ThrowTemperature() => this.ThrowMote(_temperature.ToStringTemperature("F0"));

    private static float RoundOffset(float celsius) =>
        GenTemperature.ConvertTemperatureOffset(
            Mathf.RoundToInt(GenTemperature.CelsiusToOffset(celsius, Prefs.TemperatureMode)),
            Prefs.TemperatureMode,
            TemperatureDisplayMode.Celsius
        );
}
