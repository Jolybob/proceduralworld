using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Materializes world-space structure placements into the current chunk.
    /// Placement ownership and footprint calculation live in <see cref="IStructurePlacementSource"/>.
    /// </summary>
    public sealed class StructurePlacementPass : IWorldGenerationPass
    {
        public int Order => 500;

        private readonly StructureCatalog catalog;
        private readonly IStructurePlacementSource source;

        public StructurePlacementPass(
            StructureCatalog catalog,
            IStructurePlacementSource source = null)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.source = source ?? new DeterministicStructurePlacementSource();
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!context.Settings.structuresEnabled)
                return;

            var placements = new StructurePlacementSet();
            source.Collect(context, placements);

            foreach (StructurePlacement placement in placements.Placements)
            {
                if (!catalog.TryGet(placement.Definition.Id, out StructureDefinition definition))
                    continue;
                if (!CanMaterializeLocalIntersection(context, placement, definition))
                    continue;

                StampLocalIntersection(context, placement, definition);
            }
        }

        private static bool CanMaterializeLocalIntersection(
            WorldGenerationContext context,
            StructurePlacement placement,
            StructureDefinition definition)
        {
            int size = context.Chunk.Size;
            long chunkMinX = (long)context.ChunkCoordinate.X * size;
            long chunkMinY = (long)context.ChunkCoordinate.Y * size;
            long chunkMaxX = chunkMinX + size - 1L;
            long chunkMaxY = chunkMinY + size - 1L;

            int minWorldX = Math.Max(placement.MinX, ToInt(chunkMinX));
            int maxWorldX = Math.Min(placement.MaxX, ToInt(chunkMaxX));
            int minWorldY = Math.Max(placement.MinY, ToInt(chunkMinY));
            int maxWorldY = Math.Min(placement.MaxY, ToInt(chunkMaxY));

            for (int worldY = minWorldY; worldY <= maxWorldY; worldY++)
            {
                for (int worldX = minWorldX; worldX <= maxWorldX; worldX++)
                {
                    int localX = worldX - ToInt(chunkMinX);
                    int localY = worldY - ToInt(chunkMinY);
                    GeneratedCell cell = context.Chunk.GetCell(localX, localY);

                    if ((cell.Flags & (GeneratedCellFlags.Carved
                        | GeneratedCellFlags.Reserved
                        | GeneratedCellFlags.HasResource
                        | GeneratedCellFlags.HasStructure)) != 0)
                        return false;
                    if (cell.Region != definition.Region || cell.Terrain != definition.Terrain)
                        return false;
                }
            }

            return true;
        }

        private static void StampLocalIntersection(
            WorldGenerationContext context,
            StructurePlacement placement,
            StructureDefinition definition)
        {
            int size = context.Chunk.Size;
            int chunkMinX = ToInt((long)context.ChunkCoordinate.X * size);
            int chunkMinY = ToInt((long)context.ChunkCoordinate.Y * size);
            int chunkMaxX = ToInt((long)context.ChunkCoordinate.X * size + size - 1L);
            int chunkMaxY = ToInt((long)context.ChunkCoordinate.Y * size + size - 1L);

            int minWorldX = Math.Max(placement.MinX, chunkMinX);
            int maxWorldX = Math.Min(placement.MaxX, chunkMaxX);
            int minWorldY = Math.Max(placement.MinY, chunkMinY);
            int maxWorldY = Math.Min(placement.MaxY, chunkMaxY);

            for (int worldY = minWorldY; worldY <= maxWorldY; worldY++)
            {
                for (int worldX = minWorldX; worldX <= maxWorldX; worldX++)
                {
                    int localX = worldX - chunkMinX;
                    int localY = worldY - chunkMinY;
                    GeneratedCell cell = context.Chunk.GetCell(localX, localY);
                    cell.SetStructure(definition.Id);
                    context.Chunk.SetCell(localX, localY, cell);
                }
            }
        }

        private static int ToInt(long value)
        {
            if (value < int.MinValue || value > int.MaxValue)
                throw new OverflowException("World coordinate exceeds the supported range.");
            return (int)value;
        }
    }
}
