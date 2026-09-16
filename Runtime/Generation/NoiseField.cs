using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    public interface INoiseField
    {
        float Sample(float x, float y);
    }

    public sealed class SeededPerlinNoiseField : INoiseField
    {
        private readonly int seed;
        private readonly float scale;
        private readonly float strength;
        private readonly int octaves;
        private readonly float persistence;
        private readonly float lacunarity;

        public SeededPerlinNoiseField(
            int seed,
            float scale,
            float strength = 1f,
            int octaves = 4,
            float persistence = 0.5f,
            float lacunarity = 2f)
        {
            this.seed = seed;
            this.scale = Mathf.Max(0.0001f, scale);
            this.strength = strength;
            this.octaves = Mathf.Max(1, octaves);
            this.persistence = Mathf.Clamp01(persistence);
            this.lacunarity = Mathf.Max(1f, lacunarity);
        }

        public float Sample(float x, float y)
        {
            float amplitude = 1f;
            float frequency = scale;
            float value = 0f;
            float normalization = 0f;

            for (int octave = 0; octave < octaves; octave++)
            {
                value += Mathf.PerlinNoise(
                    x * frequency + SeedOffset(100 + octave * 17),
                    y * frequency + SeedOffset(200 + octave * 29)) * amplitude;
                normalization += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return ((value / normalization) - 0.5f) * 2f * strength;
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
