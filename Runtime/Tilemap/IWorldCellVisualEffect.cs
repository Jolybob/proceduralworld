using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Optional supplementary visual effect for a generated cell.
    /// Effects are rendered independently from the base tile so decals and overlays do not replace terrain.
    /// </summary>
    public interface IWorldCellVisualEffect
    {
        int Order { get; }
        bool TryResolve(GeneratedCell cell, out TileBase tile);
    }
}
