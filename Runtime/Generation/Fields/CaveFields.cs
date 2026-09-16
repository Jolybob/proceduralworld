using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Supplies a deterministic scalar field used to decide where caves may be carved.
    /// </summary>
    public interface ICaveFieldProvider
    {
        float Sample(int worldX, int worldY);
    }

    /// <summary>
    /// Default cave field based on an independent seeded multi-octave noise field.
    /// </summary>
    public sealed class DefaultCaveFieldProvider : ICaveFieldProvider
    {
        private readonly INoiseField field;

        public DefaultCaveFieldProvider(int seed, WorldGenerationSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            field = new SeededPerlinNoiseField(
                seed + settings.caveSeedOffset,
                settings.caveScale,
                1f,
                settings.noiseOctaves,
                settings.noisePersistence,
                settings.noiseLacunarity);
        }

        public float Sample(int worldX, int worldY)
        {
            return Mathf.Clamp01(0.5f + field.Sample(worldX, worldY) * 0.5f);
        }
    }
}
