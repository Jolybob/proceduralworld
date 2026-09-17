using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Immutable world-space placement for a structure. The owner chunk is the only
    /// chunk allowed to create the placement; every intersecting chunk materializes
    /// its local portion of the footprint.
    /// </summary>
    public readonly struct StructurePlacement : IEquatable<StructurePlacement>
    {
        public StructureDefinition Definition { get; }
        public WorldPosition Anchor { get; }
        public ChunkCoord OwnerChunk { get; }

        public int MinX => Anchor.X;
        public int MinY => Anchor.Y;
        public int MaxX => checked(Anchor.X + Definition.Width - 1);
        public int MaxY => checked(Anchor.Y + Definition.Height - 1);

        public StructurePlacement(
            StructureDefinition definition,
            WorldPosition anchor,
            ChunkCoord ownerChunk)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Anchor = anchor;
            OwnerChunk = ownerChunk;

            long maxX = (long)anchor.X + definition.Width - 1L;
            long maxY = (long)anchor.Y + definition.Height - 1L;
            if (maxX > int.MaxValue || maxX < int.MinValue)
                throw new OverflowException("Structure footprint exceeds the supported world X coordinate range.");
            if (maxY > int.MaxValue || maxY < int.MinValue)
                throw new OverflowException("Structure footprint exceeds the supported world Y coordinate range.");
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

        public bool Equals(StructurePlacement other)
        {
            return Definition.Id == other.Definition.Id
                && Anchor == other.Anchor
                && OwnerChunk == other.OwnerChunk;
        }

        public override bool Equals(object obj)
        {
            return obj is StructurePlacement other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Definition.Id.GetHashCode();
                hash = hash * 397 ^ Anchor.GetHashCode();
                hash = hash * 397 ^ OwnerChunk.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(StructurePlacement left, StructurePlacement right) => left.Equals(right);
        public static bool operator !=(StructurePlacement left, StructurePlacement right) => !left.Equals(right);
    }
}
