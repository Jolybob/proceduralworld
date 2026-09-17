using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Optional overlay layer that targets a dedicated visual channel.
    /// Returning false allows the next layer in the same channel to resolve the cell.
    /// </summary>
    public interface IWorldCellVisualOverlayChannelLayer : IWorldCellVisualOverlayLayer
    {
        WorldCellVisualOverlayChannel Channel { get; }
    }
}
