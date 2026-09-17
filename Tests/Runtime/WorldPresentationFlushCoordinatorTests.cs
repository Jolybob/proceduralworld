using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldPresentationFlushCoordinatorTests
    {
        [Test]
        public void FlushCoalescesDirectChangesAndPreservesOrder()
        {
            var journal = new WorldChangeObserverJournal(new WorldChangeJournal());
            var renderer = new RecordingRenderer();
            var coordinator = new WorldPresentationFlushCoordinator(journal, renderer);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);
            var first = new WorldPosition(1, 2);
            var second = new WorldPosition(3, 4);

            journal.Record(new WorldCellChange(first, cell, empty, WorldEditOperationKind.SetTile));
            journal.Record(new WorldCellChange(second, cell, empty, WorldEditOperationKind.SetTile));
            journal.Record(new WorldCellChange(first, empty, cell, WorldEditOperationKind.SetTile));

            Assert.AreEqual(2, coordinator.DirtyCount);
            coordinator.Flush();

            Assert.AreEqual(1, renderer.Batches.Count);
            Assert.AreEqual(2, renderer.Batches[0].Count);
            Assert.AreEqual(first, renderer.Batches[0].Changes[0].Position);
            Assert.AreEqual(cell, renderer.Batches[0].Changes[0].After);
            Assert.AreEqual(second, renderer.Batches[0].Changes[1].Position);
            Assert.AreEqual(0, coordinator.DirtyCount);
        }

        [Test]
        public void FlushDoesNothingWhenClean()
        {
            var journal = new WorldChangeObserverJournal(new WorldChangeJournal());
            var renderer = new RecordingRenderer();
            var coordinator = new WorldPresentationFlushCoordinator(journal, renderer);

            coordinator.Flush();

            Assert.AreEqual(0, renderer.Batches.Count);
        }

        [Test]
        public void FlushCoalescesLogicalBatchNotifications()
        {
            var journal = new WorldChangeObserverJournal(new WorldChangeJournal());
            var renderer = new RecordingRenderer();
            var coordinator = new WorldPresentationFlushCoordinator(journal, renderer);
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            var empty = new GeneratedCell(WorldTile.Empty, 0);
            var position = new WorldPosition(7, 8);

            journal.RecordBatch(new[]
            {
                new WorldCellChange(position, cell, empty, WorldEditOperationKind.SetTile),
                new WorldCellChange(position, empty, cell, WorldEditOperationKind.SetCell)
            });

            coordinator.Flush();

            Assert.AreEqual(1, renderer.Batches.Count);
            Assert.AreEqual(1, renderer.Batches[0].Count);
            Assert.AreEqual(cell, renderer.Batches[0].Changes[0].Before);
            Assert.AreEqual(cell, renderer.Batches[0].Changes[0].After);
            Assert.AreEqual(WorldEditOperationKind.SetCell, renderer.Batches[0].Changes[0].Operation);
        }

        [Test]
        public void DisposeStopsNotificationsAndClearsPendingChanges()
        {
            var journal = new WorldChangeObserverJournal(new WorldChangeJournal());
            var renderer = new RecordingRenderer();
            var coordinator = new WorldPresentationFlushCoordinator(journal, renderer);
            var cell = new GeneratedCell(WorldTile.Deep, 0);

            journal.Record(new WorldCellChange(
                new WorldPosition(1, 1),
                cell,
                cell,
                WorldEditOperationKind.SetCell));

            coordinator.Dispose();
            coordinator.Flush();
            journal.Record(new WorldCellChange(
                new WorldPosition(2, 2),
                cell,
                cell,
                WorldEditOperationKind.SetCell));

            Assert.IsTrue(coordinator.IsDisposed);
            Assert.AreEqual(0, coordinator.DirtyCount);
            Assert.AreEqual(0, renderer.Batches.Count);
        }

        private sealed class RecordingRenderer : IWorldChangeRenderer
        {
            public readonly List<WorldChangeBatch> Batches = new List<WorldChangeBatch>();

            public void Render(WorldCellChange change)
            {
            }

            public void RenderBatch(WorldChangeBatch batch)
            {
                Batches.Add(batch);
            }
        }
    }
}
