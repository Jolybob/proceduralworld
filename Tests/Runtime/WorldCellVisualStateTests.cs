using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Tilemaps;
using UnityEngine;
using Jolybob.ProceduralWorld.Tilemap;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldCellVisualStateTests
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
        public void StoreReturnsNoneForUnknownPosition()
        {
            var store = new WorldCellVisualStateStore();
            var cell = new GeneratedCell(WorldTile.Deep, 0);

            Assert.AreEqual(WorldCellVisualState.None, store.GetState(new WorldPosition(4, -2), cell));
        }

        [Test]
        public void StoreRemovesNoneState()
        {
            var store = new WorldCellVisualStateStore();
            var position = new WorldPosition(4, -2);
            var cell = new GeneratedCell(WorldTile.Deep, 0);

            store.SetState(position, WorldCellVisualState.Selected);
            store.SetState(position, WorldCellVisualState.None);

            Assert.AreEqual(WorldCellVisualState.None, store.GetState(position, cell));
        }

        [Test]
        public void StateOverlayResolvesFromPositionAwareState()
        {
            var expected = CreateTile("Selected");
            var store = new WorldCellVisualStateStore();
            var position = new WorldPosition(7, 9);
            store.SetState(position, WorldCellVisualState.Selected);
            var layer = new WorldCellVisualStateOverlayLayer(
                store,
                new Dictionary<WorldCellVisualState, TileBase> { { WorldCellVisualState.Selected, expected } });
            var cell = new GeneratedCell(WorldTile.Deep, 0);

            TileBase tile;
            Assert.IsTrue(layer.TryResolve(position, cell, out tile));
            Assert.AreSame(expected, tile);
        }

        [Test]
        public void StateOverlayFallsThroughWhenStateIsUnmapped()
        {
            var store = new WorldCellVisualStateStore();
            var position = new WorldPosition(7, 9);
            store.SetState(position, WorldCellVisualState.Hovered);
            var layer = new WorldCellVisualStateOverlayLayer(
                store,
                new Dictionary<WorldCellVisualState, TileBase>());
            var cell = new GeneratedCell(WorldTile.Deep, 0);

            TileBase tile;
            Assert.IsFalse(layer.TryResolve(position, cell, out tile));
            Assert.IsNull(tile);
        }

        [Test]
        public void ContextCatalogUsesPositionAwareLayerWithoutBreakingLegacyLayers()
        {
            var expected = CreateTile("Selected");
            var store = new WorldCellVisualStateStore();
            var position = new WorldPosition(3, 5);
            store.SetState(position, WorldCellVisualState.Selected);
            var catalog = new WorldCellVisualOverlayCatalog(new IWorldCellVisualOverlayLayer[]
            {
                new WorldCellVisualStateOverlayLayer(
                    store,
                    new Dictionary<WorldCellVisualState, TileBase> { { WorldCellVisualState.Selected, expected } })
            });
            var cell = new GeneratedCell(WorldTile.Deep, 0);

            Assert.AreSame(expected, catalog.Resolve(position, cell));
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
