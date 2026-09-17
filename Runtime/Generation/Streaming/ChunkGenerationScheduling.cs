using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public interface IWorldChunkGenerator
    {
        GeneratedChunk GenerateChunk(ChunkCoord coordinate);
    }

    public readonly struct ChunkGenerationRequest
    {
        public ChunkCoord Coordinate { get; }
        public int Priority { get; }
        public long Sequence { get; }

        internal ChunkGenerationRequest(ChunkCoord coordinate, int priority, long sequence)
        {
            Coordinate = coordinate;
            Priority = priority;
            Sequence = sequence;
        }
    }

    public interface IChunkGenerationScheduler
    {
        int Count { get; }
        void Enqueue(ChunkCoord coordinate, int priority = 0);
        bool Contains(ChunkCoord coordinate);
        bool Cancel(ChunkCoord coordinate);
        bool TryDequeue(out ChunkGenerationRequest request);
        void Clear();
    }

    public sealed class DeterministicChunkGenerationScheduler : IChunkGenerationScheduler
    {
        private readonly SortedSet<ChunkGenerationRequest> queue = new SortedSet<ChunkGenerationRequest>(Comparer<ChunkGenerationRequest>.Create(Compare));
        private readonly Dictionary<ChunkCoord, ChunkGenerationRequest> pending = new Dictionary<ChunkCoord, ChunkGenerationRequest>();
        private long nextSequence;

        public int Count => queue.Count;

        public void Enqueue(ChunkCoord coordinate, int priority = 0)
        {
            if (pending.TryGetValue(coordinate, out ChunkGenerationRequest existing))
            {
                if (existing.Priority == priority)
                    return;

                queue.Remove(existing);
            }

            long sequence = nextSequence++;
            var request = new ChunkGenerationRequest(coordinate, priority, sequence);
            pending[coordinate] = request;
            queue.Add(request);
        }

        public bool Contains(ChunkCoord coordinate)
        {
            return pending.ContainsKey(coordinate);
        }

        public bool Cancel(ChunkCoord coordinate)
        {
            if (!pending.TryGetValue(coordinate, out ChunkGenerationRequest request))
                return false;

            pending.Remove(coordinate);
            queue.Remove(request);
            return true;
        }

        public bool TryDequeue(out ChunkGenerationRequest request)
        {
            if (queue.Count == 0)
            {
                request = default(ChunkGenerationRequest);
                return false;
            }

            request = queue.Min;
            queue.Remove(request);
            pending.Remove(request.Coordinate);
            return true;
        }

        public void Clear()
        {
            queue.Clear();
            pending.Clear();
        }

        private static int Compare(ChunkGenerationRequest left, ChunkGenerationRequest right)
        {
            int priority = left.Priority.CompareTo(right.Priority);
            if (priority != 0)
                return priority;

            int sequence = left.Sequence.CompareTo(right.Sequence);
            if (sequence != 0)
                return sequence;

            int y = left.Coordinate.Y.CompareTo(right.Coordinate.Y);
            return y != 0 ? y : left.Coordinate.X.CompareTo(right.Coordinate.X);
        }
    }

    public sealed class BudgetedChunkGenerationService
    {
        private readonly IWorldChunkGenerator generator;
        private readonly IChunkGenerationScheduler scheduler;

        public IWorldChunkGenerator Generator => generator;
        public IChunkGenerationScheduler Scheduler => scheduler;

        public BudgetedChunkGenerationService(
            IWorldChunkGenerator generator,
            IChunkGenerationScheduler scheduler)
        {
            this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public int Process(int maxChunks, IList<GeneratedChunk> generatedChunks)
        {
            if (maxChunks < 0)
                throw new ArgumentOutOfRangeException(nameof(maxChunks));
            if (generatedChunks == null)
                throw new ArgumentNullException(nameof(generatedChunks));

            int processed = 0;
            while (processed < maxChunks && scheduler.TryDequeue(out ChunkGenerationRequest request))
            {
                generatedChunks.Add(generator.GenerateChunk(request.Coordinate));
                processed++;
            }

            return processed;
        }
    }

    public sealed class WorldScheduledChunkStreamingController
    {
        private readonly IWorldChunkGenerator generator;
        private readonly ChunkStreamingPlanner planner;
        private readonly IWorldChunkSink sink;
        private readonly IChunkGenerationScheduler scheduler;
        private readonly BudgetedChunkGenerationService generation;
        private readonly List<GeneratedChunk> generatedBuffer = new List<GeneratedChunk>();
        private readonly HashSet<ChunkCoord> loadedChunks = new HashSet<ChunkCoord>();

        public IWorldChunkGenerator Generator => generator;
        public ChunkStreamingPlanner Planner => planner;
        public IWorldChunkSink Sink => sink;
        public IChunkGenerationScheduler Scheduler => scheduler;
        public IReadOnlyCollection<ChunkCoord> LoadedChunks => loadedChunks;
        public int PendingGenerations => scheduler.Count;

        public WorldScheduledChunkStreamingController(
            IWorldChunkGenerator generator,
            ChunkStreamingPlanner planner,
            IWorldChunkSink sink,
            IChunkGenerationScheduler scheduler = null)
        {
            this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
            this.scheduler = scheduler ?? new DeterministicChunkGenerationScheduler();
            generation = new BudgetedChunkGenerationService(this.generator, this.scheduler);
        }

        public ChunkStreamingState GetState(ChunkCoord coordinate)
        {
            if (loadedChunks.Contains(coordinate))
                return ChunkStreamingState.Loaded;
            if (scheduler.Contains(coordinate))
                return ChunkStreamingState.Pending;
            return ChunkStreamingState.Inactive;
        }

        public ChunkStreamingDelta Update(ChunkCoord center)
        {
            ChunkStreamingDelta delta = planner.Update(center);

            for (int i = 0; i < delta.ToUnload.Count; i++)
            {
                ChunkCoord coordinate = delta.ToUnload[i];
                scheduler.Cancel(coordinate);
                loadedChunks.Remove(coordinate);
                sink.Unload(coordinate);
            }

            for (int i = 0; i < delta.ToLoad.Count; i++)
                scheduler.Enqueue(delta.ToLoad[i], i);

            return delta;
        }

        public int Process(int maxChunks)
        {
            generatedBuffer.Clear();
            int processed = generation.Process(maxChunks, generatedBuffer);

            for (int i = 0; i < generatedBuffer.Count; i++)
            {
                GeneratedChunk chunk = generatedBuffer[i];
                loadedChunks.Add(chunk.Coordinate);
                sink.Load(chunk.Coordinate, chunk);
            }

            return processed;
        }

        public void Reset()
        {
            planner.Reset();
            scheduler.Clear();
            generatedBuffer.Clear();
            loadedChunks.Clear();
        }
    }
}
