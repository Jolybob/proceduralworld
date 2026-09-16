using System;

namespace Jolybob.ProceduralWorld
{
    public readonly struct WorldPosition : IEquatable<WorldPosition>
    {
        public readonly int X;
        public readonly int Y;

        public WorldPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(WorldPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is WorldPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(WorldPosition a, WorldPosition b) => a.Equals(b);
        public static bool operator !=(WorldPosition a, WorldPosition b) => !a.Equals(b);
        public override string ToString() => $"({X}, {Y})";
    }

    public readonly struct ChunkCoord : IEquatable<ChunkCoord>
    {
        public readonly int X;
        public readonly int Y;

        public ChunkCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(ChunkCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is ChunkCoord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(ChunkCoord a, ChunkCoord b) => a.Equals(b);
        public static bool operator !=(ChunkCoord a, ChunkCoord b) => !a.Equals(b);
        public override string ToString() => $"Chunk({X}, {Y})";
    }

    public enum WorldTile : byte
    {
        Empty = 0,
        Deep = 1,
        Mid = 2,
        Inner = 3,
        Core = 4
    }

    /// <summary>Stable identifier for a generated terrain definition.</summary>
    public readonly struct TerrainId : IEquatable<TerrainId>
    {
        public readonly byte Value;

        public TerrainId(byte value) => Value = value;

        public bool Equals(TerrainId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TerrainId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(TerrainId left, TerrainId right) => left.Equals(right);
        public static bool operator !=(TerrainId left, TerrainId right) => !left.Equals(right);
        public override string ToString() => Value.ToString();
    }

    public struct GeneratedCell
    {
        public WorldTile Tile;
        public byte Biome;
        public GeneratedCellFlags Flags;

        public GeneratedCell(WorldTile tile, byte biome)
            : this(tile, biome, GeneratedCellFlags.None)
        {
        }

        public GeneratedCell(WorldTile tile, byte biome, GeneratedCellFlags flags)
        {
            Tile = tile;
            Biome = biome;
            Flags = flags;
        }
    }

    public sealed class GeneratedChunk
    {
        public ChunkCoord Coordinate { get; }
        public int Size { get; }
        public GeneratedCell[] Cells { get; }

        public GeneratedChunk(ChunkCoord coordinate, int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            Coordinate = coordinate;
            Size = size;
            Cells = new GeneratedCell[size * size];
        }

        public GeneratedCell GetCell(int x, int y)
        {
            if ((uint)x >= Size || (uint)y >= Size)
                throw new ArgumentOutOfRangeException();
            return Cells[y * Size + x];
        }

        public void SetCell(int x, int y, GeneratedCell cell)
        {
            if ((uint)x >= Size || (uint)y >= Size)
                throw new ArgumentOutOfRangeException();
            Cells[y * Size + x] = cell;
        }
    }
}
