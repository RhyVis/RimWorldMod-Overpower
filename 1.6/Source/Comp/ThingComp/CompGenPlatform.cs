using RimWorld.Planet;

namespace Rhynia.Overpower;

public class CompProperties_GenPlatform : CompProperties
{
    public CompProperties_GenPlatform() => compClass = typeof(CompGenPlatform);
}

[StaticConstructorOnStartup]
public class CompGenPlatform : ThingComp
{
    private const int CooldownTime = 2500 * 24;
    private int _cooldown;

    private static readonly Texture2D Tex = ContentFinder<Texture2D>.Get("UI/Issues/SpaceHabitat");

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;
        yield return new Command_ActionWithCooldown
        {
            defaultLabel = "RhyniaOverpower_GenPlatform_Gizmo_Label".Translate(),
            defaultDesc = "RhyniaOverpower_GenPlatform_Gizmo_Desc".Translate(),
            icon = Tex,
            action = Action,
            cooldownPercentGetter = () => 1.0f - _cooldown / CooldownTime,
        };
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _cooldown, "genPlatformCooldown", 0);
    }

    public override void CompTick()
    {
        base.CompTick();
        if (_cooldown > 0)
            _cooldown--;
    }

    private void Action()
    {
        if (_cooldown > 0)
        {
            Messages.Message(
                "RhyniaOverpower_GenPlatform_Cooldown".Translate(_cooldown.ToStringTicksToPeriod()),
                MessageTypeDefOf.RejectInput
            );
            return;
        }
        if (!TryFindTile(out var tile))
        {
            Warn("Failed to find a valid tile for spawning orbital platform");
            return;
        }

        var obj = (SpaceMapParent)
            WorldObjectMaker.MakeWorldObject(DefOf_Overpower.Rhy_AsteroidPlatformWorldObject);
        obj.Tile = tile;
        obj.nameInt = "RhyniaOverpower_GenPlatform_GenLabel".Translate();
        Find.WorldObjects.Add(obj);

        Messages.Message(
            "RhyniaOverpower_GenPlatform_Found".Translate(),
            MessageTypeDefOf.TaskCompletion
        );

        _cooldown = CooldownTime;
    }

    private bool TryFindTile(out PlanetTile tile)
    {
        tile = PlanetTile.Invalid;

        var origin = TileFinder.TryFindRandomPlayerTile(out var originTile, false, canBeSpace: true)
            ? originTile
            : new(0, Find.WorldGrid.Surface);

        if (
            !Find.WorldGrid.TryGetFirstAdjacentLayerOfDef(
                origin,
                PlanetLayerDefOf.Orbit,
                out PlanetLayer layer
            )
        )
            return false;

        return layer.FastTileFinder.Query(new(origin, 1f, 3f)).TryRandomElement(out tile);
    }
}
