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

        private sealed class FixedRegionResolver : IWorldPresentationRegionResolver
        {
            private readonly Dictionary<WorldPosition, int> regions = new Dictionary<WorldPosition, int>();
            public void Set(WorldPosition position, int regionId) { regions[position] = regionId; }
            public int ResolveRegion(WorldPosition position) { return regions[position]; }
        }
    }
}
