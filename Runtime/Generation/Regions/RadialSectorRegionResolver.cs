using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Resolves macro regions from world-space polar coordinates.
    /// Boundaries can be deterministically warped so large regions remain organic instead of perfectly radial.
    /// </summary>
    public sealed class RadialSectorRegionResolver : IPositionAwareRegionResolver
    {
        private readonly MacroRegionCatalog catalog;
        private readonly int seed;
        private readonly RegionId fallbackRegion;
        private readonly float seedRotationRadians;
        private readonly bool rotateBySeed;

        public RadialSectorRegionResolver(
            MacroRegionCatalog catalog,
            int seed,
            RegionId fallbackRegion,
            bool rotateBySeed = true,
            float seedRotationRadians = 0f)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.seed = seed;
            this.fallbackRegion = fallbackRegion;
            this.rotateBySeed = rotateBySeed;
            this.seedRotationRadians = seedRotationRadians;
        }

        public RegionId Resolve(EnvironmentSample sample)
        {
            return fallbackRegion;
        }

        public RegionId Resolve(EnvironmentSample sample, int worldX, int worldY)
        {
            float x = worldX;
            float y = worldY;
            float radius = MathF.Sqrt(x * x + y * y);
            float angle = MathF.Atan2(y, x);

            if (rotateBySeed)
                angle = NormalizeAngle(angle + seedRotationRadians + SeedRotation(seed));

            MacroRegionDefinition best = null;
            float bestScore = float.NegativeInfinity;

            IReadOnlyList<MacroRegionDefinition> definitions = catalog.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                MacroRegionDefinition definition = definitions[i];
                float normalizedRadius = radius;
                if (definition.BoundaryWarp > 0f)
                {
                    float noise = SampleBoundaryNoise(
                        x * definition.BoundaryNoiseScale,
                        y * definition.BoundaryNoiseScale,
                        seed,
                        i);
                    normalizedRadius += (noise - 0.5f) * 2f * definition.BoundaryWarp;
                }

                if (normalizedRadius < definition.MinRadius || normalizedRadius > definition.MaxRadius)
                    continue;

                float angularOffset = ShortestAngle(angle, definition.CenterAngle);
                if (definition.AngularWarp > 0f)
                {
                    float angularNoise = SampleBoundaryNoise(
                        x * definition.BoundaryNoiseScale * 0.73f + 17.31f,
                        y * definition.BoundaryNoiseScale * 0.73f - 11.27f,
                        seed,
                        i + 97);
                    angularOffset += (angularNoise - 0.5f) * 2f * definition.AngularWarp;
                }

                float halfWidth = definition.AngularWidth * 0.5f;
                float angularDistance = MathF.Abs(WrapSigned(angularOffset));
                if (angularDistance > halfWidth)
                    continue;

                float radialSpan = definition.MaxRadius - definition.MinRadius;
                float radialMargin = MathF.Min(
                    normalizedRadius - definition.MinRadius,
                    definition.MaxRadius - normalizedRadius) / radialSpan;
                float angularMargin = (halfWidth - angularDistance) / MathF.Max(halfWidth, 0.0001f);
                float score = MathF.Min(radialMargin, angularMargin);

                if (best == null || definition.Priority > best.Priority ||
                    (definition.Priority == best.Priority && score > bestScore) ||
                    (definition.Priority == best.Priority && MathF.Abs(score - bestScore) < 0.000001f && definition.Id.Value < best.Id.Value))
                {
                    best = definition;
                    bestScore = score;
                }
            }

            return best != null ? best.Id : fallbackRegion;
        }

        private static float ShortestAngle(float from, float to)
        {
            return WrapSigned(from - to);
        }

        private static float WrapSigned(float angle)
        {
            float twoPi = MathF.PI * 2f;
            float wrapped = angle % twoPi;
            if (wrapped > MathF.PI)
                wrapped -= twoPi;
            else if (wrapped < -MathF.PI)
                wrapped += twoPi;
            return wrapped;
        }

        private static float NormalizeAngle(float angle)
        {
            float twoPi = MathF.PI * 2f;
            float normalized = angle % twoPi;
            return normalized < 0f ? normalized + twoPi : normalized;
        }

        private static float SeedRotation(int seed)
        {
            uint hash = Mix((uint)seed ^ 0xA511E9B3u);
            return (hash / 4294967295f) * MathF.PI * 2f;
        }

        private static float SampleBoundaryNoise(float x, float y, int seed, int salt)
        {
            int x0 = FloorToInt(x);
            int y0 = FloorToInt(y);
            float tx = x - x0;
            float ty = y - y0;

            float a = Hash01(x0, y0, seed, salt);
            float b = Hash01(x0 + 1, y0, seed, salt);
            float c = Hash01(x0, y0 + 1, seed, salt);
            float d = Hash01(x0 + 1, y0 + 1, seed, salt);

            tx = SmoothStep(tx);
            ty = SmoothStep(ty);
            float ab = Lerp(a, b, tx);
            float cd = Lerp(c, d, tx);
            return Lerp(ab, cd, ty);
        }

        private static float Hash01(int x, int y, int seed, int salt)
        {
            unchecked
            {
                uint h = (uint)seed;
                h ^= (uint)x * 0x9E3779B9u;
                h ^= (uint)y * 0x85EBCA6Bu;
                h ^= (uint)salt * 0xC2B2AE35u;
                h = Mix(h);
                return (h & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }

        private static int FloorToInt(float value)
        {
            int integer = (int)value;
            return value < integer ? integer - 1 : integer;
        }

        private static float SmoothStep(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
