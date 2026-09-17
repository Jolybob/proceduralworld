namespace Jolybob.ProceduralWorld.Tilemap
{
    public interface IWorldCellVisualStateProvider
    {
        WorldCellVisualState GetState(WorldPosition position, GeneratedCell cell);
    }
}
