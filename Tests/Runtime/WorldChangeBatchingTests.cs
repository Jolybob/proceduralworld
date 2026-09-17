using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldChangeBatchingTests
    {
        [Test]
        public void TransactionCommitPublishesOneBatchAndAllCellChanges()
        {
            const int seed = 138427;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var sourceJournal = new InMemoryWorldChangeJournal();
            var journal = new WorldChangeObserverJournal(sourceJournal);
            var observer = new BatchObserver();
            using (journal.SubscribeBatch(observer))
            {
                var transaction = new WorldEditTransaction(access, settings.chunkSize, journal);
                Assert.IsTrue(transaction.TrySetCell(new WorldPosition(0, 0), CreateReservedCell(access, new WorldPosition(0, 0))));
                Assert.IsTrue(transaction.TrySetCell(new WorldPosition(1, 0), CreateReservedCell(access, new WorldPosition(1, 0))));

                Assert.IsTrue(transaction.Commit());
            }

            Assert.AreEqual(2, sourceJournal.Changes.Count);
            Assert.AreEqual(1, observer.Batches.Count);
            Assert.AreEqual(2, observer.Batches[0].Count);
            Assert.AreEqual(sourceJournal.Changes[0], observer.Batches[0].Changes[0]);
            Assert.AreEqual(sourceJournal.Changes[1], observer.Batches[0].Changes[1]);
        }

        [Test]
        public void DirectRecordStillUsesPerChangeNotifications()
        {
            var journal = new WorldChangeObserverJournal(new InMemoryWorldChangeJournal());
            var singleObserver = new RecordingObserver();
            var batchObserver = new BatchObserver();
            using (journal.Subscribe(singleObserver))
            using (journal.SubscribeBatch(batchObserver))
            {
                journal.Record(CreateChange(1, 1));
            }

            Assert.AreEqual(1, singleObserver.Changes.Count);
            Assert.AreEqual(0, batchObserver.Batches.Count);
        }

        [Test]
        public void EmptyBatchDoesNotRecordOrNotify()
        {
            var sourceJournal = new InMemoryWorldChangeJournal();
            var journal = new WorldChangeObserverJournal(sourceJournal);
            var observer = new BatchObserver();
            using (journal.SubscribeBatch(observer))
            {
                journal.RecordBatch(new List<WorldCellChange>());
            }

            Assert.AreEqual(0, sourceJournal.Changes.Count);
            Assert.AreEqual(0, observer.Batches.Count);
        }

        [Test]
        public void TransactionFallsBackToPerChangeJournalWhenBatchingIsUnavailable()
        {
            const int seed = 138427;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new InMemoryWorldChangeJournal();
            var transaction = new WorldEditTransaction(access, settings.chunkSize, journal);
            Assert.IsTrue(transaction.TrySetCell(new WorldPosition(0, 1), CreateReservedCell(access, new WorldPosition(0, 1))));
            Assert.IsTrue(transaction.TrySetCell(new WorldPosition(1, 1), CreateReservedCell(access, new WorldPosition(1, 1))));

            Assert.IsTrue(transaction.Commit());
            Assert.AreEqual(2, journal.Changes.Count);
        }

        private static GeneratedCell CreateReservedCell(IWorldChunkAccess access, WorldPosition position)
        {
            Assert.IsTrue(access.TryGetCell(position, out GeneratedCell cell));
            cell.Flags |= GeneratedCellFlags.Reserved;
            return cell;
        }

        private static WorldCellChange CreateChange(int x, int y)
        {
            var before = new GeneratedCell();
            var after = before;
            after.Flags |= GeneratedCellFlags.Reserved;
            return new WorldCellChange(
                new WorldPosition(x, y),
                before,
                after,
                WorldEditOperationKind.SetCell);
        }

        private sealed class RecordingObserver : IWorldChangeListener
        {
            public readonly List<WorldCellChange> Changes = new List<WorldCellChange>();

            public void OnWorldChanged(WorldCellChange change)
            {
                Changes.Add(change);
            }
        }

        private sealed class BatchObserver : IWorldChangeBatchListener
        {
            public readonly List<WorldChangeBatch> Batches = new List<WorldChangeBatch>();

            public void OnWorldChanged(WorldChangeBatch batch)
            {
                Batches.Add(batch);
            }
        }

        private sealed class NullSink : IWorldChunkSink
        {
            public void Load(ChunkCoord coordinate, GeneratedChunk chunk) { }
            public void Unload(ChunkCoord coordinate) { }
        }
    }
}
