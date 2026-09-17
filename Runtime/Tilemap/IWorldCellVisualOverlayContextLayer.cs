using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    public interface IWorldCellVisualOverlayContextLayer : IWorldCellVisualOverlayLayer
    {
        bool TryResolve(WorldPosition position, GeneratedCell cell, out TileBase tile);
    }
}
