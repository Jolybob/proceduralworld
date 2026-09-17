using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Converts deterministic owner-chunk candidates into world-space structure placements.
    /// Anchor coordinates are always local to the owner chunk, so footprints may extend
    /// into any neighboring chunk without changing the placement identity.
    /// </summary>
    public sealed class StructurePlacementPlanner
    {
        private readonly WorldRandomService random;

        public StructurePlacementPlanner(int seed)
        {
            random = new WorldRandomService(seed);
        }

        public StructurePlacement CreatePlacement(
            ChunkCoord ownerChunk,
            StructureDefinition definition,
            int anchorX,
            int anchorY,
            int chunkSize = 64)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            if ((uint)anchorX >= chunkSize)
                throw new ArgumentOutOfRangeException(nameof(anchorX));
            if ((uint)anchorY >= chunkSize)
                throw new ArgumentOutOfRangeException(nameof(anchorY));

            long worldX = (long)ownerChunk.X * chunkSize + anchorX;
            long worldY = (long)ownerChunk.Y * chunkSize + anchorY;
            if (worldX < int.MinValue || worldX > int.MaxValue)
                throw new OverflowException("Structure anchor exceeds the supported world X coordinate range.");
            if (worldY < int.MinValue || worldY > int.MaxValue)
                throw new OverflowException("Structure anchor exceeds the supported world Y coordinate range.");

            return new StructurePlacement(
                definition,
                new WorldPosition((int)worldX, (int)worldY),
                ownerChunk);
        }

        public int CollectOwnerChunk(
            ChunkCoord ownerChunk,
            StructureDefinition definition,
            int chunkSize,
            StructurePlacementSet output)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            IWorldRandom stream = random.Create(
                ownerChunk,
                WorldRandomDomain.Structures,
                definition.Id.Value);
            int attempts = Math.Max(chunkSize * chunkSize, definition.Width * definition.Height * 4);
            int placed = 0;

            for (int attempt = 0; attempt < attempts && placed < definition.MaxPerChunk; attempt++)
            {
                int anchorX = stream.NextInt(0, chunkSize);
                int anchorY = stream.NextInt(0, chunkSize);
                StructurePlacement placement = CreatePlacement(
                    ownerChunk,
                    definition,
                    anchorX,
                    anchorY,
                    chunkSize);

                if (!FarEnoughFromOrigin(
                    placement.Anchor.X,
                    placement.Anchor.Y,
                    definition.MinimumDistanceFromOrigin))
                    continue;
                if (!stream.Chance(definition.SpawnChance))
                    continue;

                if (output.Add(placement))
                    placed++;
            }

            return placed;
        }

        private static bool FarEnoughFromOrigin(int worldX, int worldY, int minimumDistance)
        {
            long squaredDistance = (long)worldX * worldX + (long)worldY * worldY;
            long minimumSquared = (long)minimumDistance * minimumDistance;
            return squaredDistance >= minimumSquared;
        }
    }
}
