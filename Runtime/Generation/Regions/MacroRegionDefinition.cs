using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Describes a large world-space region using radial and angular bounds around the world origin.
    /// Angles are expressed in radians.
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
            if (float.IsNaN(minRadius) || float.IsInfinity(minRadius) || minRadius < 0f)
                throw new ArgumentOutOfRangeException(nameof(minRadius));
            if (float.IsNaN(maxRadius) || float.IsInfinity(maxRadius) || maxRadius <= minRadius)
                throw new ArgumentOutOfRangeException(nameof(maxRadius));
            if (float.IsNaN(centerAngle) || float.IsInfinity(centerAngle))
                throw new ArgumentOutOfRangeException(nameof(centerAngle));
            if (float.IsNaN(angularWidth) || float.IsInfinity(angularWidth) || angularWidth <= 0f || angularWidth > MathF.PI * 2f)
                throw new ArgumentOutOfRangeException(nameof(angularWidth));
            if (float.IsNaN(boundaryWarp) || float.IsInfinity(boundaryWarp) || boundaryWarp < 0f)
                throw new ArgumentOutOfRangeException(nameof(boundaryWarp));
            if (float.IsNaN(boundaryNoiseScale) || float.IsInfinity(boundaryNoiseScale) || boundaryNoiseScale <= 0f)
                throw new ArgumentOutOfRangeException(nameof(boundaryNoiseScale));
            if (float.IsNaN(angularWarp) || float.IsInfinity(angularWarp) || angularWarp < 0f)
                throw new ArgumentOutOfRangeException(nameof(angularWarp));

            Id = id;
            MinRadius = minRadius;
            MaxRadius = maxRadius;
            CenterAngle = NormalizeAngle(centerAngle);
            AngularWidth = angularWidth;
            BoundaryWarp = boundaryWarp;
            BoundaryNoiseScale = boundaryNoiseScale;
            AngularWarp = angularWarp;
            Priority = priority;
        }

        private static float NormalizeAngle(float angle)
        {
            float normalized = angle % (MathF.PI * 2f);
            return normalized < 0f ? normalized + MathF.PI * 2f : normalized;
        }
    }
}
