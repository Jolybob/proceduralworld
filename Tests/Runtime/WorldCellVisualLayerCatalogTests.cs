using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Jolybob.ProceduralWorld.Tilemap;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldCellVisualLayerCatalogTests
    {
        private readonly List<Tile> createdTiles = new List<Tile>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdTiles.Count; i++)
                UnityEngine.Object.DestroyImmediate(createdTiles[i]);
            createdTiles.Clear();
        }

        [Test]
        public void LayersAreEvaluatedByOrder()
        {
            var first = CreateTile("First");
            var second = CreateTile("Second");
            var fallback = new WorldCellVisualCatalog(
                new Dictionary<WorldTile, TileBase> { { WorldTile.Deep, CreateTile("Fallback") } });

            var catalog = new WorldCellVisualLayerCatalog(
                new IWorldCellVisualLayer[]
                {
                    new StubLayer(20, second),
                    new StubLayer(10, first)
                },
                fallback);

            Assert.AreSame(first, catalog.Resolve(new GeneratedCell(WorldTile.Deep, 0)));
        }

        [Test]
        public void UnresolvedLayerFallsThroughToNextLayer()
        {
            var expected = CreateTile("Expected");
            var fallback = new WorldCellVisualCatalog(
                new Dictionary<WorldTile, TileBase> { { WorldTile.Deep, CreateTile("Fallback") } });

            var catalog = new WorldCellVisualLayerCatalog(
                new IWorldCellVisualLayer[] { new StubLayer(10, null, false), new StubLayer(20, expected) },
                fallback);

            Assert.AreSame(expected, catalog.Resolve(new GeneratedCell(WorldTile.Deep, 0)));
        }

        [Test]
        public void NoLayerMatchUsesFallbackResolver()
        {
            var expected = CreateTile("Fallback");
            var fallback = new WorldCellVisualCatalog(
                new Dictionary<WorldTile, TileBase> { { WorldTile.Deep, expected } });
            var catalog = new WorldCellVisualLayerCatalog(
                new IWorldCellVisualLayer[] { new StubLayer(10, null, false) },
                fallback);

            Assert.AreSame(expected, catalog.Resolve(new GeneratedCell(WorldTile.Deep, 0)));
        }

        [Test]
        public void DuplicateLayerOrderFailsFast()
        {
            var fallback = new WorldCellVisualCatalog(
                new Dictionary<WorldTile, TileBase> { { WorldTile.Deep, CreateTile("Fallback") } });

            Assert.Throws<ArgumentException>(() => new WorldCellVisualLayerCatalog(
                new IWorldCellVisualLayer[]
                {
                    new StubLayer(10, CreateTile("First")),
                    new StubLayer(10, CreateTile("Second"))
                },
                fallback));
        }

        [Test]
        public void ResourceLayerResolvesOnlyFlaggedMappedResources()
        {
            var expected = CreateTile("Resource");
            var layer = new ResourceWorldCellVisualLayer(
                new Dictionary<ResourceId, TileBase> { { new ResourceId(7), expected } });

            var cell = new GeneratedCell(WorldTile.Deep, 0);
            TileBase tile;
            Assert.IsFalse(layer.TryResolve(cell, out tile));

            cell.SetResource(new ResourceId(7));
            Assert.IsTrue(layer.TryResolve(cell, out tile));
            Assert.AreSame(expected, tile);
        }

        [Test]
        public void UnmappedResourceFallsThrough()
        {
            var layer = new ResourceWorldCellVisualLayer(
                new Dictionary<ResourceId, TileBase> { { new ResourceId(7), CreateTile("Resource") } });
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            cell.SetResource(new ResourceId(8));

            TileBase tile;
            Assert.IsFalse(layer.TryResolve(cell, out tile));
            Assert.IsNull(tile);
        }

        [Test]
        public void StructureLayerResolvesOnlyFlaggedMappedStructures()
        {
            var expected = CreateTile("Structure");
            var layer = new StructureWorldCellVisualLayer(
                new Dictionary<StructureId, TileBase> { { new StructureId(7), expected } });

            var cell = new GeneratedCell(WorldTile.Deep, 0);
            TileBase tile;
            Assert.IsFalse(layer.TryResolve(cell, out tile));

            cell.SetStructure(new StructureId(7));
            Assert.IsTrue(layer.TryResolve(cell, out tile));
            Assert.AreSame(expected, tile);
        }

        [Test]
        public void UnmappedStructureFallsThrough()
        {
            var layer = new StructureWorldCellVisualLayer(
                new Dictionary<StructureId, TileBase> { { new StructureId(7), CreateTile("Structure") } });
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            cell.SetStructure(new StructureId(8));

            TileBase tile;
            Assert.IsFalse(layer.TryResolve(cell, out tile));
            Assert.IsNull(tile);
        }

        private Tile CreateTile(string name)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            createdTiles.Add(tile);
            return tile;
        }

        private sealed class StubLayer : IWorldCellVisualLayer
        {
            private readonly TileBase tile;
            private readonly bool resolves;

            public StubLayer(int order, TileBase tile, bool resolves = true)
            {
                Order = order;
                this.tile = tile;
                this.resolves = resolves;
            }

            public int Order { get; }

            public bool TryResolve(GeneratedCell cell, out TileBase result)
            {
                result = tile;
                return resolves;
            }
        }
    }
}
