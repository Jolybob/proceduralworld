using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldChangeTrackingTests
    {
        [Test]
        public void EditServiceRecordsSuccessfulCellChanges()
        {
            const int seed = 116503;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(2, -1));

            var journal = new InMemoryWorldChangeJournal();
            var edits = new WorldEditService(access, settings.chunkSize, journal);
            WorldPosition position = new WorldPosition(9, -3);

            Assert.IsTrue(edits.TryGetCell(position, out GeneratedCell before));
            GeneratedCell after = before;
            after.Flags |= GeneratedCellFlags.Reserved;

            Assert.IsTrue(edits.TrySetCell(position, after));
            Assert.AreEqual(1, journal.Changes.Count);
            Assert.AreEqual(position, journal.Changes[0].Position);
            Assert.AreEqual(before, journal.Changes[0].Before);
            Assert.AreEqual(after, journal.Changes[0].After);
            Assert.AreEqual(WorldEditOperationKind.SetCell, journal.Changes[0].Operation);
        }

        [Test]
        public void NoOpEditDoesNotCreateAChange()
        {
            const int seed = 116503;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new InMemoryWorldChangeJournal();
            var edits = new WorldEditService(access, settings.chunkSize, journal);

            Assert.IsTrue(edits.TryGetCell(new WorldPosition(0, 0), out GeneratedCell cell));
            Assert.IsTrue(edits.TrySetCell(new WorldPosition(0, 0), cell));
            Assert.AreEqual(0, journal.Changes.Count);
        }

        [Test]
        public void NamedEditOperationsRecordBeforeAndAfterState()
        {
            const int seed = 116503;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(generator, new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            access.Update(new ChunkCoord(0, 0));

            var journal = new InMemoryWorldChangeJournal();
            var edits = new WorldEditService(access, settings.chunkSize, journal);
            var position = new WorldPosition(1, 1);

            Assert.IsTrue(edits.TryGetCell(position, out GeneratedCell initial));
            WorldTile targetTile = initial.Tile == WorldTile.Core ? WorldTile.Inner : WorldTile.Core;

            Assert.IsTrue(edits.TrySetTile(position, targetTile));
            Assert.IsTrue(edits.TrySetResource(position, new ResourceId(9)));
            Assert.IsTrue(edits.TryClearResource(position));
            Assert.IsTrue(edits.TrySetStructure(position, new StructureId(7)));
            Assert.IsTrue(edits.TryClearStructure(position));

            Assert.AreEqual(5, journal.Changes.Count);
            Assert.AreEqual(WorldEditOperationKind.SetTile, journal.Changes[0].Operation);
            Assert.AreEqual(WorldEditOperationKind.SetResource, journal.Changes[1].Operation);
            Assert.AreEqual(WorldEditOperationKind.ClearResource, journal.Changes[2].Operation);
            Assert.AreEqual(WorldEditOperationKind.SetStructure, journal.Changes[3].Operation);
            Assert.AreEqual(WorldEditOperationKind.ClearStructure, journal.Changes[4].Operation);
            for (int i = 0; i < journal.Changes.Count; i++)
                Assert.AreNotEqual(journal.Changes[i].Before, journal.Changes[i].After);
        }

        [Test]
        public void FailedEditIsNotRecorded()
        {
            var access = new WorldPersistentChunkStreamingController(
                new WorldChunkPersistenceService(
                    new ProceduralWorldGenerator(116503, new WorldGenerationSettings { chunkSize = 4 }),
                    new InMemoryWorldChunkStore()),
                new ChunkStreamingPlanner(0),
                new NullSink());
            var journal = new InMemoryWorldChangeJournal();
            var edits = new WorldEditService(access, 4, journal);

            Assert.IsFalse(edits.TrySetTile(new WorldPosition(0, 0), WorldTile.Core));
            Assert.AreEqual(0, journal.Changes.Count);
        }

        private sealed class NullSink : IWorldChunkSink
        {
            public void Load(ChunkCoord coordinate, GeneratedChunk chunk) { }
            public void Unload(ChunkCoord coordinate) { }
        }
    }
}
