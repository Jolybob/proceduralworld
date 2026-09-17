using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class ChunkGenerationSchedulingTests
    {
        [Test]
        public void SchedulerUsesPriorityThenStableInsertionOrder()
        {
            var scheduler = new DeterministicChunkGenerationScheduler();

            scheduler.Enqueue(new ChunkCoord(5, 5), 10);
            scheduler.Enqueue(new ChunkCoord(1, 1), 0);
            scheduler.Enqueue(new ChunkCoord(2, 2), 0);
            scheduler.Enqueue(new ChunkCoord(3, 3), -1);

            ChunkGenerationRequest first;
            ChunkGenerationRequest second;
            ChunkGenerationRequest third;
            ChunkGenerationRequest fourth;

            Assert.IsTrue(scheduler.TryDequeue(out first));
            Assert.IsTrue(scheduler.TryDequeue(out second));
            Assert.IsTrue(scheduler.TryDequeue(out third));
            Assert.IsTrue(scheduler.TryDequeue(out fourth));

            Assert.AreEqual(new ChunkCoord(3, 3), first.Coordinate);
            Assert.AreEqual(new ChunkCoord(1, 1), second.Coordinate);
            Assert.AreEqual(new ChunkCoord(2, 2), third.Coordinate);
            Assert.AreEqual(new ChunkCoord(5, 5), fourth.Coordinate);
            Assert.AreEqual(0, scheduler.Count);
        }

        [Test]
        public void ReEnqueueUpdatesPriorityWithoutDuplicatingRequest()
        {
            var scheduler = new DeterministicChunkGenerationScheduler();
            var coordinate = new ChunkCoord(7, -2);

            scheduler.Enqueue(coordinate, 10);
            scheduler.Enqueue(coordinate, 0);

            Assert.AreEqual(1, scheduler.Count);
            ChunkGenerationRequest request;
            Assert.IsTrue(scheduler.TryDequeue(out request));
            Assert.AreEqual(coordinate, request.Coordinate);
            Assert.AreEqual(0, request.Priority);
            Assert.IsFalse(scheduler.TryDequeue(out request));
        }

        [Test]
        public void SchedulerContainsOnlyPendingRequests()
        {
            var scheduler = new DeterministicChunkGenerationScheduler();
            var coordinate = new ChunkCoord(7, -2);

            Assert.IsFalse(scheduler.Contains(coordinate));
            scheduler.Enqueue(coordinate, 1);
            Assert.IsTrue(scheduler.Contains(coordinate));

            ChunkGenerationRequest request;
            Assert.IsTrue(scheduler.TryDequeue(out request));
            Assert.IsFalse(scheduler.Contains(coordinate));
        }

        [Test]
        public void CancelRemovesPendingGeneration()
        {
            var scheduler = new DeterministicChunkGenerationScheduler();
            var coordinate = new ChunkCoord(-4, 9);

            scheduler.Enqueue(coordinate, 1);

            Assert.IsTrue(scheduler.Cancel(coordinate));
            Assert.IsFalse(scheduler.Cancel(coordinate));
            Assert.AreEqual(0, scheduler.Count);
        }

        [Test]
        public void BudgetLimitsTheNumberOfGeneratedChunks()
        {
            var scheduler = new DeterministicChunkGenerationScheduler();
            scheduler.Enqueue(new ChunkCoord(0, 0), 0);
            scheduler.Enqueue(new ChunkCoord(1, 0), 1);
            scheduler.Enqueue(new ChunkCoord(2, 0), 2);

            var generator = new ProceduralWorldGenerator(
                88411,
                new WorldGenerationSettings { chunkSize = 4 });
            var service = new BudgetedChunkGenerationService(generator, scheduler);
            var generated = new List<GeneratedChunk>();

            Assert.AreEqual(2, service.Process(2, generated));
            Assert.AreEqual(2, generated.Count);
            Assert.AreEqual(new ChunkCoord(0, 0), generated[0].Coordinate);
            Assert.AreEqual(new ChunkCoord(1, 0), generated[1].Coordinate);
            Assert.AreEqual(1, scheduler.Count);
        }

        [Test]
        public void ScheduledStreamingCanUpdateAndProcessWithinABudget()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var sink = new RecordingSink();
            var controller = new WorldScheduledChunkStreamingController(
                new ProceduralWorldGenerator(88411, settings),
                new ChunkStreamingPlanner(1),
                sink);

            ChunkStreamingDelta delta = controller.Update(new ChunkCoord(0, 0));

            Assert.AreEqual(9, delta.ToLoad.Count);
            Assert.AreEqual(9, controller.PendingGenerations);
            Assert.AreEqual(0, sink.Loaded.Count);

            Assert.AreEqual(ChunkStreamingState.Pending, controller.GetState(delta.ToLoad[0]));
            Assert.AreEqual(3, controller.Process(3));
            Assert.AreEqual(3, sink.Loaded.Count);
            Assert.AreEqual(6, controller.PendingGenerations);
            Assert.AreEqual(delta.ToLoad[0], sink.Loaded[0].Coordinate);
            Assert.AreEqual(ChunkStreamingState.Loaded, controller.GetState(delta.ToLoad[0]));
            Assert.AreEqual(ChunkStreamingState.Pending, controller.GetState(delta.ToLoad[3]));

            controller.Update(new ChunkCoord(3, 0));
            Assert.AreEqual(9, sink.Unloaded.Count);
            Assert.AreEqual(9, controller.PendingGenerations);
            Assert.AreEqual(ChunkStreamingState.Inactive, controller.GetState(new ChunkCoord(0, 0)));
        }

        [Test]
        public void ScheduledPersistentStreamingLoadsOnlyWhenBudgetIsProcessed()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(
                new ProceduralWorldGenerator(99107, settings),
                store);
            var sink = new RecordingSink();
            var controller = new WorldScheduledPersistentChunkStreamingController(
                persistence,
                new ChunkStreamingPlanner(1),
                sink);

            ChunkStreamingDelta delta = controller.Update(new ChunkCoord(0, 0));

            Assert.AreEqual(9, delta.ToLoad.Count);
            Assert.AreEqual(9, controller.PendingGenerations);
            Assert.AreEqual(0, controller.LoadedChunks.Count);
            Assert.AreEqual(0, sink.Loaded.Count);
            Assert.AreEqual(ChunkStreamingState.Pending, controller.GetState(delta.ToLoad[0]));

            Assert.AreEqual(2, controller.Process(2));
            Assert.AreEqual(2, controller.LoadedChunks.Count);
            Assert.AreEqual(2, sink.Loaded.Count);
            Assert.AreEqual(ChunkStreamingState.Loaded, controller.GetState(delta.ToLoad[0]));
            Assert.AreEqual(ChunkStreamingState.Pending, controller.GetState(delta.ToLoad[2]));
        }

        [Test]
        public void ScheduledPersistentStreamingCancelsPendingUnloadAndSavesLoadedChunks()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var store = new InMemoryWorldChunkStore();
            var persistence = new WorldChunkPersistenceService(
                new ProceduralWorldGenerator(99107, settings),
                store);
            var sink = new RecordingSink();
            var controller = new WorldScheduledPersistentChunkStreamingController(
                persistence,
                new ChunkStreamingPlanner(1),
                sink);

            controller.Update(new ChunkCoord(0, 0));
            Assert.AreEqual(1, controller.Process(1));
            GeneratedChunk loaded = controller.LoadedChunks[new ChunkCoord(0, 0)];

            GeneratedCell edited = loaded.GetCell(0, 0);
            WorldTile replacement = edited.Tile == WorldTile.Core ? WorldTile.Deep : WorldTile.Core;
            edited.SetTerrain(edited.Terrain, replacement);
            loaded.SetCell(0, 0, edited);

            controller.Update(new ChunkCoord(3, 0));

            Assert.IsFalse(controller.LoadedChunks.ContainsKey(new ChunkCoord(0, 0)));
            Assert.IsTrue(store.TryLoad(new ChunkCoord(0, 0), out _));
            Assert.AreEqual(9, sink.Unloaded.Count);
        }

        private sealed class RecordingSink : IWorldChunkSink
        {
            public readonly List<Loaded> Loaded = new List<Loaded>();
            public readonly List<ChunkCoord> Unloaded = new List<ChunkCoord>();

            public void Load(ChunkCoord coordinate, GeneratedChunk chunk)
            {
                Loaded.Add(new Loaded(coordinate, chunk));
            }

            public void Unload(ChunkCoord coordinate)
            {
                Unloaded.Add(coordinate);
            }
        }

        private readonly struct Loaded
        {
            public readonly ChunkCoord Coordinate;
            public readonly GeneratedChunk Chunk;

            public Loaded(ChunkCoord coordinate, GeneratedChunk chunk)
            {
                Coordinate = coordinate;
                Chunk = chunk;
            }
        }
    }
}
