using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldPresentationDirtyRegionSetTests
    {
        [Test]
        public void GroupsChangesByRegionInFirstSeenOrder()
        {
            var resolver = new FixedRegionResolver();
            var first = new WorldPosition(1, 1);
            var second = new WorldPosition(2, 2);
            var third = new WorldPosition(3, 3);
            resolver.Set(first, 10); resolver.Set(second, 20); resolver.Set(third, 10);
            var dirty = new WorldPresentationDirtyRegionSet(resolver);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);

            dirty.Mark(new WorldCellChange(first, cell, empty, WorldEditOperationKind.SetTile));
            dirty.Mark(new WorldCellChange(second, cell, empty, WorldEditOperationKind.SetTile));
            dirty.Mark(new WorldCellChange(third, cell, empty, WorldEditOperationKind.SetTile));

            IReadOnlyList<WorldPresentationDirtyRegionBatch> batches = dirty.Drain();
            Assert.AreEqual(2, batches.Count);
            Assert.AreEqual(10, batches[0].RegionId);
            Assert.AreEqual(2, batches[0].Changes.Count);
            Assert.AreEqual(20, batches[1].RegionId);
            Assert.AreEqual(1, batches[1].Changes.Count);
        }

        [Test]
        public void CoalescesRepeatedPositionInsideRegion()
        {
            var resolver = new FixedRegionResolver();
            var position = new WorldPosition(1, 1);
            resolver.Set(position, 10);
            var dirty = new WorldPresentationDirtyRegionSet(resolver);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);

            dirty.Mark(new WorldCellChange(position, cell, empty, WorldEditOperationKind.SetTile));
            dirty.Mark(new WorldCellChange(position, empty, cell, WorldEditOperationKind.SetCell));

            var batches = dirty.Drain();
            Assert.AreEqual(1, batches.Count);
            Assert.AreEqual(1, batches[0].Changes.Count);
            Assert.AreEqual(cell, batches[0].Changes.Changes[0].Before);
            Assert.AreEqual(cell, batches[0].Changes.Changes[0].After);
            Assert.AreEqual(WorldEditOperationKind.SetCell, batches[0].Changes.Changes[0].Operation);
        }

        [Test]
        public void MarksAllImpactedRegionsAndCoalescesSharedRegion()
        {
            var resolver = new FixedImpactResolver();
            var first = new WorldPosition(1, 1);
            var second = new WorldPosition(2, 2);
            resolver.Set(first, 10, 20);
            resolver.Set(second, 20);
            var dirty = new WorldPresentationDirtyRegionSet(resolver);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);

            dirty.Mark(new WorldCellChange(first, cell, empty, WorldEditOperationKind.SetTile));
            dirty.Mark(new WorldCellChange(second, empty, cell, WorldEditOperationKind.SetCell));

            var batches = dirty.Drain();
            Assert.AreEqual(2, batches.Count);
            Assert.AreEqual(10, batches[0].RegionId);
            Assert.AreEqual(1, batches[0].Changes.Count);
            Assert.AreEqual(20, batches[1].RegionId);
            Assert.AreEqual(2, batches[1].Changes.Count);
            Assert.AreEqual(first, batches[1].Changes.Changes[0].Position);
            Assert.AreEqual(second, batches[1].Changes.Changes[1].Position);
        }

        [Test]
        public void SupportsImpactResolverWithNoAffectedRegions()
        {
            var resolver = new FixedImpactResolver();
            var position = new WorldPosition(1, 1);
            resolver.Set(position);
            var dirty = new WorldPresentationDirtyRegionSet(resolver);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);

            dirty.Mark(new WorldCellChange(position, cell, empty, WorldEditOperationKind.SetTile));

            Assert.AreEqual(0, dirty.Count);
            Assert.AreEqual(0, dirty.Drain().Count);
        }

        private sealed class FixedRegionResolver : IWorldPresentationRegionResolver
        {
            private readonly Dictionary<WorldPosition, int> regions = new Dictionary<WorldPosition, int>();
            public void Set(WorldPosition position, int regionId) { regions[position] = regionId; }
            public int ResolveRegion(WorldPosition position) { return regions[position]; }
        }

        private sealed class FixedImpactResolver : IWorldPresentationRegionImpactResolver
        {
            private readonly Dictionary<WorldPosition, int[]> regions = new Dictionary<WorldPosition, int[]>();

            public void Set(WorldPosition position, params int[] regionIds) { regions[position] = regionIds; }

            public void ResolveRegions(WorldPosition position, ICollection<int> regionIds)
            {
                int[] resolved = regions[position];
                for (int i = 0; i < resolved.Length; i++) regionIds.Add(resolved[i]);
            }
        }
    }
}
