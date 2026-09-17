using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public interface IWorldPresentationRegionImpactResolver
    {
        void ResolveRegions(WorldPosition position, ICollection<int> regionIds);
    }

    public sealed class SingleRegionPresentationImpactResolver : IWorldPresentationRegionImpactResolver
    {
        private readonly IWorldPresentationRegionResolver resolver;

        public SingleRegionPresentationImpactResolver(IWorldPresentationRegionResolver resolver)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public void ResolveRegions(WorldPosition position, ICollection<int> regionIds)
        {
            if (regionIds == null) throw new ArgumentNullException(nameof(regionIds));
            regionIds.Add(resolver.ResolveRegion(position));
        }
    }

    public sealed class WorldPresentationDirtyRegionSet
    {
        private readonly IWorldPresentationRegionImpactResolver impactResolver;
        private readonly Dictionary<int, WorldPresentationDirtySet> dirtyByRegion = new Dictionary<int, WorldPresentationDirtySet>();
        private readonly List<int> regionOrder = new List<int>();
        private readonly List<int> resolvedRegions = new List<int>();

        public int Count => regionOrder.Count;

        public WorldPresentationDirtyRegionSet(IWorldPresentationRegionResolver resolver)
            : this(new SingleRegionPresentationImpactResolver(resolver))
        {
        }

        public WorldPresentationDirtyRegionSet(IWorldPresentationRegionImpactResolver impactResolver)
        {
            this.impactResolver = impactResolver ?? throw new ArgumentNullException(nameof(impactResolver));
        }

        public void Mark(WorldCellChange change)
        {
            resolvedRegions.Clear();
            impactResolver.ResolveRegions(change.Position, resolvedRegions);
            for (int i = 0; i < resolvedRegions.Count; i++)
            {
                MarkRegion(resolvedRegions[i], change);
            }
        }

        public void MarkBatch(WorldChangeBatch batch)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            for (int i = 0; i < batch.Changes.Count; i++) Mark(batch.Changes[i]);
        }

        public IReadOnlyList<WorldPresentationDirtyRegionBatch> Drain()
        {
            var batches = new List<WorldPresentationDirtyRegionBatch>(regionOrder.Count);
            for (int i = 0; i < regionOrder.Count; i++)
            {
                int regionId = regionOrder[i];
                batches.Add(new WorldPresentationDirtyRegionBatch(regionId, dirtyByRegion[regionId].Drain()));
            }
            Clear();
            return batches.AsReadOnly();
        }

        public void Clear()
        {
            dirtyByRegion.Clear();
            regionOrder.Clear();
            resolvedRegions.Clear();
        }

        private void MarkRegion(int regionId, WorldCellChange change)
        {
            WorldPresentationDirtySet dirty;
            if (!dirtyByRegion.TryGetValue(regionId, out dirty))
            {
                dirty = new WorldPresentationDirtySet();
                dirtyByRegion.Add(regionId, dirty);
                regionOrder.Add(regionId);
            }
            dirty.Mark(change);
        }
    }

    public readonly struct WorldPresentationDirtyRegionBatch
    {
        public readonly int RegionId;
        public readonly WorldChangeBatch Changes;

        public WorldPresentationDirtyRegionBatch(int regionId, WorldChangeBatch changes)
        {
            if (changes == null) throw new ArgumentNullException(nameof(changes));
            RegionId = regionId;
            Changes = changes;
        }
    }
}
