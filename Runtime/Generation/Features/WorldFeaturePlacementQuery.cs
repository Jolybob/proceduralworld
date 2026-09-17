using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Minimal deterministic context required to query world-space feature placements.
    /// It deliberately excludes mutable chunk data so feature discovery can run without
    /// generating or materializing the target chunk first.
    /// </summary>
    public readonly struct WorldFeaturePlacementQueryContext
    {
        public int Seed { get; }
        public int ChunkSize { get; }
        public ChunkCoord ChunkCoordinate { get; }

        public WorldFeaturePlacementQueryContext(int seed, int chunkSize, ChunkCoord chunkCoordinate)
        {
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));

            Seed = seed;
            ChunkSize = chunkSize;
            ChunkCoordinate = chunkCoordinate;
        }
    }

    /// <summary>
    /// Supplies deterministic world-space feature placements without requiring a generated chunk.
    /// </summary>
    public interface IWorldFeaturePlacementQuerySource
    {
        void Collect(WorldFeaturePlacementQueryContext context, WorldFeaturePlacementSet output);
    }
}
