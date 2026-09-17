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
            WorldCellVisualState state = stateProvider.GetState(position, cell);
            if (tiles.TryGetValue(state, out tile))
                return true;

            WorldCellVisualState bestState = WorldCellVisualState.None;
            TileBase bestTile = null;
            int bestBitCount = -1;

            foreach (var pair in tiles)
            {
                if (pair.Key == WorldCellVisualState.None || (state & pair.Key) != pair.Key)
                    continue;

                int bitCount = CountBits((byte)pair.Key);
                if (bitCount > bestBitCount ||
                    (bitCount == bestBitCount && pair.Key.CompareTo(bestState) < 0))
                {
                    bestState = pair.Key;
                    bestTile = pair.Value;
                    bestBitCount = bitCount;
                }
            }

            tile = bestTile;
            return bestBitCount >= 0;
        }

        private static int CountBits(byte value)
        {
            int count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }
    }
}
