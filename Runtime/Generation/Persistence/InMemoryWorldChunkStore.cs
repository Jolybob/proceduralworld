using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Simple in-memory chunk store useful for tests, prototypes, and editor tooling.
    /// </summary>
    public sealed class InMemoryWorldChunkStore : IWorldChunkStore
    {
        private readonly Dictionary<ChunkCoord, WorldChunkSaveData> data =
            new Dictionary<ChunkCoord, WorldChunkSaveData>();

        public int Count => data.Count;

        public bool TryLoad(ChunkCoord coordinate, out WorldChunkSaveData saveData)
        {
            return data.TryGetValue(coordinate, out saveData);
        }

        public void Save(WorldChunkSaveData saveData)
        {
            if (saveData == null)
                throw new System.ArgumentNullException(nameof(saveData));

            data[saveData.Coordinate] = saveData;
        }

        public bool Delete(ChunkCoord coordinate)
        {
            return data.Remove(coordinate);
        }

        public void Clear()
        {
            data.Clear();
        }
    }
}
