using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Resolves region identity into terrain identity and presentation tile through catalogs.
    /// </summary>
    public sealed class TerrainPass : IWorldGenerationPass
    {
        public int Order => 200;

        private readonly RegionCatalog regionCatalog;
        private readonly TerrainCatalog terrainCatalog;

        public TerrainPass()
            : this(RegionCatalog.CreateDefault(), TerrainCatalog.CreateDefault())
        {
        }

        public TerrainPass(RegionCatalog regionCatalog, TerrainCatalog terrainCatalog)
        {
            this.regionCatalog = regionCatalog ?? throw new ArgumentNullException(nameof(regionCatalog));
            this.terrainCatalog = terrainCatalog ?? throw new ArgumentNullException(nameof(terrainCatalog));
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            int size = context.Chunk.Size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var cell = context.Chunk.GetCell(x, y);
                    RegionId regionId = ResolveRegionId(cell);
                    RegionDefinition region = regionCatalog.Get(regionId);
                    TerrainDefinition terrain = terrainCatalog.Get(region.DefaultTerrain);
                    cell.SetRegion(regionId);
                    cell.SetTerrain(terrain.Id, terrain.Tile);
                    context.Chunk.SetCell(x, y, cell);
                }
            }
        }

        private static RegionId ResolveRegionId(GeneratedCell cell)
        {
            // Legacy integrations may still write Biome directly. Canonical state wins when
            // both fields agree; a mismatch falls back to the legacy value and re-synchronizes it.
            if (cell.Region.Value != cell.Biome)
                return new RegionId(cell.Biome);

            return cell.Region;
        }
    }
}
