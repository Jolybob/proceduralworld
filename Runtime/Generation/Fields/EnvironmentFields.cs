using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Produces environmental values used by region selection and other generation systems.
    /// Field generation is intentionally independent from biome/region decisions.
    /// </summary>
    public interface IEnvironmentFieldProvider
    {
        EnvironmentSample Sample(int worldX, int worldY);
    }

    /// <summary>
    /// Default deterministic field provider built from seeded noise plus world-space distance.
    /// </summary>
    public sealed class DefaultEnvironmentFieldProvider : IEnvironmentFieldProvider
    {
        private readonly INoiseField elevationField;
        private readonly INoiseField temperatureField;
        private readonly INoiseField moistureField;
        private readonly float distanceNormalization;

        public DefaultEnvironmentFieldProvider(
            int seed,
            WorldGenerationSettings settings,
            INoiseField elevationField)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (elevationField == null)
                throw new ArgumentNullException(nameof(elevationField));

            this.elevationField = elevationField;
            temperatureField = new SeededPerlinNoiseField(
                seed + 101,
                settings.temperatureScale,
                1f,
                settings.noiseOctaves,
                settings.noisePersistence,
                settings.noiseLacunarity);
            moistureField = new SeededPerlinNoiseField(
                seed + 202,
                settings.moistureScale,
                1f,
                settings.noiseOctaves,
                settings.noisePersistence,
                settings.noiseLacunarity);
            distanceNormalization = Mathf.Max(settings.midRadius, 1f);
        }

        public EnvironmentSample Sample(int worldX, int worldY)
        {
            float distance = Mathf.Sqrt((float)worldX * worldX + (float)worldY * worldY);
            float radial = Mathf.Clamp01(distance / distanceNormalization);
            float elevation = Mathf.Clamp01(0.5f + elevationField.Sample(worldX, worldY) * 0.5f);
            float temperature = Mathf.Clamp01(0.5f + temperatureField.Sample(worldX, worldY) * 0.5f);
            float moisture = Mathf.Clamp01(0.5f + moistureField.Sample(worldX, worldY) * 0.5f);

            return new EnvironmentSample(temperature, moisture, elevation, radial);
        }
    }
}
