using System;
using System.Collections.Generic;
using UnityEngine;
using UnityTilemap = UnityEngine.Tilemaps.Tilemap;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Renders multiple supplementary visual channels simultaneously on dedicated Tilemaps.
    /// </summary>
    public sealed class WorldTilemapOverlayChannelRenderer : IWorldChunkSink, IWorldChangeRenderer
    {
        private readonly IReadOnlyDictionary<WorldCellVisualOverlayChannel, UnityTilemap> tilemaps;
        private readonly int chunkSize;
        private readonly WorldCellVisualOverlayChannelCatalog catalog;

        public int ChunkSize => chunkSize;
        public WorldCellVisualOverlayChannelCatalog Catalog => catalog;

        public WorldTilemapOverlayChannelRenderer(
            IReadOnlyDictionary<WorldCellVisualOverlayChannel, UnityTilemap> tilemaps,
            int chunkSize,
            WorldCellVisualOverlayChannelCatalog catalog)
        {
            this.tilemaps = tilemaps ?? throw new ArgumentNullException(nameof(tilemaps));
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.chunkSize = chunkSize;

            foreach (var pair in tilemaps)
            {
                if (pair.Value == null)
                    throw new ArgumentException("Overlay channel Tilemap collection cannot contain null values.", nameof(tilemaps));
            }
        }

        public void Load(ChunkCoord coordinate, GeneratedChunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));
            if (chunk.Size != chunkSize)
                throw new ArgumentException("The generated chunk size does not match the renderer chunk size.", nameof(chunk));

            ClearChunk(coordinate);
            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                    RenderCell(ToTilemapPosition(coordinate, x, y), ToWorldPosition(coordinate, x, y), chunk.GetCell(x, y));
            }
        }

        public void Unload(ChunkCoord coordinate) => ClearChunk(coordinate);

        public void Render(WorldCellChange change)
        {
            RenderCell(ToTilemapPosition(change.Position), change.Position, change.After);
        }

        public void RenderBatch(WorldChangeBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));
            if (batch.Count == 0)
                return;

            var latest = new Dictionary<WorldPosition, GeneratedCell>();
            var positions = new List<WorldPosition>(batch.Count);
            for (int i = 0; i < batch.Changes.Count; i++)
            {
                WorldCellChange change = batch.Changes[i];
                if (!latest.ContainsKey(change.Position))
                    positions.Add(change.Position);
                latest[change.Position] = change.After;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                WorldPosition position = positions[i];
                RenderCell(ToTilemapPosition(position), position, latest[position]);
            }
        }

        private void RenderCell(Vector3Int tilemapPosition, WorldPosition worldPosition, GeneratedCell cell)
        {
            var resolved = catalog.Resolve(worldPosition, cell);
            foreach (var pair in tilemaps)
            {
                TileBase tile;
                pair.Value.SetTile(tilemapPosition, resolved.TryGetValue(pair.Key, out tile) ? tile : null);
            }
        }

        private void ClearChunk(ChunkCoord coordinate)
        {
            var bounds = new BoundsInt(
                checked(coordinate.X * chunkSize), checked(coordinate.Y * chunkSize), 0,
                chunkSize, chunkSize, 1);
            foreach (var pair in tilemaps)
                pair.Value.SetTilesBlock(bounds, new TileBase[chunkSize * chunkSize]);
        }

        private Vector3Int ToTilemapPosition(ChunkCoord coordinate, int localX, int localY)
        {
            return new Vector3Int(checked(coordinate.X * chunkSize + localX), checked(coordinate.Y * chunkSize + localY), 0);
        }

        private WorldPosition ToWorldPosition(ChunkCoord coordinate, int localX, int localY)
        {
            return new WorldPosition(checked(coordinate.X * chunkSize + localX), checked(coordinate.Y * chunkSize + localY));
        }

        private static Vector3Int ToTilemapPosition(WorldPosition position) => new Vector3Int(position.X, position.Y, 0);
    }
}
