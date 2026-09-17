using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>Deterministic, allocation-light collection of unique structure placements.</summary>
    public sealed class StructurePlacementSet
    {
        private readonly HashSet<StructurePlacement> seen = new HashSet<StructurePlacement>();
        private readonly List<StructurePlacement> placements = new List<StructurePlacement>();

        public IReadOnlyList<StructurePlacement> Placements => placements;

        public int Count => placements.Count;

        public bool Add(StructurePlacement placement)
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
