using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    [Serializable]
    public sealed class WorldGenerationSettings
    {
        [Min(1)] public int chunkSize = 64;
        [Min(0.001f)] public float noiseScale = 0.025f;
        [Range(0f, 1f)] public float noiseStrength = 0.85f;
        [Min(1f)] public float coreRadius = 18f;
        [Min(1f)] public float innerRadius = 70f;
        [Min(1f)] public float midRadius = 140f;
        [Min(1f)] public float borderWarp = 20f;
    }

    public sealed class ProceduralWorldGenerator
    {
        private readonly int seed;
        private readonly WorldGenerationSettings settings;

        public ProceduralWorldGenerator(int seed, WorldGenerationSettings settings)
        {
            this.seed = seed;
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (settings.chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(settings.chunkSize));
        }

        public GeneratedChunk GenerateChunk(ChunkCoord coordinate)
        {
            var chunk = new GeneratedChunk(coordinate, settings.chunkSize);
            int size = settings.chunkSize;

            for (int localY = 0; localY < size; localY++)
            {
                for (int localX = 0; localX < size; localX++)
                {
                    int worldX = coordinate.X * size + localX;
                    int worldY = coordinate.Y * size + localY;

                    float distance = Mathf.Sqrt(worldX * worldX + worldY * worldY);
                    float angle = Mathf.Atan2(worldY, worldX);
                    float warp = Mathf.PerlinNoise(
                        Mathf.Cos(angle) * 0.75f + SeedOffset(11),
                        Mathf.Sin(angle) * 0.75f + SeedOffset(23)) * 2f - 1f;

                    float radius = distance + warp * settings.borderWarp;
                    float noise = FractalNoise(worldX, worldY);

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

                    chunk.SetCell(localX, localY, new GeneratedCell(tile, biome));
                }
            }

            return chunk;
        }

        private float FractalNoise(int x, int y)
        {
            float amplitude = 1f;
            float frequency = settings.noiseScale;
            float value = 0f;
            float normalization = 0f;

            for (int octave = 0; octave < 4; octave++)
            {
                value += Mathf.PerlinNoise(
                    x * frequency + SeedOffset(100 + octave * 17),
                    y * frequency + SeedOffset(200 + octave * 29)) * amplitude;
                normalization += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return ((value / normalization) - 0.5f) * 2f * settings.noiseStrength;
        }

        private float SeedOffset(int salt)
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
