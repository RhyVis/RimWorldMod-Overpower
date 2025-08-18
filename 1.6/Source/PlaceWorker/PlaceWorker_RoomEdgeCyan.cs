namespace Rhynia.Overpower;

public class PlaceWorker_RoomEdgeCyan : PlaceWorker
{
    private static Color drawColor = Color.Lerp(
        GenTemperature.ColorRoomHot,
        GenTemperature.ColorRoomCold,
        0.5f
    );

    public override void DrawGhost(
        ThingDef def,
        IntVec3 center,
        Rot4 rot,
        Color ghostCol,
        Thing? thing = null
    )
    {
        var room = center.GetRoom(Find.CurrentMap);
        if (room is { UsesOutdoorTemperature: false })
            GenDraw.DrawFieldEdges([.. room.Cells], drawColor);
    }
}
