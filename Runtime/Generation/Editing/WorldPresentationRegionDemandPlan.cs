using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public readonly struct WorldPresentationRegionDemandPlan
    {
        public IReadOnlyList<int> RegionsToLoad { get; }
        public IReadOnlyList<int> RegionsToUnload { get; }

        public WorldPresentationRegionDemandPlan(IReadOnlyList<int> regionsToLoad, IReadOnlyList<int> regionsToUnload)
        {
            RegionsToLoad = regionsToLoad ?? throw new ArgumentNullException(nameof(regionsToLoad));
            RegionsToUnload = regionsToUnload ?? throw new ArgumentNullException(nameof(regionsToUnload));
        }
    }

    public sealed class WorldPresentationRegionDemandPlanner
    {
        public WorldPresentationRegionDemandPlan CreatePlan(IEnumerable<int> currentRegions, IEnumerable<int> demandedRegions)
        {
            if (currentRegions == null) throw new ArgumentNullException(nameof(currentRegions));
            if (demandedRegions == null) throw new ArgumentNullException(nameof(demandedRegions));

            var current = new HashSet<int>();
            var currentOrder = new List<int>();
            foreach (var id in currentRegions) if (current.Add(id)) currentOrder.Add(id);

            var demanded = new HashSet<int>();
            var demandOrder = new List<int>();
            foreach (var id in demandedRegions) if (demanded.Add(id)) demandOrder.Add(id);

            var unload = new List<int>();
            for (var i = currentOrder.Count - 1; i >= 0; i--)
                if (!demanded.Contains(currentOrder[i])) unload.Add(currentOrder[i]);

            var load = new List<int>();
            for (var i = 0; i < demandOrder.Count; i++)
                if (!current.Contains(demandOrder[i])) load.Add(demandOrder[i]);

            return new WorldPresentationRegionDemandPlan(load, unload);
        }
    }
}
