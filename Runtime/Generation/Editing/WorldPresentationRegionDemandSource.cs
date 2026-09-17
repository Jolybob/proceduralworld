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

    public sealed class WorldPresentationRegionDemandSourceCoordinator : IDisposable
    {
        private readonly WorldPresentationRegionDemandCoordinator demand;
        private readonly Dictionary<int, IWorldPresentationRegionDemandSource> sources = new Dictionary<int, IWorldPresentationRegionDemandSource>();
        private readonly List<int> sourceOrder = new List<int>();
        private readonly HashSet<int> pendingRefreshes = new HashSet<int>();
        private readonly List<int> pendingRefreshOrder = new List<int>();
        private int batchDepth;
        private bool disposed;

        public int SourceCount => sourceOrder.Count;
        public bool IsBatching => batchDepth > 0;
        public bool IsDisposed => disposed;
        public IReadOnlyList<int> RegisteredSourceIds => sourceOrder.AsReadOnly();

        public WorldPresentationRegionDemandSourceCoordinator(WorldPresentationRegionDemandCoordinator demand)
        {
            this.demand = demand ?? throw new ArgumentNullException(nameof(demand));
        }

        public bool HasSource(int sourceId) => !disposed && sources.ContainsKey(sourceId);

        public WorldPresentationRegionDemandSourceBatch BeginBatch()
        {
            if (!disposed)
                batchDepth++;

            return new WorldPresentationRegionDemandSourceBatch(this);
        }

        public void Register(IWorldPresentationRegionDemandSource source)
        {
            if (disposed)
                return;

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
            if (disposed || !sources.TryGetValue(sourceId, out var source)) return false;

            source.DemandChanged -= OnDemandChanged;
            sources.Remove(sourceId);
            sourceOrder.Remove(sourceId);
            RemovePendingRefresh(sourceId);
            demand.RemoveSourceDemand(sourceId);
            return true;
        }

        public void Refresh(int sourceId)
        {
            if (disposed || !sources.TryGetValue(sourceId, out var source)) return;
            demand.SetSourceDemand(sourceId, source.GetDemandedRegions());
        }

        public void RefreshAll()
        {
            if (disposed)
                return;

            for (var i = 0; i < sourceOrder.Count; i++)
                QueueRefresh(sourceOrder[i]);

            if (batchDepth == 0)
                FlushPendingRefreshes();
        }

        public void Clear()
        {
            if (disposed)
                return;

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
            batchDepth = 0;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            Clear();
            disposed = true;
        }

        internal void EndBatch()
        {
            if (disposed || batchDepth == 0)
                return;

            batchDepth--;
            if (batchDepth == 0)
                FlushPendingRefreshes();
        }

        private void OnDemandChanged(int sourceId)
        {
            if (disposed)
                return;

            QueueRefresh(sourceId);
        }

        private void QueueRefresh(int sourceId)
        {
            if (disposed)
                return;

            if (batchDepth == 0)
            {
                Refresh(sourceId);
                return;
            }

            if (pendingRefreshes.Add(sourceId))
                pendingRefreshOrder.Add(sourceId);
        }

        private void FlushPendingRefreshes()
        {
            if (disposed)
                return;

            for (var i = 0; i < pendingRefreshOrder.Count; i++)
                Refresh(pendingRefreshOrder[i]);

            pendingRefreshOrder.Clear();
            pendingRefreshes.Clear();
        }

        private void RemovePendingRefresh(int sourceId)
        {
            if (!pendingRefreshes.Remove(sourceId))
                return;

            pendingRefreshOrder.Remove(sourceId);
        }
    }
}
