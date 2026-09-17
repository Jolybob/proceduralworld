using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Deterministically places resource deposits on eligible generated terrain.
    /// Deposits are grown from an anchor cell using a deterministic local flood-fill.
    /// Rendering adapters decide how resources are presented.
    /// </summary>
    public sealed class ResourcePass : IWorldGenerationPass
    {
        public int Order => 400;

        private readonly ResourceCatalog catalog;

        public ResourcePass(ResourceCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!context.Settings.resourcesEnabled)
                return;

            int size = context.Chunk.Size;
            foreach (ResourceDefinition definition in catalog.Definitions)
                PlaceResource(definition, context, size);
        }

        private static void PlaceResource(ResourceDefinition definition, WorldGenerationContext context, int size)
        {
            IWorldRandom random = context.Random.Create(
                context.ChunkCoordinate,
                WorldRandomDomain.Resources,
                definition.Id.Value);

            int placedDeposits = 0;
            int cellCount = size * size;
            int attempts = Math.Max(cellCount, definition.MaxPerChunk * 8);

            for (int attempt = 0; attempt < attempts && placedDeposits < definition.MaxPerChunk; attempt++)
            {
                int index = random.NextInt(0, cellCount);
                int anchorX = index % size;
                int anchorY = index / size;
                var anchor = context.Chunk.GetCell(anchorX, anchorY);

                if (!IsEligible(anchor, definition))
                    continue;

                int worldX = context.ChunkCoordinate.X * size + anchorX;
                int worldY = context.ChunkCoordinate.Y * size + anchorY;
                if (!FarEnoughFromOrigin(worldX, worldY, definition.MinimumDistanceFromOrigin))
                    continue;

                if (!random.Chance(definition.SpawnChance))
                    continue;

                int targetSize = random.NextInt(
                    definition.DepositSizeMin,
                    definition.DepositSizeMax + 1);

                int deposited = GrowDeposit(
                    context,
                    definition,
                    anchorX,
                    anchorY,
                    targetSize,
                    random,
                    size);

                if (deposited > 0)
                    placedDeposits++;
            }
        }

        private static int GrowDeposit(
            WorldGenerationContext context,
            ResourceDefinition definition,
            int anchorX,
            int anchorY,
            int targetSize,
            IWorldRandom random,
            int size)
        {
            if (targetSize <= 0 || !IsEligible(context.Chunk.GetCell(anchorX, anchorY), definition))
                return 0;

            int deposited = 0;
            int[] frontierX = new int[targetSize];
            int[] frontierY = new int[targetSize];
            int frontierCount = 1;
            frontierX[0] = anchorX;
            frontierY[0] = anchorY;

            StampResource(context, definition, anchorX, anchorY);
            deposited++;

            while (frontierCount > 0 && deposited < targetSize)
            {
                int frontierIndex = random.NextInt(0, frontierCount);
                int x = frontierX[frontierIndex];
                int y = frontierY[frontierIndex];

                frontierCount--;
                frontierX[frontierIndex] = frontierX[frontierCount];
                frontierY[frontierIndex] = frontierY[frontierCount];

                AddDepositNeighbor(context, definition, x + 1, y, random, frontierX, frontierY, ref frontierCount, ref deposited, targetSize, size);
                AddDepositNeighbor(context, definition, x - 1, y, random, frontierX, frontierY, ref frontierCount, ref deposited, targetSize, size);
                AddDepositNeighbor(context, definition, x, y + 1, random, frontierX, frontierY, ref frontierCount, ref deposited, targetSize, size);
                AddDepositNeighbor(context, definition, x, y - 1, random, frontierX, frontierY, ref frontierCount, ref deposited, targetSize, size);
            }

            return deposited;
        }

        private static void AddDepositNeighbor(
            WorldGenerationContext context,
            ResourceDefinition definition,
            int x,
            int y,
            IWorldRandom random,
            int[] frontierX,
            int[] frontierY,
            ref int frontierCount,
            ref int deposited,
            int targetSize,
            int size)
        {
            if (deposited >= targetSize || frontierCount >= frontierX.Length)
                return;
            if ((uint)x >= size || (uint)y >= size)
                return;
            if (!random.Chance(definition.DepositGrowthChance))
                return;

            var cell = context.Chunk.GetCell(x, y);
            if (!IsEligible(cell, definition))
                return;

            StampResource(context, definition, x, y);
            frontierX[frontierCount] = x;
            frontierY[frontierCount] = y;
            frontierCount++;
            deposited++;
        }

        private static void StampResource(
            WorldGenerationContext context,
            ResourceDefinition definition,
            int x,
            int y)
        {
            var cell = context.Chunk.GetCell(x, y);
            cell.SetResource(definition.Id);
            context.Chunk.SetCell(x, y, cell);
        }

        private static bool IsEligible(GeneratedCell cell, ResourceDefinition definition)
        {
            if ((cell.Flags & (GeneratedCellFlags.Carved | GeneratedCellFlags.Reserved | GeneratedCellFlags.HasResource)) != 0)
                return false;

            return cell.Region == definition.Region && cell.Terrain == definition.Terrain;
        }

        private static bool FarEnoughFromOrigin(int worldX, int worldY, int minimumDistance)
        {
            long squaredDistance = (long)worldX * worldX + (long)worldY * worldY;
            long minimumSquared = (long)minimumDistance * minimumDistance;
            return squaredDistance >= minimumSquared;
        }
    }
}
