using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
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

    public sealed class ProceduralWorldGenerator
    {
        private readonly int seed;
        private readonly WorldGenerationSettings settings;
        private readonly WorldGenerationPipeline pipeline;
        private readonly INoiseField noise;
        private readonly IEnvironmentFieldProvider environmentFields;
        private readonly RegionCatalog regions;
        private readonly TerrainCatalog terrains;

        public ProceduralWorldGenerator(int seed, WorldGenerationSettings settings)
            : this(seed, settings, null, null, null, null)
        {
        }

        public ProceduralWorldGenerator(
            int seed,
            WorldGenerationSettings settings,
            WorldGenerationPipeline pipeline)
            : this(seed, settings, pipeline, null, null, null)
        {
        }

        public ProceduralWorldGenerator(
            int seed,
            WorldGenerationSettings settings,
            WorldGenerationPipeline pipeline,
            IEnvironmentFieldProvider environmentFields)
            : this(seed, settings, pipeline, environmentFields, null, null)
        {
        }

        public ProceduralWorldGenerator(
            int seed,
            WorldGenerationSettings settings,
            WorldGenerationPipeline pipeline,
            IEnvironmentFieldProvider environmentFields,
            RegionCatalog regions,
            TerrainCatalog terrains)
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

            this.environmentFields = environmentFields ??
                new DefaultEnvironmentFieldProvider(seed, settings, noise);
            this.regions = regions ?? RegionCatalog.CreateDefault();
            this.terrains = terrains ?? TerrainCatalog.CreateDefault();
            this.pipeline = pipeline ?? CreateDefaultPipeline(this.regions, this.terrains);
        }

        public GeneratedChunk GenerateChunk(ChunkCoord coordinate)
        {
            var chunk = new GeneratedChunk(coordinate, settings.chunkSize);
            var context = new WorldGenerationContext(
                seed,
                settings,
                coordinate,
                chunk,
                noise,
                environmentFields);
            pipeline.Execute(context);
            return chunk;
        }

        private static WorldGenerationPipeline CreateDefaultPipeline(
            RegionCatalog regions,
            TerrainCatalog terrains)
        {
            return new WorldGenerationPipeline()
                .Add(new RegionBiomePass(new ThresholdRegionResolver()))
                .Add(new TerrainPass(regions, terrains));
        }
    }
}
