using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Supplies world-space structure placements that intersect the requested chunk.
    /// Implementations must be deterministic for a fixed world seed and generation settings.
    /// </summary>
    public interface IStructurePlacementSource
    {
        void Collect(WorldGenerationContext context, StructurePlacementSet output);
    }

    /// <summary>
    /// Deterministically plans structure anchors in owner chunks, then queries only the
    /// owner chunks whose possible footprints can intersect the requested chunk.
    /// </summary>
    public sealed class DeterministicStructurePlacementSource : IStructurePlacementSource
    {
        public void Collect(WorldGenerationContext context, StructurePlacementSet output)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            int chunkSize = context.Chunk.Size;
            var planner = new StructurePlacementPlanner(context.Seed);

            foreach (StructureDefinition definition in context.Structures.Definitions)
            {
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
                throw new OverflowException("Structure owner chunk coordinate exceeds the supported range.");

            return (int)quotient;
        }
    }
}
