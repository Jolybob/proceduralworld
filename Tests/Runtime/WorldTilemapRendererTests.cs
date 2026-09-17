using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityTilemap = UnityEngine.Tilemaps.Tilemap;
using UnityEngine.Tilemaps;
using Jolybob.ProceduralWorld;
using Jolybob.ProceduralWorld.Tilemap;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldTilemapRendererTests
    {
        private GameObject gameObject;
        private UnityTilemap tilemap;
        private Dictionary<WorldTile, TileBase> tiles;
        private readonly List<Tile> createdTiles = new List<Tile>();

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("WorldTilemapRendererTests");
            tilemap = gameObject.AddComponent<UnityTilemap>();

            tiles = new Dictionary<WorldTile, TileBase>
            {
                { WorldTile.Empty, CreateTile("Empty") },
                { WorldTile.Deep, CreateTile("Deep") },
                { WorldTile.Mid, CreateTile("Mid") },
                { WorldTile.Inner, CreateTile("Inner") },
                { WorldTile.Core, CreateTile("Core") }
            };
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdTiles.Count; i++)
                Object.DestroyImmediate(createdTiles[i]);

            createdTiles.Clear();

            if (gameObject != null)
                Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void LoadAndUnloadUseChunkCoordinateAndChunkSize()
        {
            var renderer = new WorldTilemapRenderer(tilemap, 2, tiles);
            var chunk = new GeneratedChunk(new ChunkCoord(-1, 2), 2);
            chunk.SetCell(0, 0, new GeneratedCell(WorldTile.Core, 0));
            chunk.SetCell(1, 0, new GeneratedCell(WorldTile.Inner, 0));
            chunk.SetCell(0, 1, new GeneratedCell(WorldTile.Mid, 0));
            chunk.SetCell(1, 1, new GeneratedCell(WorldTile.Deep, 0));

            renderer.Load(chunk.Coordinate, chunk);

            Assert.AreSame(tiles[WorldTile.Core], tilemap.GetTile(new Vector3Int(-2, 4, 0)));
            Assert.AreSame(tiles[WorldTile.Inner], tilemap.GetTile(new Vector3Int(-1, 4, 0)));
            Assert.AreSame(tiles[WorldTile.Mid], tilemap.GetTile(new Vector3Int(-2, 5, 0)));
            Assert.AreSame(tiles[WorldTile.Deep], tilemap.GetTile(new Vector3Int(-1, 5, 0)));

            renderer.Unload(chunk.Coordinate);

            Assert.IsNull(tilemap.GetTile(new Vector3Int(-2, 4, 0)));
            Assert.IsNull(tilemap.GetTile(new Vector3Int(-1, 5, 0)));
        }

        [Test]
        public void DirectChangeUpdatesOnlyTheChangedCell()
        {
            var renderer = new WorldTilemapRenderer(tilemap, 2, tiles);
            tilemap.SetTile(new Vector3Int(3, -2, 0), tiles[WorldTile.Deep]);
            tilemap.SetTile(new Vector3Int(4, -2, 0), tiles[WorldTile.Inner]);

            var before = new GeneratedCell(WorldTile.Deep, 0);
            var after = new GeneratedCell(WorldTile.Core, 0);
            renderer.Render(new WorldCellChange(
                new WorldPosition(3, -2),
                before,
                after,
                WorldEditOperationKind.SetTile));

            Assert.AreSame(tiles[WorldTile.Core], tilemap.GetTile(new Vector3Int(3, -2, 0)));
            Assert.AreSame(tiles[WorldTile.Inner], tilemap.GetTile(new Vector3Int(4, -2, 0)));
        }

        [Test]
        public void BatchCoalescesRepeatedPositionsToLatestState()
        {
            var renderer = new WorldTilemapRenderer(tilemap, 2, tiles);
            var before = new GeneratedCell(WorldTile.Deep, 0);
            var inner = new GeneratedCell(WorldTile.Inner, 0);
            var core = new GeneratedCell(WorldTile.Core, 0);

            var changes = new List<WorldCellChange>
            {
                new WorldCellChange(
                    new WorldPosition(1, 1),
                    before,
                    inner,
                    WorldEditOperationKind.SetTile),
                new WorldCellChange(
                    new WorldPosition(2, 1),
                    before,
                    core,
                    WorldEditOperationKind.SetTile),
                new WorldCellChange(
                    new WorldPosition(1, 1),
                    inner,
                    core,
                    WorldEditOperationKind.SetTile)
            };

            renderer.RenderBatch(new WorldChangeBatch(changes));

            Assert.AreSame(tiles[WorldTile.Core], tilemap.GetTile(new Vector3Int(1, 1, 0)));
            Assert.AreSame(tiles[WorldTile.Core], tilemap.GetTile(new Vector3Int(2, 1, 0)));
        }

        [Test]
        public void UnknownWorldTileFailsFast()
        {
            var renderer = new WorldTilemapRenderer(tilemap, 2, new Dictionary<WorldTile, TileBase>());

            Assert.Throws<KeyNotFoundException>(() =>
                renderer.Render(new WorldCellChange(
                    new WorldPosition(0, 0),
                    new GeneratedCell(WorldTile.Deep, 0),
                    new GeneratedCell(WorldTile.Core, 0),
                    WorldEditOperationKind.SetTile)));
        }

        private Tile CreateTile(string name)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            createdTiles.Add(tile);
            return tile;
        }
    }
}
