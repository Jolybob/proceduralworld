using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Resolves macro regions from world-space radius and angle, with optional deterministic
    /// radial and angular boundary distortion. Region definitions are evaluated by priority,
    /// then by angular/radial fit score so overlapping definitions remain deterministic.
    /// </summary>
    public sealed class RadialSectorRegionResolver : IPositionAwareRegionResolver
    {
        private const float TwoPi = 6.28318530717958647692f;

        private readonly MacroRegionCatalog catalog;
        private readonly INoiseField customBoundaryField;
        private readonly Dictionary<RegionId, INoiseField> boundaryFields;
        private readonly float seedRotation;
        private readonly RegionId fallbackRegion;

        public RadialSectorRegionResolver(
            MacroRegionCatalog catalog,
            int seed,
            RegionId fallbackRegion,
            INoiseField boundaryField = null,
            float seedRotationRadians = 0f)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.fallbackRegion = fallbackRegion;
            this.customBoundaryField = boundaryField;
            boundaryFields = new Dictionary<RegionId, INoiseField>();

            if (boundaryField == null)
            {
                for (int i = 0; i < catalog.Definitions.Count; i++)
                {
                    MacroRegionDefinition definition = catalog.Definitions[i];
                    boundaryFields[definition.Id] = new SeededPerlinNoiseField(
                        seed + definition.Id.Value * 7919,
                        definition.BoundaryNoiseScale,
                        1f,
                        3,
                        0.5f,
                        2f);
                }
            }

            seedRotation = NormalizeAngle(seedRotationRadians + SeedRotation(seed));
        }

        public RegionId Resolve(EnvironmentSample sample)
        {
            // IRegionResolver is intentionally preserved for compatibility. A caller without
            // world position cannot use radial/sector geometry, so the fallback is returned.
            return fallbackRegion;
        }

        public RegionId Resolve(WorldPosition position, EnvironmentSample sample)
        {
            float radius = Mathf.Sqrt((float)position.X * position.X + (float)position.Y * position.Y);
            float angle = NormalizeAngle(Mathf.Atan2(position.Y, position.X) + seedRotation);

            MacroRegionDefinition best = null;
            float bestScore = float.PositiveInfinity;

            for (int i = 0; i < catalog.Definitions.Count; i++)
            {
                MacroRegionDefinition definition = catalog.Definitions[i];
                float effectiveRadius = GetEffectiveRadius(position, radius, definition);

                if (effectiveRadius < definition.MinRadius || effectiveRadius > definition.MaxRadius)
                    continue;

                if (!ContainsAngle(angle, position, definition))
                    continue;

                float radialCenter = (definition.MinRadius + definition.MaxRadius) * 0.5f;
                float radialSpan = Mathf.Max(definition.MaxRadius - definition.MinRadius, 1f);
                float radialScore = Mathf.Abs(effectiveRadius - radialCenter) / radialSpan;

                float angularScore = GetAngularDistance(angle, definition.CenterAngle) /
                                     Mathf.Max(definition.AngularWidth, 0.0001f);

                float score = radialScore + angularScore * 0.5f;
                if (best == null || definition.Priority > best.Priority ||
                    (definition.Priority == best.Priority && score < bestScore))
                {
                    best = definition;
                    bestScore = score;
                }
            }

            return best != null ? best.Id : fallbackRegion;
        }

        private float GetEffectiveRadius(
            WorldPosition position,
            float radius,
            MacroRegionDefinition definition)
        {
            if (definition.BoundaryWarp <= 0f)
                return radius;

            INoiseField field = GetBoundaryField(definition);
            float normalizedNoise = 0.5f + field.Sample(position.X, position.Y) * 0.5f;
            float radialOffset = (normalizedNoise * 2f - 1f) * definition.BoundaryWarp;
            return radius + radialOffset;
        }

        private bool ContainsAngle(
            float angle,
            WorldPosition position,
            MacroRegionDefinition definition)
        {
            if (definition.AngularWidth >= TwoPi - 0.0001f)
                return true;

            float warpedAngle = angle;
            if (definition.AngularWarp > 0f)
            {
                INoiseField field = GetBoundaryField(definition);
                float warpNoise = 0.5f + field.Sample(
                    position.X + 7919,
                    position.Y - 104729) * 0.5f;
                float offset = (warpNoise * 2f - 1f) * definition.AngularWarp;
                warpedAngle = NormalizeAngle(angle + offset);
            }

            float delta = Mathf.Abs(Mathf.DeltaAngle(
                RadiansToDegrees(warpedAngle),
                RadiansToDegrees(definition.CenterAngle)));
            return delta <= RadiansToDegrees(definition.AngularWidth * 0.5f);
        }

        private INoiseField GetBoundaryField(MacroRegionDefinition definition)
        {
            if (customBoundaryField != null)
                return customBoundaryField;

            return boundaryFields[definition.Id];
        }

        private static float GetAngularDistance(float a, float b)
        {
            return Mathf.Abs(Mathf.DeltaAngle(RadiansToDegrees(a), RadiansToDegrees(b))) * Mathf.Deg2Rad;
        }

        private static float RadiansToDegrees(float radians)
        {
            return radians * Mathf.Rad2Deg;
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= TwoPi;
            if (angle < 0f)
                angle += TwoPi;
            return angle;
        }

        private static float SeedRotation(int seed)
        {
            unchecked
            {
                uint h = (uint)seed;
                h ^= 0x9E3779B9u + (h << 6) + (h >> 2);
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                return (h & 0x00FFFFFFu) / 16777215f * TwoPi;
            }
        }
    }
}
