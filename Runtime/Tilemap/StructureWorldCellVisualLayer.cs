using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Resolves structure-specific visuals when a generated cell contains a structure.
    /// Unmapped or structure-free cells fall through to the next visual layer.
    /// </summary>
    public sealed class StructureWorldCellVisualLayer : IWorldCellVisualLayer
    {
        private readonly IReadOnlyDictionary<StructureId, TileBase> tiles;

        public StructureWorldCellVisualLayer(IReadOnlyDictionary<StructureId, TileBase> tiles, int order = 300)
        {
            this.tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            if (order < 0)
                throw new ArgumentOutOfRangeException(nameof(order));
            Order = order;
        }

        public int Order { get; }

        public bool TryResolve(GeneratedCell cell, out TileBase tile)
        {
            tile = null;
            if ((cell.Flags & GeneratedCellFlags.HasStructure) == 0)
                return false;

            return tiles.TryGetValue(cell.Structure, out tile);
        }
    }
}
