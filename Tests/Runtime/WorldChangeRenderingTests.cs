using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldChangeRenderingTests
    {
        [Test]
        public void DirectChangesReachRendererOnce()
        {
            var journal = new WorldChangeObserverJournal(new InMemoryWorldChangeJournal());
            var renderer = new RecordingRenderer();
            using (new WorldChangeRenderObserver(journal, renderer))
            {
                journal.Record(CreateChange(1, 2));
            }

            Assert.AreEqual(1, renderer.Changes.Count);
            Assert.AreEqual(0, renderer.Batches.Count);
        }

        [Test]
        public void TransactionBatchReachesRendererWithoutPerCellDuplication()
        {
            const int seed = 149863;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new WorldChangeObserverJournal(new InMemoryWorldChangeJournal());
            var renderer = new RecordingRenderer();
            using (new WorldChangeRenderObserver(journal, renderer))
            {
                var transaction = new WorldEditTransaction(access, settings.chunkSize, journal);
                Assert.IsTrue(transaction.TrySetCell(new WorldPosition(0, 0), CreateReservedCell(access, new WorldPosition(0, 0))));
                Assert.IsTrue(transaction.TrySetCell(new WorldPosition(1, 0), CreateReservedCell(access, new WorldPosition(1, 0))));
                Assert.IsTrue(transaction.Commit());
            }

            Assert.AreEqual(0, renderer.Changes.Count);
            Assert.AreEqual(1, renderer.Batches.Count);
            Assert.AreEqual(2, renderer.Batches[0].Count);
        }

        [Test]
        public void DisposeStopsBothNotificationPaths()
        {
            var journal = new WorldChangeObserverJournal(new InMemoryWorldChangeJournal());
            var renderer = new RecordingRenderer();
            var observer = new WorldChangeRenderObserver(journal, renderer);

            observer.Dispose();
            observer.Dispose();
            Assert.IsTrue(observer.IsDisposed);

            journal.Record(CreateChange(3, 4));
            journal.RecordBatch(new List<WorldCellChange> { CreateChange(5, 6) });

            Assert.AreEqual(0, renderer.Changes.Count);
            Assert.AreEqual(0, renderer.Batches.Count);
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

        private sealed class RecordingRenderer : IWorldChangeRenderer
        {
            public readonly List<WorldCellChange> Changes = new List<WorldCellChange>();
            public readonly List<WorldChangeBatch> Batches = new List<WorldChangeBatch>();

            public void Render(WorldCellChange change)
            {
                Changes.Add(change);
            }

            public void RenderBatch(WorldChangeBatch batch)
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
