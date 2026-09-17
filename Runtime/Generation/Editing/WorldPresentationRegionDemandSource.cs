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

        public int SourceCount => sourceOrder.Count;

        public WorldPresentationRegionDemandSourceCoordinator(WorldPresentationRegionDemandCoordinator demand)
        {
            this.demand = demand ?? throw new ArgumentNullException(nameof(demand));
        }

        public bool HasSource(int sourceId) => sources.ContainsKey(sourceId);

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
            Refresh(source.SourceId);
        }

        public bool Unregister(int sourceId)
        {
            if (!sources.TryGetValue(sourceId, out var source)) return false;

            source.DemandChanged -= OnDemandChanged;
            sources.Remove(sourceId);
            sourceOrder.Remove(sourceId);
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
                Refresh(sourceOrder[i]);
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
        }

        private void OnDemandChanged(int sourceId)
        {
            Refresh(sourceId);
        }
    }
}
