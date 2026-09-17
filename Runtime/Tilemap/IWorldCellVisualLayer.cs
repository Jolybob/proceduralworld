using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Optional visual layer that can override the presentation tile for a generated cell.
    /// Returning false allows the next layer or fallback resolver to handle the cell.
    /// </summary>
    public interface IWorldCellVisualLayer
    {
        int Order { get; }
        bool TryResolve(GeneratedCell cell, out TileBase tile);
    }
}
