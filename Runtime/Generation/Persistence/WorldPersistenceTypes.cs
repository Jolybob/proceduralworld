using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// A persisted override for one cell in a chunk.
    /// </summary>
    public readonly struct WorldCellModification : IEquatable<WorldCellModification>
    {
        public readonly int X;
        public readonly int Y;
        public readonly GeneratedCell Cell;

        public WorldCellModification(int x, int y, GeneratedCell cell)
        {
            X = x;
            Y = y;
            Cell = cell;
        }

        public bool Equals(WorldCellModification other)
        {
            return X == other.X && Y == other.Y && WorldPersistenceUtility.AreEqual(Cell, other.Cell);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldCellModification other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Cell.Region, Cell.Terrain, Cell.Resource, Cell.Structure, Cell.Tile, Cell.Biome, Cell.Flags);
        }
    }

    /// <summary>
    /// Serializable-independent chunk save data. Stores only generated-cell overrides.
    /// </summary>
    public sealed class WorldChunkSaveData
    {
        private readonly List<WorldCellModification> modifications;

        public ChunkCoord Coordinate { get; }
        public int FormatVersion { get; }
        public IReadOnlyList<WorldCellModification> Modifications => modifications;

        public WorldChunkSaveData(
            ChunkCoord coordinate,
            IEnumerable<WorldCellModification> modifications,
            int formatVersion = 1)
        {
            if (formatVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(formatVersion));

            Coordinate = coordinate;
            FormatVersion = formatVersion;
            this.modifications = modifications == null
                ? new List<WorldCellModification>()
                : new List<WorldCellModification>(modifications);
        }
    }

    public interface IWorldChunkStore
    {
        bool TryLoad(ChunkCoord coordinate, out WorldChunkSaveData data);
        void Save(WorldChunkSaveData data);
        bool Delete(ChunkCoord coordinate);
    }

    internal static class WorldPersistenceUtility
    {
        public static bool AreEqual(GeneratedCell left, GeneratedCell right)
        {
            return left.Region == right.Region &&
                   left.Terrain == right.Terrain &&
                   left.Resource == right.Resource &&
                   left.Structure == right.Structure &&
                   left.Tile == right.Tile &&
                   left.Biome == right.Biome &&
                   left.Flags == right.Flags;
        }
    }
}
