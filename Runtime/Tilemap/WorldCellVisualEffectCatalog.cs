using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Deterministically composes supplementary visual effects.
    /// Lower order values are evaluated first and have higher precedence.
    /// </summary>
    public sealed class WorldCellVisualEffectCatalog
    {
        private readonly IReadOnlyList<IWorldCellVisualEffect> effects;

        public WorldCellVisualEffectCatalog(IEnumerable<IWorldCellVisualEffect> effects)
        {
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));

            var ordered = new List<IWorldCellVisualEffect>();
            foreach (var effect in effects)
            {
                if (effect == null)
                    throw new ArgumentException("Visual effect collection cannot contain null entries.", nameof(effects));
                ordered.Add(effect);
            }

            ordered.Sort((left, right) => left.Order.CompareTo(right.Order));
            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i - 1].Order == ordered[i].Order)
                {
                    throw new ArgumentException(
                        $"Visual effect order '{ordered[i].Order}' is assigned more than once.",
                        nameof(effects));
                }
            }

            this.effects = ordered;
        }

        public bool TryResolve(GeneratedCell cell, out TileBase tile)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].TryResolve(cell, out tile))
                    return true;
            }

            tile = null;
            return false;
        }
    }
}
