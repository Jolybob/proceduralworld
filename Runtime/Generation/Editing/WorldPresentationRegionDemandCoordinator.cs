using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public sealed class WorldPresentationRegionDemandCoordinator : IDisposable
    {
        private readonly WorldPresentationRegionResidencyCoordinator residency;
        private readonly WorldPresentationRegionDemandPlanner planner;
        private readonly HashSet<int> demandedRegions = new HashSet<int>();
        private readonly List<int> demandOrder = new List<int>();
        private bool disposed;

        public bool IsDisposed => disposed;
        public int DemandedRegionCount => demandedRegions.Count;

        public WorldPresentationRegionDemandCoordinator(WorldPresentationRegionResidencyCoordinator residency)
            : this(residency, new WorldPresentationRegionDemandPlanner())
        {
        }

        public WorldPresentationRegionDemandCoordinator(WorldPresentationRegionResidencyCoordinator residency, WorldPresentationRegionDemandPlanner planner)
        {
            this.residency = residency ?? throw new ArgumentNullException(nameof(residency));
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
        }

        public bool IsDemanded(int regionId) => demandedRegions.Contains(regionId);

        public void SetDemandedRegions(IEnumerable<int> regionIds)
        {
            if (disposed) return;
            if (regionIds == null) throw new ArgumentNullException(nameof(regionIds));

            var nextDemanded = new HashSet<int>();
            var nextOrder = new List<int>();
            foreach (var regionId in regionIds)
            {
                if (nextDemanded.Add(regionId)) nextOrder.Add(regionId);
            }

            var plan = planner.CreatePlan(residency.LoadedRegions, nextOrder);
            for (var i = 0; i < plan.RegionsToUnload.Count; i++)
                residency.EnsureUnloaded(plan.RegionsToUnload[i]);
            for (var i = 0; i < plan.RegionsToLoad.Count; i++)
                residency.EnsureLoaded(plan.RegionsToLoad[i]);

            demandedRegions.Clear();
            demandedRegions.UnionWith(nextDemanded);
            demandOrder.Clear();
            demandOrder.AddRange(nextOrder);
        }

        public void ClearDemand()
        {
            if (disposed) return;
            for (var i = demandOrder.Count - 1; i >= 0; i--)
                residency.EnsureUnloaded(demandOrder[i]);
            demandedRegions.Clear();
            demandOrder.Clear();
        }

        public void Dispose()
        {
            if (disposed) return;
            ClearDemand();
            disposed = true;
        }
    }
}
