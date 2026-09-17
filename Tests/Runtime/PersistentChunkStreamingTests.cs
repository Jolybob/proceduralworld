using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class PersistentChunkStreamingTests
    {
        [Test]
        public void UpdateLoadsPersistedChunkState()
        {
            const int seed = 60427;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(generator, store);
            var sink = new RecordingSink();
            var controller = new WorldPersistentChunkStreamingController(
                persistence,
                new ChunkStreamingPlanner(0),
                sink);

            GeneratedChunk edited = generator.GenerateChunk(new ChunkCoord(2, 3));
            GeneratedCell cell = edited.GetCell(1, 2);
            cell.Flags |= GeneratedCellFlags.Reserved;
            edited.SetCell(1, 2, cell);
            persistence.SaveChunk(edited);

            controller.Update(new ChunkCoord(2, 3));

            Assert.AreEqual(1, sink.Loaded.Count);
            GeneratedCell restored = sink.Loaded[new ChunkCoord(2, 3)].GetCell(1, 2);
            Assert.IsTrue((restored.Flags & GeneratedCellFlags.Reserved) != 0);
            Assert.IsTrue(controller.LoadedChunks.ContainsKey(new ChunkCoord(2, 3)));
        }

        [Test]
        public void MovingStreamingCenterPersistsEditedChunkBeforeUnload()
        {
            const int seed = 60427;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(generator, store);
            var sink = new RecordingSink();
            var controller = new WorldPersistentChunkStreamingController(
                persistence,
                new ChunkStreamingPlanner(0),
                sink);

            ChunkCoord first = new ChunkCoord(0, 0);
            ChunkCoord second = new ChunkCoord(1, 0);

            controller.Update(first);
            GeneratedChunk loaded = sink.Loaded[first];
            GeneratedCell edited = loaded.GetCell(0, 0);
            edited.Flags |= GeneratedCellFlags.Reserved;
            loaded.SetCell(0, 0, edited);

            controller.Update(second);

            Assert.IsFalse(controller.LoadedChunks.ContainsKey(first));
            Assert.IsTrue(store.TryLoad(first, out WorldChunkSaveData data));
            Assert.AreEqual(1, data.Modifications.Count);
            Assert.IsTrue((data.Modifications[0].Cell.Flags & GeneratedCellFlags.Reserved) != 0);
            Assert.IsTrue(sink.Unloaded.Contains(first));
        }

        [Test]
        public void ResetPersistsAndUnloadsAllLoadedChunks()
        {
            const int seed = 60427;
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var generator = new ProceduralWorldGenerator(seed, settings);
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(generator, store);
            var sink = new RecordingSink();
            var controller = new WorldPersistentChunkStreamingController(
                persistence,
                new ChunkStreamingPlanner(1),
                sink);

            controller.Update(new ChunkCoord(-2, 4));
            GeneratedChunk edited = sink.Loaded[new ChunkCoord(-2, 4)];
            GeneratedCell cell = edited.GetCell(3, 3);
            cell.Flags |= GeneratedCellFlags.Reserved;
            edited.SetCell(3, 3, cell);

            controller.Reset();

            Assert.AreEqual(0, controller.LoadedChunks.Count);
            Assert.AreEqual(0, controller.Planner.ActiveChunks.Count);
            Assert.IsTrue(store.TryLoad(new ChunkCoord(-2, 4), out WorldChunkSaveData data));
            Assert.AreEqual(1, data.Modifications.Count);
            Assert.AreEqual(9, sink.Unloaded.Count);
        }

        private sealed class RecordingSink : IWorldChunkSink
        {
            public readonly Dictionary<ChunkCoord, GeneratedChunk> Loaded =
                new Dictionary<ChunkCoord, GeneratedChunk>();
            public readonly List<ChunkCoord> Unloaded = new List<ChunkCoord>();

            public void Load(ChunkCoord coordinate, GeneratedChunk chunk)
            {
                Loaded[coordinate] = chunk;
            }

            public void Unload(ChunkCoord coordinate)
            {
                Loaded.Remove(coordinate);
                Unloaded.Add(coordinate);
            }
        }
    }
}
