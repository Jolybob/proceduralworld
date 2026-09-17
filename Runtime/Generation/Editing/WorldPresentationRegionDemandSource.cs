using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public interface IWorldPresentationRegionDemandSource
    {
        int SourceId { get; }
        IEnumerable<int> GetDemandedRegions();
        event Action<int> DemandChanged;
    }

    public sealed class WorldPresentationRegionDemandSourceCoordinator
    {
        private readonly WorldPresentationRegionDemandCoordinator demand;
        private readonly Dictionary<int, IWorldPresentationRegionDemandSource> sources = new Dictionary<int, IWorldPresentationRegionDemandSource>();
        private readonly List<int> sourceOrder = new List<int>();
        private readonly HashSet<int> pendingRefreshes = new HashSet<int>();
        private readonly List<int> pendingRefreshOrder = new List<int>();
        private int batchDepth;

        public int SourceCount => sourceOrder.Count;
        public bool IsBatching => batchDepth > 0;

        public WorldPresentationRegionDemandSourceCoordinator(WorldPresentationRegionDemandCoordinator demand)
        {
            this.demand = demand ?? throw new ArgumentNullException(nameof(demand));
        }

        public bool HasSource(int sourceId) => sources.ContainsKey(sourceId);

        public WorldPresentationRegionDemandSourceBatch BeginBatch()
        {
            batchDepth++;
            return new WorldPresentationRegionDemandSourceBatch(this);
        }

        public void Register(IWorldPresentationRegionDemandSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (sources.TryGetValue(source.SourceId, out var existing))
            {
                existing.DemandChanged -= OnDemandChanged;
            }
            else
            {
                sourceOrder.Add(source.SourceId);
            }

            sources[source.SourceId] = source;
            source.DemandChanged += OnDemandChanged;
            QueueRefresh(source.SourceId);
        }

        public bool Unregister(int sourceId)
        {
            if (!sources.TryGetValue(sourceId, out var source)) return false;

            source.DemandChanged -= OnDemandChanged;
            sources.Remove(sourceId);
            sourceOrder.Remove(sourceId);
            RemovePendingRefresh(sourceId);
            demand.RemoveSourceDemand(sourceId);
            return true;
        }

        public void Refresh(int sourceId)
        {
            if (!sources.TryGetValue(sourceId, out var source)) return;
            demand.SetSourceDemand(sourceId, source.GetDemandedRegions());
        }

        public void RefreshAll()
        {
            for (var i = 0; i < sourceOrder.Count; i++)
                QueueRefresh(sourceOrder[i]);

            if (batchDepth == 0)
            {
                FlushPendingRefreshes();
            }
        }

        public void Clear()
        {
            for (var i = sourceOrder.Count - 1; i >= 0; i--)
            {
                var sourceId = sourceOrder[i];
                sources[sourceId].DemandChanged -= OnDemandChanged;
                demand.RemoveSourceDemand(sourceId);
            }

            sources.Clear();
            sourceOrder.Clear();
            pendingRefreshes.Clear();
            pendingRefreshOrder.Clear();
        }

        internal void EndBatch()
        {
            if (batchDepth == 0)
            {
                return;
            }

            batchDepth--;
            if (batchDepth == 0)
            {
                FlushPendingRefreshes();
            }
        }

        private void OnDemandChanged(int sourceId)
        {
            QueueRefresh(sourceId);
        }

        private void QueueRefresh(int sourceId)
        {
            if (batchDepth == 0)
            {
                Refresh(sourceId);
                return;
            }

            if (pendingRefreshes.Add(sourceId))
            {
                pendingRefreshOrder.Add(sourceId);
            }
        }

        private void FlushPendingRefreshes()
        {
            for (var i = 0; i < pendingRefreshOrder.Count; i++)
            {
                Refresh(pendingRefreshOrder[i]);
            }

            pendingRefreshOrder.Clear();
            pendingRefreshes.Clear();
        }

        private void RemovePendingRefresh(int sourceId)
        {
            if (!pendingRefreshes.Remove(sourceId))
            {
                return;
            }

            pendingRefreshOrder.Remove(sourceId);
        }
    }
}
