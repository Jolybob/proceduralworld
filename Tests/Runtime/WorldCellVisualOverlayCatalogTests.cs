using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Jolybob.ProceduralWorld.Tilemap;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldCellVisualOverlayCatalogTests
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
            var catalog = new WorldCellVisualOverlayCatalog(
                new IWorldCellVisualOverlayLayer[]
                {
                    new StubLayer(20, second, true),
                    new StubLayer(10, first, true)
                });

            TileBase resolved = catalog.Resolve(new GeneratedCell(WorldTile.Deep, 0));

            Assert.AreSame(first, resolved);
        }

        [Test]
        public void UnresolvedLayerFallsThrough()
        {
            var expected = CreateTile("Expected");
            var catalog = new WorldCellVisualOverlayCatalog(
                new IWorldCellVisualOverlayLayer[]
                {
                    new StubLayer(10, null, false),
                    new StubLayer(20, expected, true)
                });

            TileBase resolved = catalog.Resolve(new GeneratedCell(WorldTile.Deep, 0));

            Assert.AreSame(expected, resolved);
        }

        [Test]
        public void NoLayerMatchReturnsNull()
        {
            var catalog = new WorldCellVisualOverlayCatalog(
                new IWorldCellVisualOverlayLayer[]
                {
                    new StubLayer(10, null, false)
                });

            TileBase resolved = catalog.Resolve(new GeneratedCell(WorldTile.Deep, 0));

            Assert.IsNull(resolved);
        }

        [Test]
        public void DuplicateOrderFailsFast()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new WorldCellVisualOverlayCatalog(
                    new IWorldCellVisualOverlayLayer[]
                    {
                        new StubLayer(10, null, false),
                        new StubLayer(10, null, false)
                    }));
        }

        [Test]
        public void NegativeOrderFailsFast()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new WorldCellVisualOverlayCatalog(
                    new IWorldCellVisualOverlayLayer[]
                    {
                        new StubLayer(-1, null, false)
                    }));
        }

        private Tile CreateTile(string name)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            createdTiles.Add(tile);
            return tile;
        }

        private sealed class StubLayer : IWorldCellVisualOverlayLayer
        {
            private readonly TileBase tile;
            private readonly bool resolves;

            public StubLayer(int order, TileBase tile, bool resolves)
            {
                Order = order;
                this.tile = tile;
                this.resolves = resolves;
            }

            public int Order { get; }

            public bool TryResolve(GeneratedCell cell, out TileBase resolvedTile)
            {
                resolvedTile = tile;
                return resolves;
            }
        }
    }
}
