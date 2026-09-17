using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Supplies world-space feature placements that intersect the requested chunk.
    /// Implementations must be deterministic for a fixed world seed and placement definition set.
    /// </summary>
    public interface IWorldFeaturePlacementSource
    {
        void Collect(WorldGenerationContext context, WorldFeaturePlacementSet output);
    }

    /// <summary>
    /// Deterministically discovers owner chunks whose possible feature footprints can intersect
    /// the requested chunk, then delegates anchor generation to <see cref="WorldFeaturePlacementPlanner"/>.
    /// </summary>
    public sealed class DeterministicWorldFeaturePlacementSource :
        IWorldFeaturePlacementSource,
        IWorldFeaturePlacementQuerySource
    {
        private readonly IWorldFeaturePlacementDefinition[] definitions;
        private readonly WorldRandomDomain randomDomain;

        public DeterministicWorldFeaturePlacementSource(
            IEnumerable<IWorldFeaturePlacementDefinition> definitions,
            WorldRandomDomain randomDomain)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            this.definitions = new List<IWorldFeaturePlacementDefinition>(definitions).ToArray();
            if (this.definitions.Length == 0)
                throw new ArgumentException("Feature placement definitions cannot be empty.", nameof(definitions));

            for (int i = 0; i < this.definitions.Length; i++)
            {
                if (this.definitions[i] == null)
                    throw new ArgumentException("Feature placement definitions cannot contain null entries.", nameof(definitions));
            }

            this.randomDomain = randomDomain;
        }

        public void Collect(WorldGenerationContext context, WorldFeaturePlacementSet output)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            Collect(
                new WorldFeaturePlacementQueryContext(
                    context.Seed,
                    context.Chunk.Size,
                    context.ChunkCoordinate),
                output);
        }

        public void Collect(WorldFeaturePlacementQueryContext context, WorldFeaturePlacementSet output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            int chunkSize = context.ChunkSize;
            var planner = new WorldFeaturePlacementPlanner(context.Seed);

            for (int i = 0; i < definitions.Length; i++)
            {
                IWorldFeaturePlacementDefinition definition = definitions[i];
                int minOwnerX = OwnerChunkForAnchor(
                    ((long)context.ChunkCoordinate.X * chunkSize) - (definition.Width - 1L),
                    chunkSize);
                int maxOwnerX = OwnerChunkForAnchor(
                    ((long)context.ChunkCoordinate.X * chunkSize) + chunkSize - 1L,
                    chunkSize);
                int minOwnerY = OwnerChunkForAnchor(
                    ((long)context.ChunkCoordinate.Y * chunkSize) - (definition.Height - 1L),
                    chunkSize);
                int maxOwnerY = OwnerChunkForAnchor(
                    ((long)context.ChunkCoordinate.Y * chunkSize) + chunkSize - 1L,
                    chunkSize);

                for (int ownerY = minOwnerY; ; ownerY++)
                {
                    for (int ownerX = minOwnerX; ; ownerX++)
                    {
                        planner.CollectOwnerChunk(
                            new ChunkCoord(ownerX, ownerY),
                            definition,
                            randomDomain,
                            chunkSize,
                            output);

                        if (ownerX == maxOwnerX)
                            break;
                    }

                    if (ownerY == maxOwnerY)
                        break;
                }
            }
        }

        private static int OwnerChunkForAnchor(long worldCoordinate, int chunkSize)
        {
            long quotient = worldCoordinate / chunkSize;
            long remainder = worldCoordinate % chunkSize;
            if (remainder != 0L && worldCoordinate < 0L)
                quotient--;

            if (quotient < int.MinValue || quotient > int.MaxValue)
                throw new OverflowException("Feature owner chunk coordinate exceeds the supported range.");

            return (int)quotient;
        }
    }
}
