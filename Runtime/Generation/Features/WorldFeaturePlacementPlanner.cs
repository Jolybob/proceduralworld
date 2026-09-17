using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Converts deterministic owner-chunk candidates into world-space feature placements.
    /// Anchor coordinates are local to the owner chunk, so a footprint may cross any number
    /// of neighboring chunks without changing placement identity.
    /// </summary>
    public sealed class WorldFeaturePlacementPlanner
    {
        private readonly WorldRandomService random;

        public WorldFeaturePlacementPlanner(int seed)
        {
            random = new WorldRandomService(seed);
        }

        public WorldFeaturePlacement CreatePlacement(
            ChunkCoord ownerChunk,
            IWorldFeaturePlacementDefinition definition,
            int anchorX,
            int anchorY,
            int chunkSize = 64)
        {
            ValidateDefinition(definition);
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            if ((uint)anchorX >= chunkSize)
                throw new ArgumentOutOfRangeException(nameof(anchorX));
            if ((uint)anchorY >= chunkSize)
                throw new ArgumentOutOfRangeException(nameof(anchorY));

            long worldX = (long)ownerChunk.X * chunkSize + anchorX;
            long worldY = (long)ownerChunk.Y * chunkSize + anchorY;
            if (worldX < int.MinValue || worldX > int.MaxValue)
                throw new OverflowException("Feature anchor exceeds the supported world X coordinate range.");
            if (worldY < int.MinValue || worldY > int.MaxValue)
                throw new OverflowException("Feature anchor exceeds the supported world Y coordinate range.");

            return new WorldFeaturePlacement(
                definition.FeatureId,
                new WorldPosition((int)worldX, (int)worldY),
                ownerChunk,
                definition.Width,
                definition.Height);
        }

        public int CollectOwnerChunk(
            ChunkCoord ownerChunk,
            IWorldFeaturePlacementDefinition definition,
            WorldRandomDomain randomDomain,
            int chunkSize,
            WorldFeaturePlacementSet output)
        {
            ValidateDefinition(definition);
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            IWorldRandom stream = random.Create(
                ownerChunk,
                randomDomain,
                unchecked((uint)definition.FeatureId));
            long requestedAttempts = Math.Max(
                (long)chunkSize * chunkSize,
                ScaledFootprintArea(definition.Width, definition.Height));
            int attempts = requestedAttempts > int.MaxValue
                ? int.MaxValue
                : (int)requestedAttempts;
            int placed = 0;

            for (int attempt = 0; attempt < attempts && placed < definition.MaxPerChunk; attempt++)
            {
                int anchorX = stream.NextInt(0, chunkSize);
                int anchorY = stream.NextInt(0, chunkSize);
                WorldFeaturePlacement placement = CreatePlacement(
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

        private static void ValidateDefinition(IWorldFeaturePlacementDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition.FeatureId < 0)
                throw new ArgumentOutOfRangeException(nameof(definition));
            if (definition.Width <= 0)
                throw new ArgumentOutOfRangeException(nameof(definition));
            if (definition.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(definition));
            if (definition.SpawnChance < 0f || definition.SpawnChance > 1f)
                throw new ArgumentOutOfRangeException(nameof(definition));
            if (definition.MaxPerChunk <= 0)
                throw new ArgumentOutOfRangeException(nameof(definition));
            if (definition.MinimumDistanceFromOrigin < 0)
                throw new ArgumentOutOfRangeException(nameof(definition));
        }

        private static long ScaledFootprintArea(int width, int height)
        {
            long area = (long)width * height;
            return area > long.MaxValue / 4L
                ? long.MaxValue
                : area * 4L;
        }

        private static bool FarEnoughFromOrigin(int worldX, int worldY, int minimumDistance)
        {
            long squaredDistance = (long)worldX * worldX + (long)worldY * worldY;
            long minimumSquared = (long)minimumDistance * minimumDistance;
            return squaredDistance >= minimumSquared;
        }
    }
}
