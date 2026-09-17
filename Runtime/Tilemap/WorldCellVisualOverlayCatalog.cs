using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Deterministically composes supplementary visual overlay layers.
    /// Lower order values are evaluated first and therefore have higher precedence.
    /// </summary>
    public sealed class WorldCellVisualOverlayCatalog
    {
        private readonly IReadOnlyList<IWorldCellVisualOverlayLayer> layers;

        public WorldCellVisualOverlayCatalog(IEnumerable<IWorldCellVisualOverlayLayer> layers)
        {
            if (layers == null)
                throw new ArgumentNullException(nameof(layers));

            var ordered = new List<IWorldCellVisualOverlayLayer>();
            foreach (var layer in layers)
            {
                if (layer == null)
                    throw new ArgumentException("Visual overlay layer collection cannot contain null entries.", nameof(layers));
                if (layer.Order < 0)
                    throw new ArgumentException("Visual overlay layer order cannot be negative.", nameof(layers));
                ordered.Add(layer);
            }

            ordered.Sort((left, right) => left.Order.CompareTo(right.Order));
            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i - 1].Order == ordered[i].Order)
                    throw new ArgumentException($"Visual overlay layer order '{ordered[i].Order}' is assigned more than once.", nameof(layers));
            }

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

            return null;
        }

        public TileBase Resolve(WorldPosition position, GeneratedCell cell)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                TileBase tile;
                var contextualLayer = layers[i] as IWorldCellVisualOverlayContextLayer;
                if (contextualLayer != null)
                {
                    if (contextualLayer.TryResolve(position, cell, out tile))
                        return tile;
                }
                else if (layers[i].TryResolve(cell, out tile))
                {
                    return tile;
                }
            }

            return null;
        }
    }
}
