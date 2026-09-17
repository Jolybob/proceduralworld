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

                for (int ownerY = minOwnerY; ownerY <= maxOwnerY; ownerY++)
                {
                    for (int ownerX = minOwnerX; ownerX <= maxOwnerX; ownerX++)
                    {
                        CollectOwnerChunk(
                            context,
                            definition,
                            new ChunkCoord(ownerX, ownerY),
                            output);
                    }
                }
            }
        }

        private static void CollectOwnerChunk(
            WorldGenerationContext context,
            StructureDefinition definition,
            ChunkCoord ownerChunk,
            StructurePlacementSet output)
        {
            int chunkSize = context.Chunk.Size;
            IWorldRandom random = context.Random.Create(
                ownerChunk,
                WorldRandomDomain.Structures,
                definition.Id.Value);

            int attempts = Math.Max(chunkSize * chunkSize, definition.Width * definition.Height * 4);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int anchorX = random.NextInt(0, chunkSize);
                int anchorY = random.NextInt(0, chunkSize);
                int worldX = ownerChunk.X * chunkSize + anchorX;
                int worldY = ownerChunk.Y * chunkSize + anchorY;

                if (!FarEnoughFromOrigin(worldX, worldY, definition.MinimumDistanceFromOrigin))
                    continue;
                if (!random.Chance(definition.SpawnChance))
                    continue;

                output.Add(new StructurePlacement(
                    definition,
                    new WorldPosition(worldX, worldY),
                    ownerChunk));

                if (CountOwnedPlacements(output, definition, ownerChunk) >= definition.MaxPerChunk)
                    return;
            }
        }

        private static int CountOwnedPlacements(
            StructurePlacementSet placements,
            StructureDefinition definition,
            ChunkCoord ownerChunk)
        {
            int count = 0;
            foreach (StructurePlacement placement in placements.Placements)
            {
                if (placement.Definition.Id == definition.Id && placement.OwnerChunk == ownerChunk)
                    count++;
            }

            return count;
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

        private static bool FarEnoughFromOrigin(int worldX, int worldY, int minimumDistance)
        {
            long squaredDistance = (long)worldX * worldX + (long)worldY * worldY;
            long minimumSquared = (long)minimumDistance * minimumDistance;
            return squaredDistance >= minimumSquared;
        }
    }
}
