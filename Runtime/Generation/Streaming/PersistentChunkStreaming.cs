using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Streaming controller that restores persisted chunk edits when loading and saves
    /// modified chunk state before unloading.
    /// </summary>
    public sealed class WorldPersistentChunkStreamingController : IWorldChunkAccess
    {
        private readonly WorldChunkPersistenceService persistence;
        private readonly ChunkStreamingPlanner planner;
        private readonly IWorldChunkSink sink;
        private readonly Dictionary<ChunkCoord, GeneratedChunk> loadedChunks =
            new Dictionary<ChunkCoord, GeneratedChunk>();

        public WorldChunkPersistenceService Persistence => persistence;
        public ChunkStreamingPlanner Planner => planner;
        public IWorldChunkSink Sink => sink;
        public IReadOnlyDictionary<ChunkCoord, GeneratedChunk> LoadedChunks => loadedChunks;

        public WorldPersistentChunkStreamingController(
            WorldChunkPersistenceService persistence,
            ChunkStreamingPlanner planner,
            IWorldChunkSink sink)
        {
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public ChunkStreamingDelta Update(ChunkCoord center)
        {
            ChunkStreamingDelta delta = planner.Update(center);

            for (int i = 0; i < delta.ToUnload.Count; i++)
                Unload(delta.ToUnload[i]);

            for (int i = 0; i < delta.ToLoad.Count; i++)
                Load(delta.ToLoad[i]);

            return delta;
        }

        public bool TryGetChunk(ChunkCoord coordinate, out GeneratedChunk chunk)
        {
            return loadedChunks.TryGetValue(coordinate, out chunk);
        }

        public bool TryGetCell(WorldPosition position, out GeneratedCell cell)
        {
            ChunkCoord coordinate = WorldChunkCoordinates.ToChunk(position, GetChunkSize());
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
            ChunkCoord coordinate = WorldChunkCoordinates.ToChunk(position, GetChunkSize());
            if (!loadedChunks.TryGetValue(coordinate, out GeneratedChunk chunk))
                return false;

            int localX = WorldChunkCoordinates.ToLocalX(position, chunk.Size);
            int localY = WorldChunkCoordinates.ToLocalY(position, chunk.Size);
            chunk.SetCell(localX, localY, cell);
            return true;
        }

        /// <summary>
        /// Saves and unloads every currently loaded chunk, then resets the planner.
        /// </summary>
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
        }

        private int GetChunkSize()
        {
            foreach (GeneratedChunk chunk in loadedChunks.Values)
                return chunk.Size;

            throw new InvalidOperationException(
                "World chunk access is unavailable until at least one chunk is loaded.");
        }

        private void Load(ChunkCoord coordinate)
        {
            GeneratedChunk chunk = persistence.LoadChunk(coordinate);
            loadedChunks[coordinate] = chunk;
            sink.Load(coordinate, chunk);
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
    }
}
