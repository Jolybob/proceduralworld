using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Dictionary-backed cell visual resolver with topology-first, terrain-fallback resolution.
    /// The renderer can depend on this abstraction while future visual layers can be introduced
    /// without changing the Tilemap adapter.
    /// </summary>
    public sealed class WorldCellVisualCatalog : IWorldCellVisualResolver
    {
        private readonly IReadOnlyDictionary<WorldTile, TileBase> terrainTiles;
        private readonly IReadOnlyDictionary<CellTopology, TileBase> topologyTiles;

        public WorldCellVisualCatalog(
            IReadOnlyDictionary<WorldTile, TileBase> terrainTiles,
            IReadOnlyDictionary<CellTopology, TileBase> topologyTiles = null)
        {
            this.terrainTiles = terrainTiles ?? throw new ArgumentNullException(nameof(terrainTiles));
            this.topologyTiles = topologyTiles;
        }

        public TileBase Resolve(GeneratedCell cell)
        {
            if (topologyTiles != null && topologyTiles.TryGetValue(cell.Topology, out var topologyTile))
                return topologyTile;

            if (terrainTiles.TryGetValue(cell.Tile, out var terrainTile))
                return terrainTile;

            throw new KeyNotFoundException(
                $"No TileBase mapping exists for world tile '{cell.Tile}' and topology '{cell.Topology}'.");
        }
    }
}
