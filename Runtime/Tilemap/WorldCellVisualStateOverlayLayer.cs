using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    public sealed class WorldCellVisualStateOverlayLayer : IWorldCellVisualOverlayContextLayer, IWorldCellVisualOverlayChannelLayer
    {
        private readonly IWorldCellVisualStateProvider stateProvider;
        private readonly IReadOnlyDictionary<WorldCellVisualState, TileBase> tiles;

        public WorldCellVisualStateOverlayLayer(
            IWorldCellVisualStateProvider stateProvider,
            IReadOnlyDictionary<WorldCellVisualState, TileBase> tiles,
            int order = 100)
            : this(stateProvider, tiles, new WorldCellVisualOverlayChannel(0), order)
        {
        }

        public WorldCellVisualStateOverlayLayer(
            IWorldCellVisualStateProvider stateProvider,
            IReadOnlyDictionary<WorldCellVisualState, TileBase> tiles,
            WorldCellVisualOverlayChannel channel,
            int order = 100)
        {
            this.stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            this.tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            if (order < 0)
                throw new ArgumentOutOfRangeException(nameof(order));
            Channel = channel;
            Order = order;
        }

        public int Order { get; }
        public WorldCellVisualOverlayChannel Channel { get; }

        public bool TryResolve(GeneratedCell cell, out TileBase tile)
        {
            tile = null;
            return false;
        }

        public bool TryResolve(WorldPosition position, GeneratedCell cell, out TileBase tile)
        {
            return tiles.TryGetValue(stateProvider.GetState(position, cell), out tile);
        }
    }
}
