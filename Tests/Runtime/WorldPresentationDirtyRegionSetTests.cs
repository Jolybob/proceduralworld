using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldPresentationDirtyRegionSetTests
    {
        [Test]
        public void MarkGroupsChangesByRegionAndPreservesRegionOrder()
        {
            var dirty = new WorldPresentationDirtyRegionSet(new FixedRegionResolver());
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);

            dirty.Mark(new WorldCellChange(new WorldPosition(1, 1), cell, empty, WorldEditOperationKind.SetTile));
            dirty.Mark(new WorldCellChange(new WorldPosition(2, 2), cell, empty, WorldEditOperationKind.SetTile));
            dirty.Mark(new WorldCellChange(new WorldPosition(3, 3), cell, empty, WorldEditOperationKind.SetTile));

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
            var dirty = new WorldPresentationDirtyRegionSet(new FixedRegionResolver());
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);
            var position = new WorldPosition(1, 1);

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
            var dirty = new WorldPresentationDirtyRegionSet(new FixedRegionResolver());
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            dirty.Mark(new WorldCellChange(new WorldPosition(1, 1), cell, cell, WorldEditOperationKind.SetCell));

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
            public int ResolveRegion(WorldPosition position)
            {
                return position.X < 3 ? 1 : 2;
            }
        }
    }
}
