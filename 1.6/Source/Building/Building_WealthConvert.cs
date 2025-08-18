using System.Text;
using AdaptiveStorage;
using StorageBuilding = AdaptiveStorage.ThingClass;

namespace Rhynia.Overpower;

public class Building_WealthConvert : StorageBuilding
{
    private static Color ActiveColor = new(51 / 255, 1, 153 / 255);
    private static Color FrameColor = new(178 / 255, 102 / 255, 1);
    public override Color DrawColor => _autoConvert ? ActiveColor : Color.clear;
    public override Color DrawColorTwo => FrameColor;

    private bool _autoConvert = true;
    private bool _autoPlace = true;
    private long _leftoverSilverValue;
    private int _mode; // 0: Convert to silver, 1: Convert to gold
    private int _ticker = 1250;

    private string ModeLabel => _mode == 0 ? ThingDefOf.Silver.label : ThingDefOf.Gold.label;

    protected override void OnSpawn(Map map, SpawnMode spawnMode)
    {
        base.OnSpawn(map, spawnMode);
        Notify_ColorChanged();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
        yield return new Command_Toggle
        {
            defaultLabel = "RhyniaOverpower_WealthConvert_Gizmo1_Label".Translate(),
            defaultDesc = "RhyniaOverpower_WealthConvert_Gizmo1_Desc".Translate(),
            icon = TexCommand.DesirePower,
            isActive = () => _autoConvert,
            toggleAction = delegate
            {
                _autoConvert = !_autoConvert;
                _ticker = 1250;
                Notify_ColorChanged();
            },
        };
        yield return new Command_Toggle
        {
            defaultLabel = "RhyniaOverpower_WealthConvert_Gizmo2_Label".Translate(),
            defaultDesc = "RhyniaOverpower_WealthConvert_Gizmo2_Desc".Translate(),
            icon = TexCommand.DesirePower,
            isActive = () => _autoPlace,
            toggleAction = delegate
            {
                _autoPlace = !_autoPlace;
                _ticker = 1250;
            },
        };
        yield return new Command_Action
        {
            defaultLabel = "RhyniaOverpower_WealthConvert_Gizmo3_Label".Translate(),
            defaultDesc = "RhyniaOverpower_WealthConvert_Gizmo3_Desc".Translate(),
            icon = TexCommand.ForbidOff,
            action = DoConvert,
        };
        yield return new Command_Action
        {
            defaultLabel = "RhyniaOverpower_WealthConvert_Gizmo4_Label".Translate(),
            defaultDesc = "RhyniaOverpower_WealthConvert_Gizmo4_Desc".Translate(),
            icon = TexCommand.ForbidOff,
            action = delegate
            {
                TryPlace(true);
            },
        };
        yield return new Command_Action
        {
            defaultLabel = "RhyniaOverpower_WealthConvert_Gizmo5_Label".Translate(ModeLabel),
            defaultDesc = "RhyniaOverpower_WealthConvert_Gizmo5_Desc".Translate(),
            icon = _mode == 0 ? ThingDefOf.Silver.uiIcon : ThingDefOf.Gold.uiIcon,
            action = delegate
            {
                _mode = _mode == 0 ? 1 : 0;
            },
        };
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _mode, "mode");
        Scribe_Values.Look(ref _autoConvert, "autoConvert");
        Scribe_Values.Look(ref _autoPlace, "autoPlace");
        Scribe_Values.Look(ref _leftoverSilverValue, "leftoverSilverValue");
    }

    public override string GetInspectString()
    {
        var sb = new StringBuilder();
        sb.Append(base.GetInspectString());
        sb.AppendLineIfNotEmpty();
        sb.Append("RhyniaOverpower_WealthConvert_Info".Translate(_leftoverSilverValue));
        return sb.ToString().TrimEnd();
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        base.Destroy(mode);
        if (_leftoverSilverValue > 0)
            TryPlace();
    }

    public override void TickRare()
    {
        base.TickRare();

        if (!_autoConvert)
            return;

        if (_ticker <= 0)
            DoConvert();
        else
            _ticker -= 250;
    }

    private void DoConvert()
    {
        var things = GetSlotGroup()
            .HeldThings.Where(t =>
                _mode == 0 ? t.def != ThingDefOf.Silver : t.def != ThingDefOf.Gold
            )
            .ToList();
        if (things.Count == 0)
            return;

        var silverValue = 0;
        foreach (var thing in things)
        {
            silverValue += (int)(thing.MarketValue * thing.stackCount);
            thing.Destroy();
        }

        MoteMaker.ThrowText(
            this.TrueCenter() + new Vector3(0.5f, 0.5f, 0.5f),
            Map,
            "RhyniaOverpower_WealthConvert_Mote".Translate(silverValue)
        );

        _leftoverSilverValue += silverValue;

        TryPlace();

        _ticker = 1250;
    }

    private void TryPlace(bool calledByPlayer = false)
    {
        if (_leftoverSilverValue <= 0)
            return;
        if (!calledByPlayer && !_autoPlace)
            return;

        if (_mode == 0)
        {
            var silverLimit = ThingDefOf.Silver.stackLimit;

            while (_leftoverSilverValue > 0)
            {
                var stack = ThingMaker.MakeThing(ThingDefOf.Silver);
                if (_leftoverSilverValue > silverLimit)
                {
                    stack.stackCount = silverLimit;
                    if (GenPlace.TryPlaceThing(stack, Position, Map, ThingPlaceMode.Near))
                    {
                        _leftoverSilverValue -= silverLimit;
                    }
                    else
                    {
                        Error($"Failed to place silver {silverLimit} at {Position}");
                        this.ThrowMote(
                            "RhyniaOverpower_WealthConvert_MoteFailure".Translate(
                                _leftoverSilverValue
                            )
                        );
                        _autoPlace = false;
                        break;
                    }
                }
                else
                {
                    stack.stackCount = (int)_leftoverSilverValue;
                    if (GenPlace.TryPlaceThing(stack, Position, Map, ThingPlaceMode.Near))
                    {
                        _leftoverSilverValue = 0;
                    }
                    else
                    {
                        Error($"Failed to place silver {_leftoverSilverValue} at {Position}");
                        this.ThrowMote(
                            "RhyniaOverpower_WealthConvert_MoteFailure".Translate(
                                _leftoverSilverValue
                            )
                        );
                        _autoPlace = false;
                        break;
                    }
                }
            }
        }
        else
        {
            var goldLimit = ThingDefOf.Gold.stackLimit;

            while (_leftoverSilverValue > 0)
            {
                var goldLimitValue = (int)(goldLimit * ThingDefOf.Gold.BaseMarketValue);
                var stack = ThingMaker.MakeThing(ThingDefOf.Gold);
                if (_leftoverSilverValue > goldLimitValue)
                {
                    stack.stackCount = goldLimit;
                    if (GenPlace.TryPlaceThing(stack, Position, Map, ThingPlaceMode.Near))
                    {
                        _leftoverSilverValue -= goldLimitValue;
                    }
                    else
                    {
                        Error($"Failed to place gold {goldLimit} at {Position}");
                        this.ThrowMote(
                            "RhyniaOverpower_WealthConvert_MoteFailure".Translate(
                                _leftoverSilverValue
                            )
                        );
                        _autoPlace = false;
                        break;
                    }
                }
                else
                {
                    var goldCount = (int)(_leftoverSilverValue / ThingDefOf.Gold.BaseMarketValue);
                    stack.stackCount = goldCount;
                    if (GenPlace.TryPlaceThing(stack, Position, Map, ThingPlaceMode.Near))
                    {
                        _leftoverSilverValue = 0;
                    }
                    else
                    {
                        Error($"Failed to place gold {goldCount} at {Position}");
                        this.ThrowMote(
                            "RhyniaOverpower_WealthConvert_MoteFailure".Translate(
                                _leftoverSilverValue
                            )
                        );
                        _autoPlace = false;
                        break;
                    }
                }
            }
        }
    }
}
