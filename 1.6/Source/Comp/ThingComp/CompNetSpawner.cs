using PipeSystem;

namespace Rhynia.Overpower;

public class CompProperties_NetSpawner : CompProperties_Resource
{
    public int spawnTickInterval = 1250;

    public int spawnAmount = 500;

    public CompProperties_NetSpawner() => compClass = typeof(CompNetSpawner);
}

public class CompNetSpawner : CompResource
{
    private new CompProperties_NetSpawner Props => (CompProperties_NetSpawner)props;

    private int _ticker;

    private string SpawnThingLabel => $"{PipeNet.def.resource.name} x{Props.spawnAmount}";

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (PipeNet is null)
        {
            Warn("PipeNet is null, cannot initialize", this);
            parent.Destroy();
            return;
        }
        if (!respawningAfterLoad)
            _ticker = Props.spawnTickInterval;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _ticker, "ticker", 0);
    }

    public override string CompInspectStringExtra()
    {
        if (!parent.Spawned)
            return null!;
        var builder = new StringBuilder(base.CompInspectStringExtra());
        builder.AppendLine(
            "NextSpawnedItemIn".Translate(SpawnThingLabel).Resolve()
                + ": "
                + _ticker.ToStringTicksToPeriod().Colorize(ColoredText.DateTimeColor)
        );
        return builder.ToString().TrimEnd();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;
        if (DebugSettings.ShowDevGizmos)
            yield return new Command_Action
            {
                action = () => _ticker = 50,
                defaultLabel = "Spawn now",
                defaultDesc = "Spawn now",
            };
    }

    public override void CompTickInterval(int delta)
    {
        base.CompTickInterval(delta);
        ProcessTick(delta);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        ProcessTick(250);
    }

    private void ProcessTick(int delta)
    {
        _ticker -= delta;
        if (_ticker <= 0 && parent is { Spawned: true, Map: not null })
        {
            DoSpawn();
            _ticker = Props.spawnTickInterval;
        }
    }

    private void DoSpawn()
    {
        var net = PipeNet;
        if (net is null)
        {
            Warn("PipeNet is null, cannot spawn", this);
            return;
        }
        if (net.AvailableCapacity >= Props.spawnAmount)
        {
            net.DistributeAmongStorage(Props.spawnAmount, out var stored);
            Debug($"Spawned {stored} of {Props.spawnAmount} {PipeNet.def.resource.name}", this);
        }
        else
        {
            net.DistributeAmongStorage(net.AvailableCapacity, out var stored);
            Debug($"Spawned {stored} of {Props.spawnAmount} {PipeNet.def.resource.name}", this);
        }
    }
}

public class Building_NetSpawner : Building
{
    private Color _color = Color.clear;

    public override Color DrawColor => _color;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        var comp = GetComp<CompNetSpawner>();
        if (comp is null)
        {
            Warn("CompNetSpawner is null, cannot initialize", this);
            Destroy();
            return;
        }

        var oColor = comp.PipeNet.def.overlayOptions.overlayColor;
        if (oColor != Color.clear)
            _color = oColor;
        else if (comp.PipeNet.def.resource is { } resource)
            _color = resource.color;

        if (_color == Color.clear)
        {
            Warn("NetSpawner color is clear, cannot initialize", this);
            Destroy();
        }

        Debug(
            $"Initialized Building_NetSpawner with color {_color} and PipeNet {comp.PipeNet?.def?.defName ?? "null"}",
            this
        );
    }
}
