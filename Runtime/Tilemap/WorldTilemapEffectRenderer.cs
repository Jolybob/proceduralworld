using System;
using System.Collections.Generic;
using UnityEngine;
using UnityTilemap = UnityEngine.Tilemaps.Tilemap;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Renders supplementary cell visuals into a dedicated Tilemap without replacing base presentation.
    /// </summary>
    public sealed class WorldTilemapEffectRenderer : IWorldChunkSink, IWorldChangeRenderer
    {
        private readonly UnityTilemap tilemap;
        private readonly int chunkSize;
        private readonly WorldCellVisualEffectCatalog effectCatalog;

        public UnityTilemap Tilemap => tilemap;
        public int ChunkSize => chunkSize;
        public WorldCellVisualEffectCatalog EffectCatalog => effectCatalog;

        public WorldTilemapEffectRenderer(
            UnityTilemap tilemap,
            int chunkSize,
            WorldCellVisualEffectCatalog effectCatalog)
        {
            this.tilemap = tilemap ?? throw new ArgumentNullException(nameof(tilemap));
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            this.effectCatalog = effectCatalog ?? throw new ArgumentNullException(nameof(effectCatalog));
            this.chunkSize = chunkSize;
        }

        public void Load(ChunkCoord coordinate, GeneratedChunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));
            if (chunk.Size != chunkSize)
            {
                throw new ArgumentException(
                    "The generated chunk size does not match the renderer chunk size.",
                    nameof(chunk));
            }

            var positions = new Vector3Int[chunkSize * chunkSize];
            var tileBases = new TileBase[chunkSize * chunkSize];
            int index = 0;

            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    positions[index] = ToTilemapPosition(coordinate, x, y);
                    TileBase tile;
                    tileBases[index] = effectCatalog.TryResolve(chunk.GetCell(x, y), out tile) ? tile : null;
                    index++;
                }
            }

            tilemap.SetTiles(positions, tileBases);
        }

        public void Unload(ChunkCoord coordinate)
        {
            var bounds = new BoundsInt(
                checked(coordinate.X * chunkSize),
                checked(coordinate.Y * chunkSize),
                0,
                chunkSize,
                chunkSize,
                1);
            tilemap.SetTilesBlock(bounds, new TileBase[chunkSize * chunkSize]);
        }

        public void Render(WorldCellChange change)
        {
            TileBase tile;
            tilemap.SetTile(
                ToTilemapPosition(change.Position),
                effectCatalog.TryResolve(change.After, out tile) ? tile : null);
        }

        public void RenderBatch(WorldChangeBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));
            if (batch.Count == 0)
                return;

            var positions = new List<Vector3Int>(batch.Count);
            var latestTiles = new Dictionary<Vector3Int, TileBase>();

            for (int i = 0; i < batch.Changes.Count; i++)
            {
                WorldCellChange change = batch.Changes[i];
                Vector3Int position = ToTilemapPosition(change.Position);
                TileBase tile;
                TileBase resolved = effectCatalog.TryResolve(change.After, out tile) ? tile : null;

                if (!latestTiles.ContainsKey(position))
                    positions.Add(position);

                latestTiles[position] = resolved;
            }

            var tileBases = new TileBase[positions.Count];
            for (int i = 0; i < positions.Count; i++)
                tileBases[i] = latestTiles[positions[i]];

            tilemap.SetTiles(positions.ToArray(), tileBases);
        }

        private static Vector3Int ToTilemapPosition(WorldPosition position)
        {
            return new Vector3Int(position.X, position.Y, 0);
        }

        private Vector3Int ToTilemapPosition(ChunkCoord coordinate, int localX, int localY)
        {
            return new Vector3Int(
                checked(coordinate.X * chunkSize + localX),
                checked(coordinate.Y * chunkSize + localY),
                0);
        }
    }
}
