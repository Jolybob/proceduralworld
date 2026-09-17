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
        private readonly WorldFeaturePlacement featurePlacement;

        public StructureDefinition Definition { get; }
        public WorldPosition Anchor => featurePlacement.Anchor;
        public ChunkCoord OwnerChunk => featurePlacement.OwnerChunk;

        public int MinX => featurePlacement.MinX;
        public int MinY => featurePlacement.MinY;
        public int MaxX => featurePlacement.MaxX;
        public int MaxY => featurePlacement.MaxY;

        public StructurePlacement(
            StructureDefinition definition,
            WorldPosition anchor,
            ChunkCoord ownerChunk)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            featurePlacement = new WorldFeaturePlacement(
                definition.Id.Value,
                anchor,
                ownerChunk,
                definition.Width,
                definition.Height);
        }

        internal StructurePlacement(
            StructureDefinition definition,
            WorldFeaturePlacement featurePlacement)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (featurePlacement.FeatureId != definition.Id.Value)
                throw new ArgumentException("Feature placement ID does not match the structure definition.", nameof(featurePlacement));
            if (featurePlacement.Width != definition.Width || featurePlacement.Height != definition.Height)
                throw new ArgumentException("Feature placement footprint does not match the structure definition.", nameof(featurePlacement));

            this.featurePlacement = featurePlacement;
        }

        public bool Contains(WorldPosition position)
        {
            return featurePlacement.Contains(position);
        }

        public bool Intersects(ChunkCoord chunk, int chunkSize)
        {
            return featurePlacement.Intersects(chunk, chunkSize);
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
