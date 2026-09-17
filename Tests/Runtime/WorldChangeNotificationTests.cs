using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldChangeNotificationTests
    {
        [Test]
        public void ObserverReceivesSuccessfulEditAfterJournalRecord()
        {
            const int seed = 116503;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var sourceJournal = new InMemoryWorldChangeJournal();
            var journal = new WorldChangeObserverJournal(sourceJournal);
            var observer = new RecordingObserver();
            using (journal.Subscribe(observer))
            {
                var edits = new WorldEditService(access, settings.chunkSize, journal);
                WorldPosition position = new WorldPosition(1, 1);
                Assert.IsTrue(edits.TryGetCell(position, out GeneratedCell before));

                GeneratedCell after = before;
                after.Flags |= GeneratedCellFlags.Reserved;

                Assert.IsTrue(edits.TrySetCell(position, after));
            }

            Assert.AreEqual(1, sourceJournal.Changes.Count);
            Assert.AreEqual(1, observer.Changes.Count);
            Assert.AreEqual(sourceJournal.Changes[0], observer.Changes[0]);
            Assert.AreEqual(new WorldPosition(1, 1), observer.Changes[0].Position);
        }

        [Test]
        public void NoOpEditsDoNotNotifyObservers()
        {
            const int seed = 116503;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new WorldChangeObserverJournal(new InMemoryWorldChangeJournal());
            var observer = new RecordingObserver();
            using (journal.Subscribe(observer))
            {
                var edits = new WorldEditService(access, settings.chunkSize, journal);
                Assert.IsTrue(edits.TryGetCell(new WorldPosition(0, 0), out GeneratedCell cell));
                Assert.IsTrue(edits.TrySetCell(new WorldPosition(0, 0), cell));
            }

            Assert.AreEqual(0, journal.Changes.Count);
            Assert.AreEqual(0, observer.Changes.Count);
        }

        [Test]
        public void UnsubscribedObserverStopsReceivingChanges()
        {
            const int seed = 116503;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new WorldChangeObserverJournal(new InMemoryWorldChangeJournal());
            var observer = new RecordingObserver();
            System.IDisposable subscription = journal.Subscribe(observer);
            var edits = new WorldEditService(access, settings.chunkSize, journal);

            Assert.IsTrue(edits.TrySetCell(new WorldPosition(1, 0),
                CreateReservedCell(access, new WorldPosition(1, 0))));
            Assert.AreEqual(1, observer.Changes.Count);

            subscription.Dispose();
            Assert.AreEqual(0, journal.ListenerCount);

            Assert.IsTrue(edits.TrySetCell(new WorldPosition(2, 0),
                CreateReservedCell(access, new WorldPosition(2, 0))));
            Assert.AreEqual(1, observer.Changes.Count);
        }

        [Test]
        public void MultipleObserversReceiveTheSameChangeInRegistrationOrder()
        {
            const int seed = 116503;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new WorldChangeObserverJournal(new InMemoryWorldChangeJournal());
            var calls = new List<int>();
            var first = new OrderedObserver(1, calls);
            var second = new OrderedObserver(2, calls);
            using (journal.Subscribe(first))
            using (journal.Subscribe(second))
            {
                var edits = new WorldEditService(access, settings.chunkSize, journal);
                Assert.IsTrue(edits.TrySetCell(new WorldPosition(3, 0),
                    CreateReservedCell(access, new WorldPosition(3, 0))));
            }

            CollectionAssert.AreEqual(new[] { 1, 2 }, calls);
        }

        private static GeneratedCell CreateReservedCell(
            IWorldChunkAccess access,
            WorldPosition position)
        {
            Assert.IsTrue(access.TryGetCell(position, out GeneratedCell cell));
            cell.Flags |= GeneratedCellFlags.Reserved;
            return cell;
        }

        private sealed class RecordingObserver : IWorldChangeListener
        {
            public readonly List<WorldCellChange> Changes = new List<WorldCellChange>();

            public void OnWorldChanged(WorldCellChange change)
            {
                Changes.Add(change);
            }
        }

        private sealed class OrderedObserver : IWorldChangeListener
        {
            private readonly int id;
            private readonly List<int> calls;

            public OrderedObserver(int id, List<int> calls)
            {
                this.id = id;
                this.calls = calls;
            }

            public void OnWorldChanged(WorldCellChange change)
            {
                calls.Add(id);
            }
        }

        private sealed class NullSink : IWorldChunkSink
        {
            public void Load(ChunkCoord coordinate, GeneratedChunk chunk) { }
            public void Unload(ChunkCoord coordinate) { }
        }
    }
}
