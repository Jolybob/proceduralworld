using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Converts region identity into terrain using data-driven catalogs.
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
                    RegionId regionId = new RegionId(cell.Biome);
                    RegionDefinition region = regionCatalog.Get(regionId);
                    TerrainDefinition terrain = terrainCatalog.Get(region.DefaultTerrain);
                    cell.Tile = terrain.Tile;
                    context.Chunk.SetCell(x, y, cell);
                }
            }
        }
    }
}
