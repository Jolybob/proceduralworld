using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Session-scoped cache and spatial query facade for deterministic world-space feature placements.
    /// Each chunk's placement set is planned at most once until that chunk is invalidated or the index is cleared.
    /// </summary>
    public sealed class WorldFeaturePlacementIndex
    {
        private readonly IWorldFeaturePlacementQuerySource source;
        private readonly int seed;
        private readonly int chunkSize;
        private readonly Dictionary<ChunkCoord, WorldFeaturePlacementSet> cache =
            new Dictionary<ChunkCoord, WorldFeaturePlacementSet>();

        public int CachedChunkCount => cache.Count;

        public WorldFeaturePlacementIndex(
            int seed,
            int chunkSize,
            IWorldFeaturePlacementQuerySource source)
        {
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.seed = seed;
            this.chunkSize = chunkSize;
        }

        /// <summary>
        /// Returns all feature placements intersecting a chunk, generating that query once and caching it.
        /// </summary>
        public IReadOnlyList<WorldFeaturePlacement> GetChunk(ChunkCoord coordinate)
        {
            if (!cache.TryGetValue(coordinate, out WorldFeaturePlacementSet placements))
            {
                placements = new WorldFeaturePlacementSet();
                source.Collect(
                    new WorldFeaturePlacementQueryContext(seed, chunkSize, coordinate),
                    placements);
                cache.Add(coordinate, placements);
            }

            return placements.Placements;
        }

        /// <summary>
        /// Adds every placement containing the given world position to <paramref name="output"/>.
        /// Point queries resolve exactly one chunk using mathematical floor division, including negatives.
        /// </summary>
        public int CollectContaining(WorldPosition position, WorldFeaturePlacementSet output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            int before = output.Count;
            ChunkCoord chunk = WorldToChunk(position, chunkSize);
            IReadOnlyList<WorldFeaturePlacement> placements = GetChunk(chunk);

            for (int i = 0; i < placements.Count; i++)
            {
                WorldFeaturePlacement placement = placements[i];
                if (placement.Contains(position))
                    output.Add(placement);
            }

            return output.Count - before;
        }

        /// <summary>
        /// Adds every placement intersecting the inclusive world-space rectangle to <paramref name="output"/>.
        /// Overlapping discoveries are de-duplicated by placement identity.
        /// </summary>
        public int CollectIntersecting(
            WorldPosition minInclusive,
            WorldPosition maxInclusive,
            WorldFeaturePlacementSet output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (minInclusive.X > maxInclusive.X)
                throw new ArgumentException("Minimum world X must not exceed maximum world X.");
            if (minInclusive.Y > maxInclusive.Y)
                throw new ArgumentException("Minimum world Y must not exceed maximum world Y.");

            int before = output.Count;
            ChunkCoord minChunk = WorldToChunk(minInclusive, chunkSize);
            ChunkCoord maxChunk = WorldToChunk(maxInclusive, chunkSize);

            for (int chunkY = minChunk.Y; ; chunkY++)
            {
                for (int chunkX = minChunk.X; ; chunkX++)
                {
                    IReadOnlyList<WorldFeaturePlacement> placements = GetChunk(
                        new ChunkCoord(chunkX, chunkY));
                    for (int i = 0; i < placements.Count; i++)
                    {
                        WorldFeaturePlacement placement = placements[i];
                        if (IntersectsWorldRectangle(
                            placement,
                            minInclusive,
                            maxInclusive))
                            output.Add(placement);
                    }

                    if (chunkX == maxChunk.X)
                        break;
                }

                if (chunkY == maxChunk.Y)
                    break;
            }

            return output.Count - before;
        }

        /// <summary>Forces one chunk query to be planned again on its next access.</summary>
        public bool Invalidate(ChunkCoord coordinate)
        {
            return cache.Remove(coordinate);
        }

        /// <summary>Clears all cached placement queries.</summary>
        public void Clear()
        {
            cache.Clear();
        }

        private static bool IntersectsWorldRectangle(
            WorldFeaturePlacement placement,
            WorldPosition minInclusive,
            WorldPosition maxInclusive)
        {
            return (long)placement.MinX <= maxInclusive.X
                && (long)placement.MaxX >= minInclusive.X
                && (long)placement.MinY <= maxInclusive.Y
                && (long)placement.MaxY >= minInclusive.Y;
        }

        private static ChunkCoord WorldToChunk(WorldPosition position, int size)
        {
            return new ChunkCoord(
                FloorDiv(position.X, size),
                FloorDiv(position.Y, size));
        }

        private static int FloorDiv(int value, int divisor)
        {
            long quotient = value / divisor;
            long remainder = value % divisor;
            if (remainder != 0L && value < 0)
                quotient--;

            if (quotient < int.MinValue || quotient > int.MaxValue)
                throw new OverflowException("World-to-chunk conversion exceeds the supported chunk coordinate range.");

            return (int)quotient;
        }
    }
}
