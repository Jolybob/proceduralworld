using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public sealed class WorldPresentationRegionDemandAggregator
    {
        private readonly Dictionary<int, List<int>> sourceRegions = new Dictionary<int, List<int>>();
        private readonly List<int> sourceOrder = new List<int>();
        private readonly List<int> aggregatedRegions = new List<int>();
        private readonly HashSet<int> aggregatedSet = new HashSet<int>();
        private bool dirty = true;

        public int SourceCount => sourceOrder.Count;

        public IReadOnlyList<int> DemandedRegions
        {
            get
            {
                RebuildIfNeeded();
                return aggregatedRegions.AsReadOnly();
            }
        }

        public bool HasSource(int sourceId)
        {
            return sourceRegions.ContainsKey(sourceId);
        }

        public void SetSourceDemand(int sourceId, IEnumerable<int> regionIds)
        {
            if (regionIds == null)
            {
                throw new ArgumentNullException(nameof(regionIds));
            }

            if (!sourceRegions.TryGetValue(sourceId, out var regions))
            {
                regions = new List<int>();
                sourceRegions.Add(sourceId, regions);
                sourceOrder.Add(sourceId);
            }

            regions.Clear();
            var unique = new HashSet<int>();
            foreach (var regionId in regionIds)
            {
                if (unique.Add(regionId))
                {
                    regions.Add(regionId);
                }
            }

            dirty = true;
        }

        public bool RemoveSource(int sourceId)
        {
            if (!sourceRegions.Remove(sourceId))
            {
                return false;
            }

            sourceOrder.Remove(sourceId);
            dirty = true;
            return true;
        }

        public void Clear()
        {
            if (sourceRegions.Count == 0)
            {
                return;
            }

            sourceRegions.Clear();
            sourceOrder.Clear();
            dirty = true;
        }

        private void RebuildIfNeeded()
        {
            if (!dirty)
            {
                return;
            }

            aggregatedRegions.Clear();
            aggregatedSet.Clear();

            for (var i = 0; i < sourceOrder.Count; i++)
            {
                var sourceId = sourceOrder[i];
                var regions = sourceRegions[sourceId];
                for (var j = 0; j < regions.Count; j++)
                {
                    var regionId = regions[j];
                    if (aggregatedSet.Add(regionId))
                    {
                        aggregatedRegions.Add(regionId);
                    }
                }
            }

            dirty = false;
        }
    }
}
