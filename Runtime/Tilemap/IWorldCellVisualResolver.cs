using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Resolves the presentation tile for a generated world cell.
    /// Generation data remains independent from Unity presentation concerns.
    /// </summary>
    public interface IWorldCellVisualResolver
    {
        TileBase Resolve(GeneratedCell cell);
    }
}
