using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldEditTransactionTests
    {
        [Test]
        public void CommitPublishesGroupedChangesToTargetJournal()
        {
            const int seed = 127931;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var access = CreateAccess(seed, settings);
            var journal = new InMemoryWorldChangeJournal();
            var transaction = new WorldEditTransaction(access, settings.chunkSize, journal);

            Assert.IsTrue(transaction.TryGetCell(new WorldPosition(0, 0), out GeneratedCell first));
            first.Flags |= GeneratedCellFlags.Reserved;
            Assert.IsTrue(transaction.TrySetCell(new WorldPosition(0, 0), first));

            Assert.IsTrue(transaction.TryGetCell(new WorldPosition(1, 0), out GeneratedCell second));
            second.Flags |= GeneratedCellFlags.Reserved;
            Assert.IsTrue(transaction.TrySetCell(new WorldPosition(1, 0), second));

            Assert.AreEqual(0, journal.Changes.Count);
            Assert.AreEqual(2, transaction.Changes.Count);
            Assert.IsTrue(transaction.Commit());

            Assert.AreEqual(2, journal.Changes.Count);
            Assert.IsTrue(transaction.IsCompleted);
            Assert.AreEqual(transaction.Changes[0], journal.Changes[0]);
            Assert.AreEqual(transaction.Changes[1], journal.Changes[1]);
        }

        [Test]
        public void RollbackRestoresAllChangesWithoutPublishingThem()
        {
            const int seed = 127931;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var access = CreateAccess(seed, settings);
            var journal = new InMemoryWorldChangeJournal();
            var transaction = new WorldEditTransaction(access, settings.chunkSize, journal);
            var position = new WorldPosition(0, 0);

            Assert.IsTrue(transaction.TryGetCell(position, out GeneratedCell before));
            GeneratedCell after = before;
            after.Flags |= GeneratedCellFlags.Reserved;
            Assert.IsTrue(transaction.TrySetCell(position, after));

            Assert.IsTrue(transaction.Rollback());
            Assert.IsTrue(access.TryGetCell(position, out GeneratedCell restored));
            Assert.AreEqual(before, restored);
            Assert.AreEqual(0, journal.Changes.Count);
            Assert.IsTrue(transaction.IsCompleted);
        }

        [Test]
        public void EmptyCommitAndRollbackReturnFalse()
        {
            const int seed = 127931;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var access = CreateAccess(seed, settings);

            var commit = new WorldEditTransaction(access, settings.chunkSize, new InMemoryWorldChangeJournal());
            Assert.IsFalse(commit.Commit());

            var rollback = new WorldEditTransaction(access, settings.chunkSize, new InMemoryWorldChangeJournal());
            Assert.IsFalse(rollback.Rollback());
        }

        [Test]
        public void CompletedTransactionRejectsFurtherEdits()
        {
            const int seed = 127931;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var access = CreateAccess(seed, settings);
            var transaction = new WorldEditTransaction(
                access,
                settings.chunkSize,
                new InMemoryWorldChangeJournal());

            Assert.IsTrue(transaction.Commit() == false);
            Assert.Throws<System.InvalidOperationException>(() =>
                transaction.TryGetCell(new WorldPosition(0, 0), out GeneratedCell ignored));
        }

        private static WorldPersistentChunkStreamingController CreateAccess(
            int seed,
            WorldGenerationSettings settings)
        {
            var generator = new ProceduralWorldGenerator(seed, settings);
            var persistence = new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore());
            var access = new WorldPersistentChunkStreamingController(
                persistence,
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));
            return access;
        }

        private sealed class NullSink : IWorldChunkSink
        {
            public void Load(ChunkCoord coordinate, GeneratedChunk chunk) { }
            public void Unload(ChunkCoord coordinate) { }
        }
    }
}
