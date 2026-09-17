using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldEditHistoryTests
    {
        [Test]
        public void CommitGroupsMultipleEditsIntoOneUndoEntry()
        {
            const int seed = 105827;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new InMemoryWorldChangeJournal();
            var edits = new WorldEditService(access, settings.chunkSize, journal);
            var history = new WorldEditHistory(access, journal);
            var firstPosition = new WorldPosition(0, 0);
            var secondPosition = new WorldPosition(1, 0);

            Assert.IsTrue(edits.TryGetCell(firstPosition, out GeneratedCell firstBefore));
            Assert.IsTrue(edits.TryGetCell(secondPosition, out GeneratedCell secondBefore));

            var firstAfter = firstBefore;
            firstAfter.Flags |= GeneratedCellFlags.Reserved;
            var secondAfter = secondBefore;
            secondAfter.Flags |= GeneratedCellFlags.Reserved;

            Assert.IsTrue(edits.TrySetCell(firstPosition, firstAfter));
            Assert.IsTrue(edits.TrySetCell(secondPosition, secondAfter));

            WorldEditHistoryEntry entry = history.Commit("Place room");

            Assert.IsNotNull(entry);
            Assert.AreEqual("Place room", entry.Name);
            Assert.AreEqual(2, entry.Changes.Count);
            Assert.AreEqual(1, history.UndoEntries.Count);
            Assert.IsTrue(history.CanUndo);
        }

        [Test]
        public void UndoAndRedoRestoreExactBeforeAndAfterState()
        {
            const int seed = 105827;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new InMemoryWorldChangeJournal();
            var edits = new WorldEditService(access, settings.chunkSize, journal);
            var history = new WorldEditHistory(access, journal);
            var position = new WorldPosition(2, 2);

            Assert.IsTrue(edits.TryGetCell(position, out GeneratedCell before));
            GeneratedCell after = before;
            after.Flags |= GeneratedCellFlags.Reserved;
            Assert.IsTrue(edits.TrySetCell(position, after));
            history.Commit("Reserve cell");

            Assert.IsTrue(history.Undo());
            Assert.IsTrue(access.TryGetCell(position, out GeneratedCell undone));
            Assert.AreEqual(before, undone);
            Assert.IsTrue(history.CanRedo);

            Assert.IsTrue(history.Redo());
            Assert.IsTrue(access.TryGetCell(position, out GeneratedCell redone));
            Assert.AreEqual(after, redone);
            Assert.IsFalse(history.CanRedo);
        }

        [Test]
        public void UndoAppliesGroupedChangesInReverseOrder()
        {
            const int seed = 105827;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new InMemoryWorldChangeJournal();
            var edits = new WorldEditService(access, settings.chunkSize, journal);
            var history = new WorldEditHistory(access, journal);
            var first = new WorldPosition(0, 0);
            var second = new WorldPosition(1, 0);

            Assert.IsTrue(edits.TryGetCell(first, out GeneratedCell firstBefore));
            Assert.IsTrue(edits.TryGetCell(second, out GeneratedCell secondBefore));
            GeneratedCell firstAfter = firstBefore;
            firstAfter.Flags |= GeneratedCellFlags.Carved;
            GeneratedCell secondAfter = secondBefore;
            secondAfter.Flags |= GeneratedCellFlags.Reserved;

            Assert.IsTrue(edits.TrySetCell(first, firstAfter));
            Assert.IsTrue(edits.TrySetCell(second, secondAfter));
            history.Commit("Two edits");

            Assert.IsTrue(history.Undo());
            Assert.IsTrue(access.TryGetCell(first, out GeneratedCell firstRestored));
            Assert.IsTrue(access.TryGetCell(second, out GeneratedCell secondRestored));
            Assert.AreEqual(firstBefore, firstRestored);
            Assert.AreEqual(secondBefore, secondRestored);
        }

        [Test]
        public void NewCommittedEditClearsRedoBranch()
        {
            const int seed = 105827;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new InMemoryWorldChangeJournal();
            var edits = new WorldEditService(access, settings.chunkSize, journal);
            var history = new WorldEditHistory(access, journal);
            var position = new WorldPosition(0, 0);

            Assert.IsTrue(edits.TryGetCell(position, out GeneratedCell before));
            GeneratedCell first = before;
            first.Flags |= GeneratedCellFlags.Reserved;
            Assert.IsTrue(edits.TrySetCell(position, first));
            history.Commit("First");
            Assert.IsTrue(history.Undo());
            Assert.IsTrue(history.CanRedo);

            GeneratedCell second = before;
            second.Flags |= GeneratedCellFlags.Carved;
            Assert.IsTrue(edits.TrySetCell(position, second));
            history.Commit("Second");

            Assert.IsFalse(history.CanRedo);
            Assert.AreEqual("Second", history.UndoEntries[0].Name);
        }

        [Test]
        public void UndoWithoutExplicitCommitCapturesPendingChanges()
        {
            const int seed = 105827;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new InMemoryWorldChangeJournal();
            var edits = new WorldEditService(access, settings.chunkSize, journal);
            var history = new WorldEditHistory(access, journal);
            var position = new WorldPosition(3, 3);

            Assert.IsTrue(edits.TryGetCell(position, out GeneratedCell before));
            GeneratedCell after = before;
            after.Flags |= GeneratedCellFlags.Reserved;
            Assert.IsTrue(edits.TrySetCell(position, after));

            Assert.IsTrue(history.CanUndo);
            Assert.IsTrue(history.Undo());
            Assert.IsTrue(access.TryGetCell(position, out GeneratedCell restored));
            Assert.AreEqual(before, restored);
            Assert.AreEqual(1, history.RedoEntries.Count);
        }

        private sealed class NullSink : IWorldChunkSink
        {
            public void Load(ChunkCoord coordinate, GeneratedChunk chunk) { }
            public void Unload(ChunkCoord coordinate) { }
        }
    }
}
