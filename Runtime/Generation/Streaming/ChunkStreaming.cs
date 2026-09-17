using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public interface IWorldChunkSink
    {
        void Load(ChunkCoord coordinate, GeneratedChunk chunk);
        void Unload(ChunkCoord coordinate);
    }

    public readonly struct ChunkStreamingDelta
    {
        public readonly IReadOnlyList<ChunkCoord> ToLoad;
        public readonly IReadOnlyList<ChunkCoord> ToUnload;

        public ChunkStreamingDelta(IReadOnlyList<ChunkCoord> toLoad, IReadOnlyList<ChunkCoord> toUnload)
        {
            ToLoad = toLoad ?? throw new ArgumentNullException(nameof(toLoad));
            ToUnload = toUnload ?? throw new ArgumentNullException(nameof(toUnload));
        }

        public bool HasChanges => ToLoad.Count > 0 || ToUnload.Count > 0;
    }

    public sealed class ChunkStreamingPlanner
    {
        private readonly int loadRadius;
        private readonly int unloadRadius;
        private readonly IChunkStreamingOrder loadOrder;
        private readonly HashSet<ChunkCoord> active = new HashSet<ChunkCoord>();
        private readonly List<ChunkCoord> toLoad = new List<ChunkCoord>();
        private readonly List<ChunkCoord> toUnload = new List<ChunkCoord>();

        public int LoadRadius => loadRadius;
        public int UnloadRadius => unloadRadius;
        public IChunkStreamingOrder LoadOrder => loadOrder;
        public IReadOnlyCollection<ChunkCoord> ActiveChunks => active;

        public ChunkStreamingPlanner(int loadRadius, int unloadRadius = -1, IChunkStreamingOrder loadOrder = null)
        {
            if (loadRadius < 0)
                throw new ArgumentOutOfRangeException(nameof(loadRadius));

            if (unloadRadius < 0)
                unloadRadius = loadRadius;

            if (unloadRadius < loadRadius)
                throw new ArgumentOutOfRangeException(nameof(unloadRadius));

            this.loadRadius = loadRadius;
            this.unloadRadius = unloadRadius;
            this.loadOrder = loadOrder ?? new NearestFirstChunkStreamingOrder();
        }

        public ChunkStreamingDelta Update(ChunkCoord center)
        {
            toLoad.Clear();
            toUnload.Clear();

            for (int y = center.Y - loadRadius; y <= center.Y + loadRadius; y++)
            {
                for (int x = center.X - loadRadius; x <= center.X + loadRadius; x++)
                {
                    var coordinate = new ChunkCoord(x, y);
                    if (active.Add(coordinate))
                        toLoad.Add(coordinate);
                }
            }

            if (unloadRadius > loadRadius)
            {
                foreach (ChunkCoord coordinate in new List<ChunkCoord>(active))
                {
                    if (IsOutsideSquare(coordinate, center, unloadRadius))
                    {
                        active.Remove(coordinate);
                        toUnload.Add(coordinate);
                    }
                }
            }
            else
            {
                foreach (ChunkCoord coordinate in new List<ChunkCoord>(active))
                {
                    if (IsOutsideSquare(coordinate, center, loadRadius))
                    {
                        active.Remove(coordinate);
                        toUnload.Add(coordinate);
                    }
                }
            }

            loadOrder.Sort(toLoad, center);
            toUnload.Sort(CompareCoordinates);
            return new ChunkStreamingDelta(new List<ChunkCoord>(toLoad), new List<ChunkCoord>(toUnload));
        }

        public void Reset()
        {
            active.Clear();
            toLoad.Clear();
            toUnload.Clear();
        }

        private static bool IsOutsideSquare(ChunkCoord coordinate, ChunkCoord center, int radius)
        {
            return Math.Abs((long)coordinate.X - center.X) > radius ||
                   Math.Abs((long)coordinate.Y - center.Y) > radius;
        }

        private static int CompareCoordinates(ChunkCoord left, ChunkCoord right)
        {
            int y = left.Y.CompareTo(right.Y);
            return y != 0 ? y : left.X.CompareTo(right.X);
        }
    }

    public sealed class WorldChunkStreamingController
    {
        private readonly ProceduralWorldGenerator generator;
        private readonly ChunkStreamingPlanner planner;
        private readonly IWorldChunkSink sink;

        public ProceduralWorldGenerator Generator => generator;
        public ChunkStreamingPlanner Planner => planner;
        public IWorldChunkSink Sink => sink;

        public WorldChunkStreamingController(
            ProceduralWorldGenerator generator,
            ChunkStreamingPlanner planner,
            IWorldChunkSink sink)
        {
            this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public ChunkStreamingDelta Update(ChunkCoord center)
        {
            ChunkStreamingDelta delta = planner.Update(center);

            for (int i = 0; i < delta.ToUnload.Count; i++)
                sink.Unload(delta.ToUnload[i]);

            for (int i = 0; i < delta.ToLoad.Count; i++)
            {
                ChunkCoord coordinate = delta.ToLoad[i];
                sink.Load(coordinate, generator.GenerateChunk(coordinate));
            }

            return delta;
        }

        public void Reset()
        {
            planner.Reset();
        }
    }
}
