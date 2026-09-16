namespace Jolybob.ProceduralWorld
{
    /// <summary>Converts region identity into the base terrain/material category.</summary>
    public sealed class TerrainPass : IWorldGenerationPass
    {
        public int Order => 200;

        public void Execute(WorldGenerationContext context)
        {
            int size = context.Chunk.Size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var cell = context.Chunk.GetCell(x, y);
                    cell.Tile = TileForRegion(cell.Biome);
                    context.Chunk.SetCell(x, y, cell);
                }
            }
        }

        private static WorldTile TileForRegion(byte region)
        {
            switch (region)
            {
                case 0: return WorldTile.Core;
                case 1: return WorldTile.Inner;
                case 2: return WorldTile.Mid;
                case 3: return WorldTile.Deep;
                case 4: return WorldTile.Mid;
                default: return WorldTile.Deep;
            }
        }
    }
}
