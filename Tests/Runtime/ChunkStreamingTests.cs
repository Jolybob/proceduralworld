using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class ChunkStreamingTests
    {
        [Test]
        public void PlannerLoadsExpectedSquareInStableOrder()
        {
            var planner = new ChunkStreamingPlanner(1);
            ChunkStreamingDelta delta = planner.Update(new ChunkCoord(0, 0));

            Assert.AreEqual(9, delta.ToLoad.Count);
            Assert.AreEqual(0, delta.ToUnload.Count);
            Assert.AreEqual(new ChunkCoord(-1, -1), delta.ToLoad[0]);
            Assert.AreEqual(new ChunkCoord(0, -1), delta.ToLoad[1]);
            Assert.AreEqual(new ChunkCoord(1, -1), delta.ToLoad[2]);
            Assert.AreEqual(new ChunkCoord(1, 1), delta.ToLoad[8]);
        }

        [Test]
        public void PlannerDoesNotReloadAnUnchangedCenter()
        {
            var planner = new ChunkStreamingPlanner(2);
            planner.Update(new ChunkCoord(4, -3));
            ChunkStreamingDelta second = planner.Update(new ChunkCoord(4, -3));

            Assert.AreEqual(0, second.ToLoad.Count);
            Assert.AreEqual(0, second.ToUnload.Count);
            Assert.IsFalse(second.HasChanges);
        }

        [Test]
        public void PlannerSupportsUnloadHysteresis()
        {
            var planner = new ChunkStreamingPlanner(1, 2);
            planner.Update(new ChunkCoord(0, 0));
            ChunkStreamingDelta delta = planner.Update(new ChunkCoord(1, 0));

            Assert.AreEqual(3, delta.ToLoad.Count);
            Assert.AreEqual(0, delta.ToUnload.Count);
            Assert.Contains(new ChunkCoord(-1, 1), new List<ChunkCoord>(planner.ActiveChunks));
        }

        [Test]
        public void PlannerUnloadsChunksOutsideTheConfiguredBoundary()
        {
            var planner = new ChunkStreamingPlanner(1);
            planner.Update(new ChunkCoord(0, 0));
            ChunkStreamingDelta delta = planner.Update(new ChunkCoord(3, 0));

            Assert.Contains(new ChunkCoord(-1, -1), new List<ChunkCoord>(delta.ToUnload));
            Assert.Contains(new ChunkCoord(-1, 1), new List<ChunkCoord>(delta.ToUnload));
            Assert.AreEqual(9, delta.ToUnload.Count);
            Assert.AreEqual(9, delta.ToLoad.Count);
        }

        [Test]
        public void ControllerGeneratesOnlyNewlyLoadedChunks()
        {
            var generator = new ProceduralWorldGenerator(75319, new WorldGenerationSettings { chunkSize = 4 });
            var planner = new ChunkStreamingPlanner(1);
            var sink = new RecordingChunkSink();
            var controller = new WorldChunkStreamingController(generator, planner, sink);

            ChunkStreamingDelta first = controller.Update(new ChunkCoord(0, 0));
            ChunkStreamingDelta second = controller.Update(new ChunkCoord(1, 0));

            Assert.AreEqual(9, first.ToLoad.Count);
            Assert.AreEqual(3, second.ToLoad.Count);
            Assert.AreEqual(3, second.ToUnload.Count);
            Assert.AreEqual(12, sink.Loaded.Count);
            Assert.AreEqual(3, sink.Unloaded.Count);
        }

        [Test]
        public void ControllerLoadsDeterministicallyForTheSameWorldState()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var sinkA = new RecordingChunkSink();
            var sinkB = new RecordingChunkSink();
            var controllerA = new WorldChunkStreamingController(
                new ProceduralWorldGenerator(75319, settings),
                new ChunkStreamingPlanner(1),
                sinkA);
            var controllerB = new WorldChunkStreamingController(
                new ProceduralWorldGenerator(75319, settings),
                new ChunkStreamingPlanner(1),
                sinkB);

            controllerA.Update(new ChunkCoord(5, -2));
            controllerB.Update(new ChunkCoord(5, -2));

            Assert.AreEqual(sinkA.Loaded.Count, sinkB.Loaded.Count);
            for (int i = 0; i < sinkA.Loaded.Count; i++)
            {
                ChunkCoord coordinate = sinkA.Loaded[i].Coordinate;
                Assert.AreEqual(coordinate, sinkB.Loaded[i].Coordinate);

                GeneratedChunk first = sinkA.Loaded[i].Chunk;
                GeneratedChunk second = sinkB.Loaded[i].Chunk;
                for (int cell = 0; cell < first.Cells.Length; cell++)
                    Assert.AreEqual(first.Cells[cell].Flags, second.Cells[cell].Flags);
            }
        }

        private sealed class RecordingChunkSink : IWorldChunkSink
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
