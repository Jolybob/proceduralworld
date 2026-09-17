using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldChunkPersistenceTests
    {
        [Test]
        public void SaveDataContainsOnlyCellsChangedFromDeterministicGeneration()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(43017, settings);
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(generator, store);
            var chunk = generator.GenerateChunk(new ChunkCoord(2, -1));

            GeneratedCell cell = chunk.GetCell(1, 2);
            cell.Flags |= GeneratedCellFlags.Reserved;
            chunk.SetCell(1, 2, cell);

            WorldChunkSaveData data = persistence.SaveChunk(chunk);

            Assert.AreEqual(new ChunkCoord(2, -1), data.Coordinate);
            Assert.AreEqual(1, data.Modifications.Count);
            Assert.AreEqual(1, data.Modifications[0].X);
            Assert.AreEqual(2, data.Modifications[0].Y);
            Assert.IsTrue((data.Modifications[0].Cell.Flags & GeneratedCellFlags.Reserved) != 0);
            Assert.AreEqual(1, store.Count);
        }

        [Test]
        public void LoadRestoresPersistedOverridesOnTopOfGeneratedChunk()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(43017, settings);
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(generator, store);
            var chunk = generator.GenerateChunk(new ChunkCoord(-3, 5));

            GeneratedCell cell = chunk.GetCell(0, 0);
            cell.SetResource(new ResourceId(11));
            cell.Flags |= GeneratedCellFlags.Reserved;
            chunk.SetCell(0, 0, cell);
            persistence.SaveChunk(chunk);

            GeneratedChunk loaded = persistence.LoadChunk(new ChunkCoord(-3, 5));
            GeneratedCell restored = loaded.GetCell(0, 0);

            Assert.AreEqual(new ResourceId(11), restored.Resource);
            Assert.IsTrue((restored.Flags & GeneratedCellFlags.HasResource) != 0);
            Assert.IsTrue((restored.Flags & GeneratedCellFlags.Reserved) != 0);
        }

        [Test]
        public void SavingAnUntouchedChunkRemovesStoredOverrides()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(43017, settings);
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(generator, store);
            var chunk = generator.GenerateChunk(new ChunkCoord(1, 1));

            GeneratedCell cell = chunk.GetCell(2, 3);
            cell.Flags |= GeneratedCellFlags.Reserved;
            chunk.SetCell(2, 3, cell);
            persistence.SaveChunk(chunk);
            Assert.AreEqual(1, store.Count);

            GeneratedChunk clean = generator.GenerateChunk(new ChunkCoord(1, 1));
            WorldChunkSaveData cleanData = persistence.SaveChunk(clean);

            Assert.AreEqual(0, cleanData.Modifications.Count);
            Assert.AreEqual(0, store.Count);
        }

        [Test]
        public void PersistenceIsDeterministicForTheSameSeedAndChunk()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(43017, settings);
            var storeA = new InMemoryWorldChunkStore();
            var storeB = new InMemoryWorldChunkStore();
            var persistenceA = new WorldChunkPersistenceService(generator, storeA);
            var persistenceB = new WorldChunkPersistenceService(generator, storeB);
            var chunkA = generator.GenerateChunk(new ChunkCoord(7, -4));
            var chunkB = generator.GenerateChunk(new ChunkCoord(7, -4));

            GeneratedCell cellA = chunkA.GetCell(3, 1);
            cellA.SetStructure(new StructureId(5));
            chunkA.SetCell(3, 1, cellA);
            GeneratedCell cellB = chunkB.GetCell(3, 1);
            cellB.SetStructure(new StructureId(5));
            chunkB.SetCell(3, 1, cellB);

            WorldChunkSaveData dataA = persistenceA.SaveChunk(chunkA);
            WorldChunkSaveData dataB = persistenceB.SaveChunk(chunkB);

            Assert.AreEqual(dataA.Coordinate, dataB.Coordinate);
            Assert.AreEqual(dataA.Modifications.Count, dataB.Modifications.Count);
            for (int i = 0; i < dataA.Modifications.Count; i++)
                Assert.AreEqual(dataA.Modifications[i], dataB.Modifications[i]);
        }

        [Test]
        public void PersistenceRejectsOverridesForAnotherChunk()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(43017, settings);
            var store = new MismatchedStore();
            var persistence = new WorldChunkPersistenceService(generator, store);

            Assert.Throws<System.InvalidOperationException>(
                () => persistence.LoadChunk(new ChunkCoord(8, 9)));
        }

        private sealed class MismatchedStore : IWorldChunkStore
        {
            public bool TryLoad(ChunkCoord coordinate, out WorldChunkSaveData data)
            {
                data = new WorldChunkSaveData(
                    new ChunkCoord(9, 9),
                    new[] { new WorldCellModification(0, 0, new GeneratedCell(WorldTile.Core, 1)) });
                return true;
            }

            public void Save(WorldChunkSaveData data)
            {
            }

            public bool Delete(ChunkCoord coordinate)
            {
                return false;
            }
        }
    }
}
