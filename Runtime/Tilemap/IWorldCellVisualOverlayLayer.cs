using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Optional supplementary visual layer rendered on a dedicated overlay Tilemap.
    /// Returning false leaves the overlay cell empty and allows the next layer to resolve it.
    /// </summary>
    public interface IWorldCellVisualOverlayLayer
    {
        int Order { get; }
        bool TryResolve(GeneratedCell cell, out TileBase tile);
    }
}
