using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Jolybob.ProceduralWorld.Tilemap;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldCellVisualOverlayChannelTests
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
        public void DifferentChannelsResolveSimultaneously()
        {
            var selected = CreateTile("Selected");
            var damaged = CreateTile("Damaged");
            var selectedChannel = new WorldCellVisualOverlayChannel(1);
            var damagedChannel = new WorldCellVisualOverlayChannel(2);
            var store = new WorldCellVisualStateStore();
            var position = new WorldPosition(8, -3);
            store.SetState(position, WorldCellVisualState.Selected | WorldCellVisualState.Damaged);

            var catalog = new WorldCellVisualOverlayChannelCatalog(new IWorldCellVisualOverlayChannelLayer[]
            {
                new WorldCellVisualStateOverlayLayer(
                    store,
                    new Dictionary<WorldCellVisualState, TileBase> { { WorldCellVisualState.Selected, selected } },
                    selectedChannel),
                new WorldCellVisualStateOverlayLayer(
                    store,
                    new Dictionary<WorldCellVisualState, TileBase> { { WorldCellVisualState.Damaged, damaged } },
                    damagedChannel)
            });

            var resolved = catalog.Resolve(position, new GeneratedCell(WorldTile.Deep, 0));

            Assert.AreSame(selected, resolved[selectedChannel]);
            Assert.AreSame(damaged, resolved[damagedChannel]);
        }

        [Test]
        public void MultipleLayersInOneChannelUseOrder()
        {
            var first = CreateTile("First");
            var second = CreateTile("Second");
            var channel = new WorldCellVisualOverlayChannel(3);
            var store = new WorldCellVisualStateStore();
            var position = new WorldPosition(1, 2);
            store.SetState(position, WorldCellVisualState.Selected);

            var catalog = new WorldCellVisualOverlayChannelCatalog(new IWorldCellVisualOverlayChannelLayer[]
            {
                new WorldCellVisualStateOverlayLayer(
                    store,
                    new Dictionary<WorldCellVisualState, TileBase> { { WorldCellVisualState.Selected, second } },
                    channel,
                    20),
                new WorldCellVisualStateOverlayLayer(
                    store,
                    new Dictionary<WorldCellVisualState, TileBase> { { WorldCellVisualState.Selected, first } },
                    channel,
                    10)
            });

            Assert.AreSame(first, catalog.Resolve(position, new GeneratedCell(WorldTile.Deep, 0))[channel]);
        }

        [Test]
        public void UnresolvedChannelIsOmitted()
        {
            var channel = new WorldCellVisualOverlayChannel(4);
            var store = new WorldCellVisualStateStore();
            var position = new WorldPosition(0, 0);
            store.SetState(position, WorldCellVisualState.Hovered);
            var catalog = new WorldCellVisualOverlayChannelCatalog(new IWorldCellVisualOverlayChannelLayer[]
            {
                new WorldCellVisualStateOverlayLayer(
                    store,
                    new Dictionary<WorldCellVisualState, TileBase>(),
                    channel)
            });

            Assert.IsFalse(catalog.Resolve(position, new GeneratedCell(WorldTile.Deep, 0)).ContainsKey(channel));
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
