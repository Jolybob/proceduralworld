using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// A configurable macro-region expressed as a radial band and angular sector.
    /// Angles are measured in radians and normalized to [0, 2*PI).
    /// </summary>
    public sealed class MacroRegionDefinition
    {
        public RegionId Id { get; }
        public float MinRadius { get; }
        public float MaxRadius { get; }
        public float CenterAngle { get; }
        public float AngularWidth { get; }
        public float BoundaryWarp { get; }
        public float BoundaryNoiseScale { get; }
        public float AngularWarp { get; }
        public int Priority { get; }

        public MacroRegionDefinition(
            RegionId id,
            float minRadius,
            float maxRadius,
            float centerAngle,
            float angularWidth,
            float boundaryWarp = 0f,
            float boundaryNoiseScale = 0.01f,
            float angularWarp = 0f,
            int priority = 0)
        {
            if (maxRadius < minRadius)
                throw new ArgumentException("Max radius cannot be less than min radius.", nameof(maxRadius));
            if (minRadius < 0f)
                throw new ArgumentOutOfRangeException(nameof(minRadius));
            if (angularWidth < 0f)
                throw new ArgumentOutOfRangeException(nameof(angularWidth));
            if (boundaryWarp < 0f)
                throw new ArgumentOutOfRangeException(nameof(boundaryWarp));
            if (boundaryNoiseScale <= 0f)
                throw new ArgumentOutOfRangeException(nameof(boundaryNoiseScale));
            if (angularWarp < 0f)
                throw new ArgumentOutOfRangeException(nameof(angularWarp));

            Id = id;
            MinRadius = minRadius;
            MaxRadius = maxRadius;
            CenterAngle = NormalizeAngle(centerAngle);
            AngularWidth = Math.Min(angularWidth, TwoPi);
            BoundaryWarp = boundaryWarp;
            BoundaryNoiseScale = boundaryNoiseScale;
            AngularWarp = Math.Min(angularWarp, TwoPi * 0.5f);
            Priority = priority;
        }

        public static MacroRegionDefinition FullRing(
            RegionId id,
            float minRadius,
            float maxRadius,
            float boundaryWarp = 0f,
            float boundaryNoiseScale = 0.01f,
            int priority = 0)
        {
            return new MacroRegionDefinition(
                id,
                minRadius,
                maxRadius,
                0f,
                TwoPi,
                boundaryWarp,
                boundaryNoiseScale,
                0f,
                priority);
        }

        private const float TwoPi = 6.28318530717958647692f;

        internal static float NormalizeAngle(float angle)
        {
            angle %= TwoPi;
            if (angle < 0f)
                angle += TwoPi;
            return angle;
        }
    }

    /// <summary>
    /// Position-aware region resolver contract. Existing IRegionResolver implementations remain valid.
    /// </summary>
    public interface IPositionAwareRegionResolver : IRegionResolver
    {
        RegionId Resolve(WorldPosition position, EnvironmentSample sample);
    }
}
