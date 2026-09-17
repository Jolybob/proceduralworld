using System;
using System.Collections.Generic;
using UnityEngine;
using UnityTilemap = UnityEngine.Tilemaps.Tilemap;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Concrete Unity Tilemap presentation adapter for streamed chunks and canonical world-change notifications.
    /// It can be used as an <see cref="IWorldChunkSink"/> and an <see cref="IWorldChangeRenderer"/>.
    /// </summary>
    public sealed class WorldTilemapRenderer : IWorldChunkSink, IWorldChangeRenderer
    {
        private readonly UnityTilemap tilemap;
        private readonly int chunkSize;
        private readonly IReadOnlyDictionary<WorldTile, TileBase> tiles;

        public UnityTilemap Tilemap => tilemap;
        public int ChunkSize => chunkSize;

        public WorldTilemapRenderer(
            UnityTilemap tilemap,
            int chunkSize,
            IReadOnlyDictionary<WorldTile, TileBase> tiles)
        {
            this.tilemap = tilemap ?? throw new ArgumentNullException(nameof(tilemap));
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            this.tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
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
                    GeneratedCell cell = chunk.GetCell(x, y);
                    positions[index] = ToTilemapPosition(coordinate, x, y);
                    tileBases[index] = ResolveTile(cell.Tile);
                    index++;
                }
            }

            tilemap.SetTiles(positions, tileBases);
        }

        public void Unload(ChunkCoord coordinate)
        {
            var bounds = new BoundsInt(
                coordinate.X * chunkSize,
                coordinate.Y * chunkSize,
                0,
                chunkSize,
                chunkSize,
                1);
            tilemap.SetTilesBlock(bounds, new TileBase[chunkSize * chunkSize]);
        }

        public void Render(WorldCellChange change)
        {
            tilemap.SetTile(ToTilemapPosition(change.Position), ResolveTile(change.After.Tile));
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
                TileBase tile = ResolveTile(change.After.Tile);

                if (!latestTiles.ContainsKey(position))
                    positions.Add(position);

                latestTiles[position] = tile;
            }

            var tileBases = new TileBase[positions.Count];
            for (int i = 0; i < positions.Count; i++)
                tileBases[i] = latestTiles[positions[i]];

            tilemap.SetTiles(positions.ToArray(), tileBases);
        }

        private TileBase ResolveTile(WorldTile tile)
        {
            TileBase result;
            if (!tiles.TryGetValue(tile, out result))
            {
                throw new KeyNotFoundException(
                    $"No TileBase mapping exists for world tile '{tile}'.");
            }

            return result;
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
