using Verse.Sound;

namespace Rhynia.Overpower;

[StaticConstructorOnStartup]
public class Building_EnvControlUnit : Building
{
    private const float OffsetMinus1 = -1f;
    private const float OffsetMinus10 = -10f;
    private const float OffsetPlus1 = 1f;
    private const float OffsetPlus10 = 10f;
    private const float DefaultTemperature = 21f;

    private static readonly Texture2D IconTempLower = ContentFinder<Texture2D>.Get(
        "UI/Commands/TempLower"
    );
    private static readonly Texture2D IconTempRaise = ContentFinder<Texture2D>.Get(
        "UI/Commands/TempRaise"
    );
    private static readonly Texture2D IconTempReset = ContentFinder<Texture2D>.Get(
        "UI/Commands/TempReset"
    );

    private bool _active = true;
    private bool _clearGas = false;
    private bool _clearFilth = false;
    private bool _clearFire = false;
    private float _temperature = -10000f;

    private Room Room => this.GetRoom();
    private bool IsRoomValid => Room is { TouchesMapEdge: false };
    private bool IsRoomSealed => Room is { UsesOutdoorTemperature: false };

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        if (_temperature < -2000f)
            _temperature = DefaultTemperature;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _active, "controlActive", true);
        Scribe_Values.Look(ref _temperature, "controlTemperature", DefaultTemperature);
        Scribe_Values.Look(ref _clearGas, "controlClearGas", false);
        Scribe_Values.Look(ref _clearFilth, "controlClearFilth", false);
        Scribe_Values.Look(ref _clearFire, "controlClearFire", false);
    }

    public override string GetInspectString()
    {
        var builder = new StringBuilder(base.GetInspectString());
        if (!IsRoomValid)
            builder.AppendLine(
                "RhyniaOverpower_EnvControlUnit_Inspect_RoomInvalid"
                    .Translate()
                    .Colorize(Color.yellow)
            );
        builder.AppendLine(
            $"{"TargetTemperature".Translate()}: {_temperature.ToStringTemperature("F0")}"
        );
        return builder.ToString().TrimEnd();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
        yield return new Command_Toggle()
        {
            defaultLabel = _active.TranslateAsEnable(),
            icon = TexCommand.DesirePower,
            isActive = () => _active,
            toggleAction = () => _active = !_active,
        };
        if (_active)
        {
            yield return new Command_Toggle()
            {
                defaultLabel = "RhyniaOverpower_EnvControlUnit_Gizmo_ClearGas".Translate(),
                icon = TexCommand.ToggleVent,
                isActive = () => _clearGas,
                toggleAction = () =>
                {
                    _clearGas = !_clearGas;
                    if (_clearGas)
                        ClearGasInRoom();
                },
            };
            yield return new Command_Toggle()
            {
                defaultLabel = "RhyniaOverpower_EnvControlUnit_Gizmo_ClearFilth".Translate(),
                icon = TexCommand.ToggleVent,
                isActive = () => _clearFilth,
                toggleAction = () =>
                {
                    _clearFilth = !_clearFilth;
                    if (_clearFilth)
                        ClearFilthInRoom();
                },
            };
            yield return new Command_Toggle()
            {
                defaultLabel = "RhyniaOverpower_EnvControlUnit_Gizmo_ClearFire".Translate(),
                icon = TexCommand.ToggleVent,
                isActive = () => _clearFire,
                toggleAction = () =>
                {
                    _clearFire = !_clearFire;
                    if (_clearFire)
                        ClearFireInRoom();
                },
            };
            yield return new Command_Action()
            {
                defaultLabel = RoundOffset(OffsetMinus1).ToStringTemperatureOffset("F0"),
                defaultDesc = "CommandLowerTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc5,
                icon = IconTempReset,
                action = () => ChangeTargetTemperature(OffsetMinus1),
            };
            yield return new Command_Action()
            {
                defaultLabel = RoundOffset(OffsetMinus10).ToStringTemperatureOffset("F0"),
                defaultDesc = "CommandLowerTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc4,
                icon = IconTempLower,
                action = () => ChangeTargetTemperature(OffsetMinus10),
            };
            yield return new Command_Action()
            {
                defaultLabel = "CommandResetTemp".Translate(),
                defaultDesc = "CommandResetTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc1,
                icon = IconTempReset,
                action = ResetTemperature,
            };
            yield return new Command_Action()
            {
                defaultLabel = $"+{RoundOffset(OffsetPlus1).ToStringTemperatureOffset("F0")}",
                defaultDesc = "CommandRaiseTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc2,
                icon = IconTempRaise,
                action = () => ChangeTargetTemperature(OffsetPlus1),
            };
            yield return new Command_Action()
            {
                defaultLabel = $"+{RoundOffset(OffsetPlus10).ToStringTemperatureOffset("F0")}",
                defaultDesc = "CommandRaiseTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc3,
                icon = IconTempRaise,
                action = () => ChangeTargetTemperature(OffsetPlus10),
            };
        }
    }

    public override void TickRare()
    {
        base.TickRare();
        if (!_active || !Spawned || !IsRoomValid)
            return;
        if (IsRoomSealed)
            SetTemperature();
        if (_clearGas)
            ClearGasInRoom();
        if (_clearFilth)
            ClearFilthInRoom();
        if (_clearFire)
            ClearFireInRoom();
    }

    private void SetTemperature()
    {
        Debug($"Setting room temperature to {_temperature}", this);
        Room.Temperature = _temperature;
    }

    private void ChangeTargetTemperature(float offset)
    {
        SoundDefOf.DragSlider.PlayOneShotOnCamera();
        var newTemperature = _temperature + offset;
        _temperature = Mathf.Clamp(newTemperature, -273.15f, 1000f);
        ThrowTemperature();
    }

    private void ResetTemperature()
    {
        _temperature = DefaultTemperature;
        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        ThrowTemperature();
    }

    private void ThrowTemperature() => this.ThrowMote(_temperature.ToStringTemperature("F0"));

    private void ClearGasInRoom()
    {
        Debug("Clearing gas in the room", this);
        foreach (var cell in this.GetRoom().Cells)
            Map.gasGrid.ClearCellUnsafe(cell);
        Map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Gas);
    }

    private void ClearFilthInRoom()
    {
        Debug("Clearing filth in the room", this);
        foreach (var item in this.GetRoom().ThingGrid().OfType<Filth>().ToList())
            item.Destroy(DestroyMode.Vanish);
    }

    private void ClearFireInRoom()
    {
        Debug("Clearing fire in the room", this);
        foreach (var item in this.GetRoom().ThingGrid().ToList())
            if (
                ((item as Fire) ?? (item?.GetAttachment(ThingDefOf.Fire) as Fire)) is Fire
                {
                    Destroyed: false
                } fire
            )
                fire.Destroy();
    }

    private static float RoundOffset(float celsius) =>
        GenTemperature.ConvertTemperatureOffset(
            Mathf.RoundToInt(GenTemperature.CelsiusToOffset(celsius, Prefs.TemperatureMode)),
            Prefs.TemperatureMode,
            TemperatureDisplayMode.Celsius
        );
}
