using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class ScheduledPersistentChunkStreamingTests
    {
        [Test]
        public void ProcessLoadsPersistedChunkOnlyWhenBudgetAllows()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(
                new ProceduralWorldGenerator(99127, settings),
                store);
            var planner = new ChunkStreamingPlanner(0);
            var scheduler = new DeterministicChunkGenerationScheduler();
            var sink = new RecordingSink();
            var controller = new WorldScheduledPersistentChunkStreamingController(
                persistence,
                planner,
                sink,
                scheduler);

            controller.Update(new ChunkCoord(0, 0));

            Assert.AreEqual(1, controller.PendingGenerations);
            Assert.AreEqual(0, sink.Loaded.Count);
            Assert.AreEqual(0, controller.Process(0));
            Assert.AreEqual(1, controller.PendingGenerations);

            Assert.AreEqual(1, controller.Process(1));
            Assert.AreEqual(0, controller.PendingGenerations);
            Assert.AreEqual(1, sink.Loaded.Count);
            Assert.IsTrue(controller.TryGetChunk(new ChunkCoord(0, 0), out GeneratedChunk loaded));
            Assert.AreEqual(new ChunkCoord(0, 0), loaded.Coordinate);
        }

        [Test]
        public void UnloadCancelsPendingRequestAndDoesNotCreatePersistedData()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(
                new ProceduralWorldGenerator(99127, settings),
                store);
            var controller = new WorldScheduledPersistentChunkStreamingController(
                persistence,
                new ChunkStreamingPlanner(0),
                new RecordingSink());

            controller.Update(new ChunkCoord(0, 0));
            controller.Update(new ChunkCoord(2, 0));

            Assert.AreEqual(1, controller.PendingGenerations);
            Assert.IsFalse(store.TryLoad(new ChunkCoord(0, 0), out _));
        }

        [Test]
        public void ModifiedLoadedChunkIsSavedBeforeUnloadAndRestoredLater()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(
                new ProceduralWorldGenerator(99127, settings),
                store);
            var sink = new RecordingSink();
            var controller = new WorldScheduledPersistentChunkStreamingController(
                persistence,
                new ChunkStreamingPlanner(0),
                sink);

            controller.Update(new ChunkCoord(0, 0));
            Assert.AreEqual(1, controller.Process(1));

            WorldPosition position = new WorldPosition(1, 1);
            GeneratedCell original;
            Assert.IsTrue(controller.TryGetCell(position, out original));
            GeneratedCell modified = original;
            modified.SetTile(WorldTile.Core);
            Assert.IsTrue(controller.SetCell(position, modified));

            controller.Update(new ChunkCoord(2, 0));
            Assert.IsTrue(store.TryLoad(new ChunkCoord(0, 0), out WorldChunkSaveData saved));
            Assert.Greater(saved.Modifications.Count, 0);

            controller.Update(new ChunkCoord(0, 0));
            Assert.AreEqual(1, controller.Process(1));

            GeneratedCell restored;
            Assert.IsTrue(controller.TryGetCell(position, out restored));
            Assert.AreEqual(WorldTile.Core, restored.Tile);
        }

        [Test]
        public void ResetCancelsPendingWorkAndUnloadsLoadedChunks()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var persistence = new WorldChunkPersistenceService(
                new ProceduralWorldGenerator(99127, settings),
                new InMemoryWorldChunkStore());
            var sink = new RecordingSink();
            var controller = new WorldScheduledPersistentChunkStreamingController(
                persistence,
                new ChunkStreamingPlanner(0),
                sink);

            controller.Update(new ChunkCoord(0, 0));
            Assert.AreEqual(1, controller.Process(1));
            controller.Update(new ChunkCoord(2, 0));
            controller.Reset();

            Assert.AreEqual(0, controller.PendingGenerations);
            Assert.AreEqual(0, controller.LoadedChunks.Count);
            Assert.IsTrue(sink.Unloaded.Contains(new ChunkCoord(0, 0)));
        }

        private sealed class RecordingSink : IWorldChunkSink
        {
            public readonly List<LoadedChunk> Loaded = new List<LoadedChunk>();
            public readonly List<ChunkCoord> Unloaded = new List<ChunkCoord>();

            public void Load(ChunkCoord coordinate, GeneratedChunk chunk)
            {
                Loaded.Add(new LoadedChunk(coordinate, chunk));
            }

            public void Unload(ChunkCoord coordinate)
            {
                Unloaded.Add(coordinate);
            }
        }

        private readonly struct LoadedChunk
        {
            public readonly ChunkCoord Coordinate;
            public readonly GeneratedChunk Chunk;

            public LoadedChunk(ChunkCoord coordinate, GeneratedChunk chunk)
            {
                Coordinate = coordinate;
                Chunk = chunk;
            }
        }
    }
}
