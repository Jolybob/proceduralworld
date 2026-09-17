using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Composes independent overlay channels while preserving deterministic layer ordering.
    /// </summary>
    public sealed class WorldCellVisualOverlayChannelCatalog
    {
        private readonly IReadOnlyDictionary<WorldCellVisualOverlayChannel, WorldCellVisualOverlayCatalog> channels;

        public WorldCellVisualOverlayChannelCatalog(IEnumerable<IWorldCellVisualOverlayChannelLayer> layers)
        {
            if (layers == null)
                throw new ArgumentNullException(nameof(layers));

            var grouped = new Dictionary<WorldCellVisualOverlayChannel, List<IWorldCellVisualOverlayLayer>>();
            foreach (var layer in layers)
            {
                if (layer == null)
                    throw new ArgumentException("Overlay channel layer collection cannot contain null entries.", nameof(layers));

                List<IWorldCellVisualOverlayLayer> channelLayers;
                if (!grouped.TryGetValue(layer.Channel, out channelLayers))
                {
                    channelLayers = new List<IWorldCellVisualOverlayLayer>();
                    grouped.Add(layer.Channel, channelLayers);
                }

                channelLayers.Add(layer);
            }

            var catalogs = new Dictionary<WorldCellVisualOverlayChannel, WorldCellVisualOverlayCatalog>();
            foreach (var pair in grouped)
                catalogs.Add(pair.Key, new WorldCellVisualOverlayCatalog(pair.Value));

            channels = catalogs;
        }

        public IReadOnlyDictionary<WorldCellVisualOverlayChannel, TileBase> Resolve(WorldPosition position, GeneratedCell cell)
        {
            var result = new Dictionary<WorldCellVisualOverlayChannel, TileBase>();
            foreach (var pair in channels)
            {
                TileBase tile = pair.Value.Resolve(position, cell);
                if (tile != null)
                    result.Add(pair.Key, tile);
            }

            return result;
        }
    }
}
