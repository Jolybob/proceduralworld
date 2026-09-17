using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Maps presentation changes into caller-defined regions such as chunks or streaming cells.
    /// Region identity is intentionally opaque so generation remains independent from a chunking strategy.
    /// </summary>
    public interface IWorldPresentationRegionResolver
    {
        int ResolveRegion(WorldPosition position);
    }

    /// <summary>
    /// Receives one deterministic batch for a resolved presentation region.
    /// </summary>
    public interface IWorldPresentationRegionRenderer
    {
        void RenderRegion(int regionId, WorldChangeBatch batch);
    }

    /// <summary>
    /// Coalesces changes by position while grouping them by presentation region.
    /// Region order is first-seen order; position order inside each region is also first-seen order.
    /// </summary>
    public sealed class WorldPresentationDirtyRegionSet
    {
        private readonly IWorldPresentationRegionResolver resolver;
        private readonly Dictionary<int, WorldPresentationDirtySet> dirtyByRegion =
            new Dictionary<int, WorldPresentationDirtySet>();
        private readonly List<int> regionOrder = new List<int>();

        public int Count => regionOrder.Count;

        public WorldPresentationDirtyRegionSet(IWorldPresentationRegionResolver resolver)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public void Mark(WorldCellChange change)
        {
            int regionId = resolver.ResolveRegion(change.Position);
            WorldPresentationDirtySet dirty;
            if (!dirtyByRegion.TryGetValue(regionId, out dirty))
            {
                dirty = new WorldPresentationDirtySet();
                dirtyByRegion.Add(regionId, dirty);
                regionOrder.Add(regionId);
            }

            dirty.Mark(change);
        }

        public void MarkBatch(WorldChangeBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));

            for (int i = 0; i < batch.Changes.Count; i++)
                Mark(batch.Changes[i]);
        }

        public IReadOnlyList<WorldPresentationDirtyRegionBatch> Drain()
        {
            var batches = new List<WorldPresentationDirtyRegionBatch>(regionOrder.Count);
            for (int i = 0; i < regionOrder.Count; i++)
            {
                int regionId = regionOrder[i];
                batches.Add(new WorldPresentationDirtyRegionBatch(
                    regionId,
                    dirtyByRegion[regionId].Drain()));
            }

            Clear();
            return batches.AsReadOnly();
        }

        public void Clear()
        {
            dirtyByRegion.Clear();
            regionOrder.Clear();
        }
    }

    /// <summary>
    /// Immutable region identifier plus its coalesced presentation changes.
    /// </summary>
    public readonly struct WorldPresentationDirtyRegionBatch
    {
        public readonly int RegionId;
        public readonly WorldChangeBatch Changes;

        public WorldPresentationDirtyRegionBatch(int regionId, WorldChangeBatch changes)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));

            RegionId = regionId;
            Changes = changes;
        }
    }
}
