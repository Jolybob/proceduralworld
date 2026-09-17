using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Persistence-aware streaming controller that separates active-chunk planning from
    /// generation scheduling. Chunks are restored only when their generation request is
    /// actually processed, and modified loaded chunks are saved before unload/reset.
    /// </summary>
    public sealed class WorldScheduledPersistentChunkStreamingController : IWorldChunkAccess
    {
        private readonly WorldChunkPersistenceService persistence;
        private readonly ChunkStreamingPlanner planner;
        private readonly IWorldChunkSink sink;
        private readonly IChunkGenerationScheduler scheduler;
        private readonly BudgetedChunkGenerationService generation;
        private readonly List<GeneratedChunk> generatedBuffer = new List<GeneratedChunk>();
        private readonly Dictionary<ChunkCoord, GeneratedChunk> loadedChunks =
            new Dictionary<ChunkCoord, GeneratedChunk>();

        public WorldChunkPersistenceService Persistence => persistence;
        public ChunkStreamingPlanner Planner => planner;
        public IWorldChunkSink Sink => sink;
        public IChunkGenerationScheduler Scheduler => scheduler;
        public int PendingGenerations => scheduler.Count;
        public IReadOnlyDictionary<ChunkCoord, GeneratedChunk> LoadedChunks => loadedChunks;

        public WorldScheduledPersistentChunkStreamingController(
            WorldChunkPersistenceService persistence,
            ChunkStreamingPlanner planner,
            IWorldChunkSink sink,
            IChunkGenerationScheduler scheduler = null)
        {
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
            this.scheduler = scheduler ?? new DeterministicChunkGenerationScheduler();
            generation = new BudgetedChunkGenerationService(
                new PersistentChunkGenerator(this.persistence),
                this.scheduler);
        }

        public ChunkStreamingDelta Update(ChunkCoord center)
        {
            ChunkStreamingDelta delta = planner.Update(center);

            for (int i = 0; i < delta.ToUnload.Count; i++)
            {
                ChunkCoord coordinate = delta.ToUnload[i];
                scheduler.Cancel(coordinate);
                Unload(coordinate);
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
                loadedChunks[chunk.Coordinate] = chunk;
                sink.Load(chunk.Coordinate, chunk);
            }

            return processed;
        }

        public bool TryGetChunk(ChunkCoord coordinate, out GeneratedChunk chunk)
        {
            return loadedChunks.TryGetValue(coordinate, out chunk);
        }

        public bool TryGetCell(WorldPosition position, out GeneratedCell cell)
        {
            if (!TryFindLoadedChunkSize(out int chunkSize))
            {
                cell = default(GeneratedCell);
                return false;
            }

            ChunkCoord coordinate = WorldChunkCoordinates.ToChunk(position, chunkSize);
            if (!loadedChunks.TryGetValue(coordinate, out GeneratedChunk chunk))
            {
                cell = default(GeneratedCell);
                return false;
            }

            int localX = WorldChunkCoordinates.ToLocalX(position, chunk.Size);
            int localY = WorldChunkCoordinates.ToLocalY(position, chunk.Size);
            cell = chunk.GetCell(localX, localY);
            return true;
        }

        public bool SetCell(WorldPosition position, GeneratedCell cell)
        {
            if (!TryFindLoadedChunkSize(out int chunkSize))
                return false;

            ChunkCoord coordinate = WorldChunkCoordinates.ToChunk(position, chunkSize);
            if (!loadedChunks.TryGetValue(coordinate, out GeneratedChunk chunk))
                return false;

            int localX = WorldChunkCoordinates.ToLocalX(position, chunk.Size);
            int localY = WorldChunkCoordinates.ToLocalY(position, chunk.Size);
            chunk.SetCell(localX, localY, cell);
            return true;
        }

        public void Reset()
        {
            foreach (KeyValuePair<ChunkCoord, GeneratedChunk> pair in
                     new List<KeyValuePair<ChunkCoord, GeneratedChunk>>(loadedChunks))
            {
                persistence.SaveChunk(pair.Value);
                sink.Unload(pair.Key);
            }

            loadedChunks.Clear();
            planner.Reset();
            scheduler.Clear();
            generatedBuffer.Clear();
        }

        private bool TryFindLoadedChunkSize(out int chunkSize)
        {
            foreach (GeneratedChunk chunk in loadedChunks.Values)
            {
                chunkSize = chunk.Size;
                return true;
            }

            chunkSize = 0;
            return false;
        }

        private void Unload(ChunkCoord coordinate)
        {
            if (!loadedChunks.TryGetValue(coordinate, out GeneratedChunk chunk))
            {
                sink.Unload(coordinate);
                return;
            }

            persistence.SaveChunk(chunk);
            loadedChunks.Remove(coordinate);
            sink.Unload(coordinate);
        }

        private sealed class PersistentChunkGenerator : IWorldChunkGenerator
        {
            private readonly WorldChunkPersistenceService persistence;

            public PersistentChunkGenerator(WorldChunkPersistenceService persistence)
            {
                this.persistence = persistence;
            }

            public GeneratedChunk GenerateChunk(ChunkCoord coordinate)
            {
                return persistence.LoadChunk(coordinate);
            }
        }
    }
}
