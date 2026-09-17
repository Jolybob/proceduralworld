using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>Deterministic, allocation-light collection of unique world feature placements.</summary>
    public sealed class WorldFeaturePlacementSet
    {
        private readonly HashSet<WorldFeaturePlacement> seen = new HashSet<WorldFeaturePlacement>();
        private readonly List<WorldFeaturePlacement> placements = new List<WorldFeaturePlacement>();

        public IReadOnlyList<WorldFeaturePlacement> Placements => placements;

        public int Count => placements.Count;

        public bool Add(WorldFeaturePlacement placement)
        {
            if (!seen.Add(placement))
                return false;

            placements.Add(placement);
            return true;
        }

        public void Clear()
        {
            seen.Clear();
            placements.Clear();
        }
    }
}
