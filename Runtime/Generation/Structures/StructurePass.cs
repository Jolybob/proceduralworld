using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Deterministically places multi-cell structure footprints on eligible generated terrain.
    /// This pass only writes generated data; rendering and prefab spawning remain adapter concerns.
    /// </summary>
    public sealed class StructurePass : IWorldGenerationPass
    {
        public int Order => 500;

        private readonly StructureCatalog catalog;

        public StructurePass(StructureCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!context.Settings.structuresEnabled)
                return;

            foreach (StructureDefinition definition in catalog.Definitions)
                PlaceStructure(definition, context);
        }

        private static void PlaceStructure(StructureDefinition definition, WorldGenerationContext context)
        {
            int size = context.Chunk.Size;
            IWorldRandom random = context.Random.Create(
                context.ChunkCoordinate,
                WorldRandomDomain.Structures,
                definition.Id.Value);

            int placed = 0;
            int attempts = Math.Max(size * size, definition.Width * definition.Height * 4);

            for (int attempt = 0; attempt < attempts && placed < definition.MaxPerChunk; attempt++)
            {
                int maxX = size - definition.Width;
                int maxY = size - definition.Height;
                if (maxX < 0 || maxY < 0)
                    return;

                int anchorX = random.NextInt(0, maxX + 1);
                int anchorY = random.NextInt(0, maxY + 1);

                int worldX = context.ChunkCoordinate.X * size + anchorX;
                int worldY = context.ChunkCoordinate.Y * size + anchorY;
                if (!FarEnoughFromOrigin(worldX, worldY, definition.MinimumDistanceFromOrigin))
                    continue;

                if (!FootprintEligible(context, definition, anchorX, anchorY))
                    continue;

                if (!random.Chance(definition.SpawnChance))
                    continue;

                StampFootprint(context, definition, anchorX, anchorY);
                placed++;
            }
        }

        private static bool FootprintEligible(
            WorldGenerationContext context,
            StructureDefinition definition,
            int anchorX,
            int anchorY)
        {
            for (int y = 0; y < definition.Height; y++)
            {
                for (int x = 0; x < definition.Width; x++)
                {
                    var cell = context.Chunk.GetCell(anchorX + x, anchorY + y);

                    if ((cell.Flags & GeneratedCellFlags.Carved) != 0)
                        return false;
                    if ((cell.Flags & GeneratedCellFlags.HasResource) != 0)
                        return false;
                    if ((cell.Flags & GeneratedCellFlags.HasStructure) != 0)
                        return false;
                    if (cell.Region != definition.Region || cell.Terrain != definition.Terrain)
                        return false;
                }
            }

            return true;
        }

        private static void StampFootprint(
            WorldGenerationContext context,
            StructureDefinition definition,
            int anchorX,
            int anchorY)
        {
            for (int y = 0; y < definition.Height; y++)
            {
                for (int x = 0; x < definition.Width; x++)
                {
                    var cell = context.Chunk.GetCell(anchorX + x, anchorY + y);
                    cell.SetStructure(definition.Id);
                    context.Chunk.SetCell(anchorX + x, anchorY + y, cell);
                }
            }
        }

        private static bool FarEnoughFromOrigin(int worldX, int worldY, int minimumDistance)
        {
            long squaredDistance = (long)worldX * worldX + (long)worldY * worldY;
            long minimumSquared = (long)minimumDistance * minimumDistance;
            return squaredDistance >= minimumSquared;
        }
    }
}
