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
        [Min(1)] public int noiseOctaves = 4;
        [Range(0f, 1f)] public float noisePersistence = 0.5f;
        [Min(1f)] public float noiseLacunarity = 2f;
    }

    public sealed class ProceduralWorldGenerator
    {
        private readonly int seed;
        private readonly WorldGenerationSettings settings;
        private readonly WorldGenerationPipeline pipeline;
        private readonly INoiseField noise;

        public ProceduralWorldGenerator(int seed, WorldGenerationSettings settings)
            : this(seed, settings, null)
        {
        }

        public ProceduralWorldGenerator(
            int seed,
            WorldGenerationSettings settings,
            WorldGenerationPipeline pipeline)
        {
            this.seed = seed;
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (settings.chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(settings.chunkSize));

            noise = new SeededPerlinNoiseField(
                seed,
                settings.noiseScale,
                settings.noiseStrength,
                settings.noiseOctaves,
                settings.noisePersistence,
                settings.noiseLacunarity);

            this.pipeline = pipeline ?? new WorldGenerationPipeline()
                .Add(new RadialBiomePass());
        }

        public GeneratedChunk GenerateChunk(ChunkCoord coordinate)
        {
            var chunk = new GeneratedChunk(coordinate, settings.chunkSize);
            var context = new WorldGenerationContext(seed, settings, coordinate, chunk, noise);
            pipeline.Execute(context);
            return chunk;
        }
    }
}
