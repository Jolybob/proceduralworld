using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Resolves resource-specific visuals when a generated cell contains a resource.
    /// Unmapped or resource-free cells fall through to the next visual layer.
    /// </summary>
    public sealed class ResourceWorldCellVisualLayer : IWorldCellVisualLayer
    {
        private readonly IReadOnlyDictionary<ResourceId, TileBase> tiles;

        public ResourceWorldCellVisualLayer(IReadOnlyDictionary<ResourceId, TileBase> tiles, int order = 200)
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
            if ((cell.Flags & GeneratedCellFlags.HasResource) == 0)
                return false;

            return tiles.TryGetValue(cell.Resource, out tile);
        }
    }
}
