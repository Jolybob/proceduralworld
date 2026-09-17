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
            int cellHash = WorldPersistenceUtility.GetCellHashCode(Cell);
            return HashCode.Combine(X, Y, cellHash);
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
                   left.Topology == right.Topology &&
                   left.Biome == right.Biome &&
                   left.Flags == right.Flags;
        }

        public static int GetCellHashCode(GeneratedCell cell)
        {
            int first = HashCode.Combine(cell.Region, cell.Terrain, cell.Resource, cell.Structure);
            return HashCode.Combine(first, cell.Tile, cell.Topology, cell.Biome, cell.Flags);
        }
    }
}