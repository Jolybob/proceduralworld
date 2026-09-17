using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Ordered composition of optional visual layers followed by a required fallback resolver.
    /// </summary>
    public sealed class WorldCellVisualLayerCatalog : IWorldCellVisualResolver
    {
        private readonly IReadOnlyList<IWorldCellVisualLayer> layers;
        private readonly IWorldCellVisualResolver fallback;

        public WorldCellVisualLayerCatalog(
            IEnumerable<IWorldCellVisualLayer> layers,
            IWorldCellVisualResolver fallback)
        {
            if (layers == null)
                throw new ArgumentNullException(nameof(layers));
            this.fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));

            var ordered = new List<IWorldCellVisualLayer>();
            foreach (var layer in layers)
            {
                if (layer == null)
                    throw new ArgumentException("Visual layer collection cannot contain null entries.", nameof(layers));
                ordered.Add(layer);
            }

            ordered.Sort((left, right) => left.Order.CompareTo(right.Order));
            this.layers = ordered;
        }

        public TileBase Resolve(GeneratedCell cell)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                TileBase tile;
                if (layers[i].TryResolve(cell, out tile))
                    return tile;
            }

            return fallback.Resolve(cell);
        }
    }
}
