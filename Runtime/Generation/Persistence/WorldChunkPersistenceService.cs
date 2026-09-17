using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Applies persisted cell overrides on top of deterministic generation and saves only differences.
    /// </summary>
    public sealed class WorldChunkPersistenceService
    {
        private readonly ProceduralWorldGenerator generator;
        private readonly IWorldChunkStore store;

        public ProceduralWorldGenerator Generator => generator;
        public IWorldChunkStore Store => store;

        public WorldChunkPersistenceService(
            ProceduralWorldGenerator generator,
            IWorldChunkStore store)
        {
            this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public GeneratedChunk LoadChunk(ChunkCoord coordinate)
        {
            GeneratedChunk chunk = generator.GenerateChunk(coordinate);

            if (!store.TryLoad(coordinate, out WorldChunkSaveData data))
                return chunk;

            ApplyModifications(chunk, data);
            return chunk;
        }

        public WorldChunkSaveData CreateSaveData(GeneratedChunk currentChunk)
        {
            if (currentChunk == null)
                throw new ArgumentNullException(nameof(currentChunk));

            GeneratedChunk generatedChunk = generator.GenerateChunk(currentChunk.Coordinate);
            var modifications = new List<WorldCellModification>();

            for (int y = 0; y < currentChunk.Size; y++)
            {
                for (int x = 0; x < currentChunk.Size; x++)
                {
                    GeneratedCell current = currentChunk.GetCell(x, y);
                    GeneratedCell generated = generatedChunk.GetCell(x, y);

                    if (!WorldPersistenceUtility.AreEqual(current, generated))
                        modifications.Add(new WorldCellModification(x, y, current));
                }
            }

            return new WorldChunkSaveData(currentChunk.Coordinate, modifications);
        }

        public WorldChunkSaveData SaveChunk(GeneratedChunk currentChunk)
        {
            WorldChunkSaveData data = CreateSaveData(currentChunk);

            if (data.Modifications.Count == 0)
                store.Delete(data.Coordinate);
            else
                store.Save(data);

            return data;
        }

        public bool DeleteChunk(ChunkCoord coordinate)
        {
            return store.Delete(coordinate);
        }

        private static void ApplyModifications(GeneratedChunk chunk, WorldChunkSaveData data)
        {
            if (data.Coordinate != chunk.Coordinate)
                throw new InvalidOperationException(
                    $"Persistence data coordinate {data.Coordinate} does not match requested chunk {chunk.Coordinate}.");

            for (int i = 0; i < data.Modifications.Count; i++)
            {
                WorldCellModification modification = data.Modifications[i];
                if ((uint)modification.X >= chunk.Size || (uint)modification.Y >= chunk.Size)
                    throw new InvalidOperationException(
                        $"Persistence data contains an out-of-range cell ({modification.X}, {modification.Y}) for {chunk.Coordinate}.");

                chunk.SetCell(modification.X, modification.Y, modification.Cell);
            }
        }
    }
}
