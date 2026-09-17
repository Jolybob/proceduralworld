using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldTilemapRendererTests
    {
        private Tilemap tilemap;
        private readonly List<Object> createdTiles = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            var gameObject = new GameObject("Tilemap");
            tilemap = gameObject.AddComponent<Tilemap>();
        }

        [TearDown]
        public void TearDown()
        {
            if (tilemap != null)
            {
                Object.DestroyImmediate(tilemap.gameObject);
            }

            for (var i = 0; i < createdTiles.Count; i++)
            {
                if (createdTiles[i] != null)
                {
                    Object.DestroyImmediate(createdTiles[i]);
                }
            }

            createdTiles.Clear();
        }

        [Test]
        public void LoadAndUnloadMapChunk()
        {
            var deep = CreateTile("Deep");
            var empty = CreateTile("Empty");
            var tiles = new Dictionary<WorldTile, TileBase>
            {
                { WorldTile.Deep, deep },
                { WorldTile.Empty, empty }
            };
            var renderer = new WorldTilemapRenderer(tilemap, 2, tiles);
            var cells = new GeneratedCell[4]
            {
                new GeneratedCell(WorldTile.Deep, 0),
                new GeneratedCell(WorldTile.Empty, 0),
                new GeneratedCell(WorldTile.Deep, 0),
                new GeneratedCell(WorldTile.Empty, 0)
            };
            var chunk = new WorldChunk(new ChunkCoordinate(0, 0), 2, cells);

            renderer.Load(chunk);

            Assert.AreEqual(deep, tilemap.GetTile(new Vector3Int(0, 0, 0)));
            Assert.AreEqual(empty, tilemap.GetTile(new Vector3Int(1, 0, 0)));
            Assert.AreEqual(deep, tilemap.GetTile(new Vector3Int(0, 1, 0)));
            Assert.AreEqual(empty, tilemap.GetTile(new Vector3Int(1, 1, 0)));

            renderer.Unload(chunk.Coordinate);

            Assert.IsNull(tilemap.GetTile(new Vector3Int(0, 0, 0)));
            Assert.IsNull(tilemap.GetTile(new Vector3Int(1, 0, 0)));
            Assert.IsNull(tilemap.GetTile(new Vector3Int(0, 1, 0)));
            Assert.IsNull(tilemap.GetTile(new Vector3Int(1, 1, 0)));
        }

        [Test]
        public void TopologyMappingOverridesTerrainMapping()
        {
            var deep = CreateTile("Deep");
            var water = CreateTile("Water");
            var tiles = new Dictionary<WorldTile, TileBase>
            {
                { WorldTile.Deep, deep }
            };
            var topologyTiles = new Dictionary<CellTopology, TileBase>
            {
                { CellTopology.Water, water }
            };
            var renderer = new WorldTilemapRenderer(tilemap, 2, tiles, topologyTiles);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            cell.SetTopology(CellTopology.Water, WorldTile.Deep);

            renderer.Render(new WorldChunk(new ChunkCoordinate(0, 0), 2, new[] { cell }));

            Assert.AreEqual(water, tilemap.GetTile(new Vector3Int(0, 0, 0)));
        }

        [Test]
        public void MissingTopologyMappingFallsBackToTerrainMapping()
        {
            var deep = CreateTile("Deep");
            var tiles = new Dictionary<WorldTile, TileBase>
            {
                { WorldTile.Deep, deep }
            };
            var topologyTiles = new Dictionary<CellTopology, TileBase>();
            var renderer = new WorldTilemapRenderer(tilemap, 2, tiles, topologyTiles);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            cell.SetTopology(CellTopology.Water, WorldTile.Deep);

            renderer.Render(new WorldChunk(new ChunkCoordinate(0, 0), 2, new[] { cell }));

            Assert.AreEqual(deep, tilemap.GetTile(new Vector3Int(0, 0, 0)));
        }

        [Test]
        public void DirectChangeRendersTopologyWhenMapped()
        {
            var deep = CreateTile("Deep");
            var water = CreateTile("Water");
            var tiles = new Dictionary<WorldTile, TileBase>
            {
                { WorldTile.Deep, deep }
            };
            var topologyTiles = new Dictionary<CellTopology, TileBase>
            {
                { CellTopology.Water, water }
            };
            var renderer = new WorldTilemapRenderer(tilemap, 2, tiles, topologyTiles);

            renderer.Render(new WorldCellChange(
                new WorldPosition(0, 0),
                new GeneratedCell(WorldTile.Deep, 0),
                new GeneratedCell(WorldTile.Water, 0),
                WorldEditOperationKind.SetTile));

            Assert.AreEqual(water, tilemap.GetTile(new Vector3Int(0, 0, 0)));
        }

        [Test]
        public void DirectChangeUpdatesOnlyChangedCell()
        {
            var deep = CreateTile("Deep");
            var empty = CreateTile("Empty");
            var tiles = new Dictionary<WorldTile, TileBase>
            {
                { WorldTile.Deep, deep },
                { WorldTile.Empty, empty }
            };
            var renderer = new WorldTilemapRenderer(tilemap, 2, tiles);

            tilemap.SetTile(new Vector3Int(0, 0, 0), deep);
            tilemap.SetTile(new Vector3Int(1, 0, 0), deep);

            renderer.Render(new WorldCellChange(
                new WorldPosition(1, 0),
                new GeneratedCell(WorldTile.Deep, 0),
                new GeneratedCell(WorldTile.Empty, 0),
                WorldEditOperationKind.SetTile));

            Assert.AreEqual(deep, tilemap.GetTile(new Vector3Int(0, 0, 0)));
            Assert.AreEqual(empty, tilemap.GetTile(new Vector3Int(1, 0, 0)));
        }

        [Test]
        public void BatchCoalescesRepeatedPositions()
        {
            var deep = CreateTile("Deep");
            var empty = CreateTile("Empty");
            var tiles = new Dictionary<WorldTile, TileBase>
            {
                { WorldTile.Deep, deep },
                { WorldTile.Empty, empty }
            };
            var renderer = new WorldTilemapRenderer(tilemap, 2, tiles);

            renderer.RenderBatch(new[]
            {
                new WorldCellChange(
                    new WorldPosition(0, 0),
                    new GeneratedCell(WorldTile.Empty, 0),
                    new GeneratedCell(WorldTile.Deep, 0),
                    WorldEditOperationKind.SetTile),
                new WorldCellChange(
                    new WorldPosition(0, 0),
                    new GeneratedCell(WorldTile.Deep, 0),
                    new GeneratedCell(WorldTile.Empty, 0),
                    WorldEditOperationKind.SetTile)
            });

            Assert.AreEqual(empty, tilemap.GetTile(new Vector3Int(0, 0, 0)));
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
                    WorldEditOperationKind.SetTile));
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
