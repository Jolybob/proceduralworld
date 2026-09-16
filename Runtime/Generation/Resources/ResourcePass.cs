using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Deterministically places resource instances on eligible generated cells.
    /// Resource placement is generation data only; rendering adapters decide how resources are presented.
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
            IWorldRandom random = context.Random.Create(context.ChunkCoordinate, WorldRandomDomain.Resources);
            int placed = 0;
            int remainingCells = size * size;

            for (int i = 0; i < remainingCells && placed < definition.MaxPerChunk; i++)
            {
                int index = SelectCandidateIndex(random, remainingCells);
                int x = index % size;
                int y = index / size;
                var cell = context.Chunk.GetCell(x, y);

                if (!IsEligible(cell, definition))
                    continue;

                int worldX = context.ChunkCoordinate.X * size + x;
                int worldY = context.ChunkCoordinate.Y * size + y;
                if (!FarEnoughFromOrigin(worldX, worldY, definition.MinimumDistanceFromOrigin))
                    continue;

                if (!random.Chance(definition.SpawnChance))
                    continue;

                cell.Resource = definition.Id;
                cell.Flags |= GeneratedCellFlags.HasResource;
                context.Chunk.SetCell(x, y, cell);
                placed++;
            }
        }

        private static int SelectCandidateIndex(IWorldRandom random, int cellCount)
        {
            return random.NextInt(0, cellCount);
        }

        private static bool IsEligible(GeneratedCell cell, ResourceDefinition definition)
        {
            if ((cell.Flags & GeneratedCellFlags.Carved) != 0)
                return false;

            return cell.Region == definition.Region && cell.Terrain == definition.Terrain;
        }

        private static bool FarEnoughFromOrigin(int worldX, int worldY, int minimumDistance)
        {
            float distance = Mathf.Sqrt((float)worldX * worldX + (float)worldY * worldY);
            return distance >= minimumDistance;
        }
    }
}
