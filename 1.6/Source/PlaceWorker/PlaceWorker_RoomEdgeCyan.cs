namespace Rhynia.Overpower;

public class PlaceWorker_RoomEdgeCyan : PlaceWorker
{
    public override void DrawGhost(
        ThingDef def,
        IntVec3 center,
        Rot4 rot,
        Color ghostCol,
        Thing? thing = null
    )
    {
        var room = center.GetRoom(Find.CurrentMap);
        if (room is not null)
            GenDraw.DrawFieldEdges([.. room.Cells], Color.cyan);
    }
}
