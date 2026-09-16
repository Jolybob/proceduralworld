using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    /// <summary>Configuration shared by the default generation pipeline.</summary>
    [Serializable]
    public sealed class WorldGenerationSettings
    {
        [Header("Chunks")]
        [Min(1)] public int chunkSize = 64;

        [Header("Base Noise")]
        [Min(0.001f)] public float noiseScale = 0.025f;
        [Range(0f, 1f)] public float noiseStrength = 0.85f;
        [Min(1)] public int noiseOctaves = 4;
        [Range(0f, 1f)] public float noisePersistence = 0.5f;
        [Min(1f)] public float noiseLacunarity = 2f;

        [Header("Region Fields")]
        [Min(0.001f)] public float temperatureScale = 0.008f;
        [Min(0.001f)] public float moistureScale = 0.012f;

        [Header("Prototype World Shape")]
        [Min(1f)] public float coreRadius = 18f;
        [Min(1f)] public float innerRadius = 70f;
        [Min(1f)] public float midRadius = 140f;
        [Min(1f)] public float borderWarp = 20f;
    }

    /// <summary>
    /// Entry point for deterministic chunk generation.
    /// The generator owns orchestration only; individual world decisions live in passes.
    /// </summary>
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

            this.pipeline = pipeline ?? CreateDefaultPipeline(seed, settings);
        }

        public GeneratedChunk GenerateChunk(ChunkCoord coordinate)
        {
            var chunk = new GeneratedChunk(coordinate, settings.chunkSize);
            var context = new WorldGenerationContext(seed, settings, coordinate, chunk, noise);
            pipeline.Execute(context);
            return chunk;
        }

        private static WorldGenerationPipeline CreateDefaultPipeline(
            int seed,
            WorldGenerationSettings settings)
        {
            return new WorldGenerationPipeline()
                .Add(new RegionBiomePass(seed, settings))
                .Add(new TerrainPass());
        }
    }
}
