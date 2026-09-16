using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    public sealed class RegionBiomePass : IWorldGenerationPass
    {
        public int Order => 100;
        private readonly INoiseField temperatureField;
        private readonly INoiseField moistureField;
        private readonly IRegionResolver resolver;

        public RegionBiomePass(int seed, WorldGenerationSettings settings)
            : this(seed, settings, new ThresholdRegionResolver()) { }

        public RegionBiomePass(int seed, WorldGenerationSettings settings, IRegionResolver resolver)
        {
            if (settings == null) throw new System.ArgumentNullException(nameof(settings));
            if (resolver == null) throw new System.ArgumentNullException(nameof(resolver));
            temperatureField = new SeededPerlinNoiseField(seed + 101, settings.temperatureScale, 1f, settings.noiseOctaves, settings.noisePersistence, settings.noiseLacunarity);
            moistureField = new SeededPerlinNoiseField(seed + 202, settings.moistureScale, 1f, settings.noiseOctaves, settings.noisePersistence, settings.noiseLacunarity);
            this.resolver = resolver;
        }

        public void Execute(WorldGenerationContext context)
        {
            int size = context.Chunk.Size;
            float maxRadius = Mathf.Max(context.Settings.midRadius, 1f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int worldX = context.ChunkCoordinate.X * size + x;
                int worldY = context.ChunkCoordinate.Y * size + y;
                float distance = Mathf.Sqrt(worldX * worldX + worldY * worldY);
                float radial = Mathf.Clamp01(distance / maxRadius);
                float temperature = Mathf.Clamp01(0.5f + temperatureField.Sample(worldX, worldY) * 0.5f);
                float moisture = Mathf.Clamp01(0.5f + moistureField.Sample(worldX, worldY) * 0.5f);
                RegionId region = resolver.Resolve(new EnvironmentSample(temperature, moisture, 1f - radial, radial));
                var cell = context.Chunk.GetCell(x, y);
                cell.Biome = region.Value;
                context.Chunk.SetCell(x, y, cell);
            }
        }
    }
}
