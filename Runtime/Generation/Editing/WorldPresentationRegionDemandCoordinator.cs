using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public sealed class WorldPresentationRegionDemandCoordinator : IDisposable
    {
        private readonly WorldPresentationRegionResidencyCoordinator residency;
        private readonly HashSet<int> demandedRegions = new HashSet<int>();
        private readonly List<int> demandOrder = new List<int>();
        private bool disposed;

        public bool IsDisposed => disposed;
        public int DemandedRegionCount => demandedRegions.Count;

        public WorldPresentationRegionDemandCoordinator(WorldPresentationRegionResidencyCoordinator residency)
        {
            this.residency = residency ?? throw new ArgumentNullException(nameof(residency));
        }

        public bool IsDemanded(int regionId)
        {
            return demandedRegions.Contains(regionId);
        }

        public void SetDemandedRegions(IEnumerable<int> regionIds)
        {
            if (disposed)
            {
                return;
            }

            if (regionIds == null)
            {
                throw new ArgumentNullException(nameof(regionIds));
            }

            var nextDemanded = new HashSet<int>();
            var nextOrder = new List<int>();

            foreach (var regionId in regionIds)
            {
                if (nextDemanded.Add(regionId))
                {
                    nextOrder.Add(regionId);
                }
            }

            for (var i = demandOrder.Count - 1; i >= 0; i--)
            {
                var regionId = demandOrder[i];
                if (!nextDemanded.Contains(regionId))
                {
                    residency.EnsureUnloaded(regionId);
                }
            }

            for (var i = 0; i < nextOrder.Count; i++)
            {
                var regionId = nextOrder[i];
                if (!demandedRegions.Contains(regionId))
                {
                    residency.EnsureLoaded(regionId);
                }
            }

            demandedRegions.Clear();
            demandedRegions.UnionWith(nextDemanded);
            demandOrder.Clear();
            demandOrder.AddRange(nextOrder);
        }

        public void ClearDemand()
        {
            if (disposed)
            {
                return;
            }

            for (var i = demandOrder.Count - 1; i >= 0; i--)
            {
                residency.EnsureUnloaded(demandOrder[i]);
            }

            demandedRegions.Clear();
            demandOrder.Clear();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            ClearDemand();
            disposed = true;
        }
    }
}
