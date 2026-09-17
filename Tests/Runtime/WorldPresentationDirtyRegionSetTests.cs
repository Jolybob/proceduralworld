using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldPresentationDirtyRegionSetTests
    {
        [Test]
        public void MarkGroupsChangesByRegionAndPreservesRegionOrder()
        {
            var resolver = new FixedRegionResolver();
            var firstPosition = new WorldPosition(1, 1);
            var secondPosition = new WorldPosition(2, 2);
            var thirdPosition = new WorldPosition(3, 3);
            resolver.Set(firstPosition, 1);
            resolver.Set(secondPosition, 1);
            resolver.Set(thirdPosition, 2);
            var dirty = new WorldPresentationDirtyRegionSet(resolver);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);

            dirty.Mark(new WorldCellChange(firstPosition, cell, empty, WorldEditOperationKind.SetTile));
            dirty.Mark(new WorldCellChange(secondPosition, cell, empty, WorldEditOperationKind.SetTile));
            dirty.Mark(new WorldCellChange(thirdPosition, cell, empty, WorldEditOperationKind.SetTile));

            IReadOnlyList<WorldPresentationDirtyRegionBatch> batches = dirty.Drain();

            Assert.AreEqual(2, batches.Count);
            Assert.AreEqual(1, batches[0].RegionId);
            Assert.AreEqual(2, batches[0].Changes.Count);
            Assert.AreEqual(2, batches[1].RegionId);
            Assert.AreEqual(1, batches[1].Changes.Count);
        }

        [Test]
        public void MarkCoalescesWithinEachRegion()
        {
            var resolver = new FixedRegionResolver();
            var position = new WorldPosition(1, 1);
            resolver.Set(position, 1);
            var dirty = new WorldPresentationDirtyRegionSet(resolver);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);

            dirty.Mark(new WorldCellChange(position, cell, empty, WorldEditOperationKind.SetTile));
            dirty.Mark(new WorldCellChange(position, empty, cell, WorldEditOperationKind.SetCell));

            IReadOnlyList<WorldPresentationDirtyRegionBatch> batches = dirty.Drain();

            Assert.AreEqual(1, batches.Count);
            Assert.AreEqual(1, batches[0].Changes.Count);
            Assert.AreEqual(cell, batches[0].Changes.Changes[0].Before);
            Assert.AreEqual(cell, batches[0].Changes.Changes[0].After);
            Assert.AreEqual(WorldEditOperationKind.SetCell, batches[0].Changes.Changes[0].Operation);
        }

        [Test]
        public void DrainClearsAllRegions()
        {
            var resolver = new FixedRegionResolver();
            var position = new WorldPosition(1, 1);
            resolver.Set(position, 1);
            var dirty = new WorldPresentationDirtyRegionSet(resolver);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            dirty.Mark(new WorldCellChange(position, cell, cell, WorldEditOperationKind.SetCell));

            dirty.Drain();

            Assert.AreEqual(0, dirty.Count);
            Assert.AreEqual(0, dirty.Drain().Count);
        }

        [Test]
        public void RejectsNullResolverAndBatch()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new WorldPresentationDirtyRegionSet(null));

            var dirty = new WorldPresentationDirtyRegionSet(new FixedRegionResolver());
            Assert.Throws<System.ArgumentNullException>(() => dirty.MarkBatch(null));
        }

        private sealed class FixedRegionResolver : IWorldPresentationRegionResolver
        {
            private readonly Dictionary<WorldPosition, int> regions = new Dictionary<WorldPosition, int>();

            public void Set(WorldPosition position, int regionId)
            {
                regions[position] = regionId;
            }

            public int ResolveRegion(WorldPosition position)
            {
                return regions[position];
            }
        }
    }
}
