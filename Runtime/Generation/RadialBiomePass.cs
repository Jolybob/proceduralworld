using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    public sealed class RadialBiomePass : IWorldGenerationPass
    {
        public int Order => 100;

        public void Execute(WorldGenerationContext context)
        {
            int size = context.Chunk.Size;
            var settings = context.Settings;

            for (int localY = 0; localY < size; localY++)
            {
                for (int localX = 0; localX < size; localX++)
                {
                    int worldX = context.ChunkCoordinate.X * size + localX;
                    int worldY = context.ChunkCoordinate.Y * size + localY;

                    float distance = Mathf.Sqrt(worldX * worldX + worldY * worldY);
                    float angle = Mathf.Atan2(worldY, worldX);
                    float warp = Mathf.PerlinNoise(
                        Mathf.Cos(angle) * 0.75f + SeedOffset(context.Seed, 11),
                        Mathf.Sin(angle) * 0.75f + SeedOffset(context.Seed, 23)) * 2f - 1f;
                    float radius = distance + warp * settings.borderWarp;
                    float noise = context.Noise.Sample(worldX, worldY);

                    WorldTile tile;
                    byte biome;

                    if (radius <= settings.coreRadius)
                    {
                        tile = WorldTile.Core;
                        biome = 0;
                    }
                    else if (radius <= settings.innerRadius + noise * 12f)
                    {
                        tile = WorldTile.Inner;
                        biome = 1;
                    }
                    else if (radius <= settings.midRadius + noise * 18f)
                    {
                        tile = WorldTile.Mid;
                        biome = 2;
                    }
                    else
                    {
                        tile = WorldTile.Deep;
                        biome = 3;
                    }

                    context.Chunk.SetCell(localX, localY, new GeneratedCell(tile, biome));
                }
            }
        }

        private static float SeedOffset(int seed, int salt)
        {
            unchecked
            {
                uint h = (uint)seed;
                h ^= 0x9E3779B9u + (uint)salt + (h << 6) + (h >> 2);
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                return (h & 0x00FFFFFFu) / 16777215f * 10000f;
            }
        }
    }
}
