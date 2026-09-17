using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Immutable world-space identity and rectangular footprint for a planned feature.
    /// The owner chunk is the deterministic authority that creates the placement.
    /// </summary>
    public readonly struct WorldFeaturePlacement : IEquatable<WorldFeaturePlacement>
    {
        public int FeatureId { get; }
        public WorldPosition Anchor { get; }
        public ChunkCoord OwnerChunk { get; }
        public int Width { get; }
        public int Height { get; }

        public int MinX => Anchor.X;
        public int MinY => Anchor.Y;
        public int MaxX => checked(Anchor.X + Width - 1);
        public int MaxY => checked(Anchor.Y + Height - 1);

        public WorldFeaturePlacement(
            int featureId,
            WorldPosition anchor,
            ChunkCoord ownerChunk,
            int width,
            int height)
        {
            if (featureId < 0)
                throw new ArgumentOutOfRangeException(nameof(featureId));
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            long maxX = (long)anchor.X + width - 1L;
            long maxY = (long)anchor.Y + height - 1L;
            if (maxX > int.MaxValue || maxX < int.MinValue)
                throw new OverflowException("Feature footprint exceeds the supported world X coordinate range.");
            if (maxY > int.MaxValue || maxY < int.MinValue)
                throw new OverflowException("Feature footprint exceeds the supported world Y coordinate range.");

            FeatureId = featureId;
            Anchor = anchor;
            OwnerChunk = ownerChunk;
            Width = width;
            Height = height;
        }

        public bool Contains(WorldPosition position)
        {
            return position.X >= MinX && position.X <= MaxX
                && position.Y >= MinY && position.Y <= MaxY;
        }

        public bool Intersects(ChunkCoord chunk, int chunkSize)
        {
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));

            long chunkMinX = (long)chunk.X * chunkSize;
            long chunkMinY = (long)chunk.Y * chunkSize;
            long chunkMaxX = chunkMinX + chunkSize - 1L;
            long chunkMaxY = chunkMinY + chunkSize - 1L;

            return (long)MinX <= chunkMaxX && (long)MaxX >= chunkMinX
                && (long)MinY <= chunkMaxY && (long)MaxY >= chunkMinY;
        }

        public bool Equals(WorldFeaturePlacement other)
        {
            return FeatureId == other.FeatureId
                && Anchor == other.Anchor
                && OwnerChunk == other.OwnerChunk
                && Width == other.Width
                && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldFeaturePlacement other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = FeatureId;
                hash = hash * 397 ^ Anchor.GetHashCode();
                hash = hash * 397 ^ OwnerChunk.GetHashCode();
                hash = hash * 397 ^ Width;
                hash = hash * 397 ^ Height;
                return hash;
            }
        }

        public static bool operator ==(WorldFeaturePlacement left, WorldFeaturePlacement right) => left.Equals(right);
        public static bool operator !=(WorldFeaturePlacement left, WorldFeaturePlacement right) => !left.Equals(right);
    }
}
