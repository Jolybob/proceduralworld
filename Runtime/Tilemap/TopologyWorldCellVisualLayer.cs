using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Resolves topology-specific visuals while allowing unmapped topology states to fall through.
    /// </summary>
    public sealed class TopologyWorldCellVisualLayer : IWorldCellVisualLayer
    {
        private readonly IReadOnlyDictionary<CellTopology, TileBase> tiles;

        public TopologyWorldCellVisualLayer(
            IReadOnlyDictionary<CellTopology, TileBase> tiles,
            int order = 100)
        {
            this.tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            if (order < 0)
                throw new ArgumentOutOfRangeException(nameof(order));
            Order = order;
        }

        public int Order { get; }

        public bool TryResolve(GeneratedCell cell, out TileBase tile)
        {
            return tiles.TryGetValue(cell.Topology, out tile);
        }
    }
}
