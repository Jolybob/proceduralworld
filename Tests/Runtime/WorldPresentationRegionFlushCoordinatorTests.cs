using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldPresentationRegionFlushCoordinatorTests
    {
        [Test]
        public void FlushRendersOnlyAffectedRegionsInFirstSeenOrder()
        {
            var journal = new WorldChangeObserverJournal(new InMemoryWorldChangeJournal());
            var resolver = new FixedRegionResolver();
            var renderer = new RecordingRegionRenderer();
            var coordinator = new WorldPresentationRegionFlushCoordinator(journal, resolver, renderer);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);
            var first = new WorldPosition(1, 1);
            var second = new WorldPosition(2, 2);
            var third = new WorldPosition(3, 3);
            resolver.Set(first, 10);
            resolver.Set(second, 20);
            resolver.Set(third, 10);

            journal.Record(new WorldCellChange(first, cell, empty, WorldEditOperationKind.SetTile));
            journal.Record(new WorldCellChange(second, cell, empty, WorldEditOperationKind.SetTile));
            journal.Record(new WorldCellChange(first, empty, cell, WorldEditOperationKind.SetCell));
            journal.Record(new WorldCellChange(third, cell, empty, WorldEditOperationKind.SetTile));

            coordinator.Flush();

            Assert.AreEqual(2, renderer.Regions.Count);
            Assert.AreEqual(10, renderer.Regions[0].RegionId);
            Assert.AreEqual(2, renderer.Regions[0].Batch.Count);
            Assert.AreEqual(cell, renderer.Regions[0].Batch.Changes[0].Before);
            Assert.AreEqual(cell, renderer.Regions[0].Batch.Changes[0].After);
            Assert.AreEqual(20, renderer.Regions[1].RegionId);
            Assert.AreEqual(1, renderer.Regions[1].Batch.Count);
            Assert.AreEqual(0, coordinator.DirtyRegionCount);
        }

        [Test]
        public void CleanFlushIsNoOp()
        {
            var renderer = new RecordingRegionRenderer();
            var coordinator = new WorldPresentationRegionFlushCoordinator(
                new WorldChangeObserverJournal(new InMemoryWorldChangeJournal()),
                new FixedRegionResolver(),
                renderer);

            coordinator.Flush();

            Assert.AreEqual(0, renderer.Regions.Count);
        }

        [Test]
        public void DisposeStopsNotificationsAndClearsPendingRegions()
        {
            var journal = new WorldChangeObserverJournal(new InMemoryWorldChangeJournal());
            var renderer = new RecordingRegionRenderer();
            var resolver = new FixedRegionResolver();
            var position = new WorldPosition(1, 1);
            resolver.Set(position, 1);
            var coordinator = new WorldPresentationRegionFlushCoordinator(journal, resolver, renderer);
            var cell = new GeneratedCell(WorldTile.Deep, 0);

            journal.Record(new WorldCellChange(position, cell, cell, WorldEditOperationKind.SetCell));
            coordinator.Dispose();
            coordinator.Flush();

            Assert.IsTrue(coordinator.IsDisposed);
            Assert.AreEqual(0, coordinator.DirtyRegionCount);
            Assert.AreEqual(0, renderer.Regions.Count);
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

        private sealed class RecordingRegionRenderer : IWorldPresentationRegionRenderer
        {
            public readonly List<RenderedRegion> Regions = new List<RenderedRegion>();

            public void RenderRegion(int regionId, WorldChangeBatch batch)
            {
                Regions.Add(new RenderedRegion(regionId, batch));
            }
        }

        private readonly struct RenderedRegion
        {
            public readonly int RegionId;
            public readonly WorldChangeBatch Batch;

            public RenderedRegion(int regionId, WorldChangeBatch batch)
            {
                RegionId = regionId;
                Batch = batch;
            }
        }
    }
}
