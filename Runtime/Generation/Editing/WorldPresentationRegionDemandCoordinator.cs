using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public sealed class WorldPresentationRegionDemandCoordinator : IDisposable
    {
        private readonly WorldPresentationRegionResidencyCoordinator residency;
        private readonly WorldPresentationRegionDemandPlanner planner;
        private readonly WorldPresentationRegionDemandAggregator aggregator;
        private readonly HashSet<int> demandedRegions = new HashSet<int>();
        private readonly List<int> demandOrder = new List<int>();
        private bool disposed;

        public bool IsDisposed => disposed;
        public int DemandedRegionCount => demandedRegions.Count;
        public int DemandSourceCount => aggregator.SourceCount;

        public WorldPresentationRegionDemandCoordinator(WorldPresentationRegionResidencyCoordinator residency)
            : this(residency, new WorldPresentationRegionDemandPlanner(), new WorldPresentationRegionDemandAggregator())
        {
        }

        public WorldPresentationRegionDemandCoordinator(
            WorldPresentationRegionResidencyCoordinator residency,
            WorldPresentationRegionDemandPlanner planner)
            : this(residency, planner, new WorldPresentationRegionDemandAggregator())
        {
        }

        public WorldPresentationRegionDemandCoordinator(
            WorldPresentationRegionResidencyCoordinator residency,
            WorldPresentationRegionDemandPlanner planner,
            WorldPresentationRegionDemandAggregator aggregator)
        {
            this.residency = residency ?? throw new ArgumentNullException(nameof(residency));
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
            this.aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        }

        public bool IsDemanded(int regionId) => demandedRegions.Contains(regionId);

        public bool HasDemandSource(int sourceId) => aggregator.HasSource(sourceId);

        public void SetSourceDemand(int sourceId, IEnumerable<int> regionIds)
        {
            if (disposed) return;
            aggregator.SetSourceDemand(sourceId, regionIds);
            ReconcileAggregatedDemand();
        }

        public bool RemoveSourceDemand(int sourceId)
        {
            if (disposed) return false;
            if (!aggregator.RemoveSource(sourceId)) return false;
            ReconcileAggregatedDemand();
            return true;
        }

        public void ClearSourceDemand()
        {
            if (disposed) return;
            aggregator.Clear();
            ReconcileAggregatedDemand();
        }

        public void SetDemandedRegions(IEnumerable<int> regionIds)
        {
            if (disposed) return;
            if (regionIds == null) throw new ArgumentNullException(nameof(regionIds));

            aggregator.Clear();
            aggregator.SetSourceDemand(0, regionIds);
            ReconcileAggregatedDemand();
        }

        public void Reconcile()
        {
            if (disposed) return;
            ReconcileAggregatedDemand();
        }

        public void ClearDemand()
        {
            if (disposed) return;
            aggregator.Clear();
            ReconcileAggregatedDemand();
        }

        public void Dispose()
        {
            if (disposed) return;
            ClearDemand();
            disposed = true;
        }

        private void ReconcileAggregatedDemand()
        {
            var nextDemanded = aggregator.DemandedRegions;
            var plan = planner.CreatePlan(residency.LoadedRegions, nextDemanded);

            for (var i = 0; i < plan.RegionsToUnload.Count; i++)
                residency.EnsureUnloaded(plan.RegionsToUnload[i]);
            for (var i = 0; i < plan.RegionsToLoad.Count; i++)
                residency.EnsureLoaded(plan.RegionsToLoad[i]);

            demandedRegions.Clear();
            for (var i = 0; i < nextDemanded.Count; i++)
                demandedRegions.Add(nextDemanded[i]);
            demandOrder.Clear();
            demandOrder.AddRange(nextDemanded);
        }
    }
}
