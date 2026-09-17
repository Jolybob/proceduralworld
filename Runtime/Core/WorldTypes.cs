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

    public readonly struct ResourceId : IEquatable<ResourceId>
    {
        public readonly byte Value;

        public ResourceId(byte value) => Value = value;

        public bool Equals(ResourceId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ResourceId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(ResourceId left, ResourceId right) => left.Equals(right);
        public static bool operator !=(ResourceId left, ResourceId right) => !left.Equals(right);
        public override string ToString() => Value.ToString();
    }

    public readonly struct StructureId : IEquatable<StructureId>
    {
        public readonly byte Value;

        public StructureId(byte value) => Value = value;

        public bool Equals(StructureId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is StructureId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(StructureId left, StructureId right) => left.Equals(right);
        public static bool operator !=(StructureId left, StructureId right) => !left.Equals(right);
        public override string ToString() => Value.ToString();
    }

    public struct GeneratedCell
    {
        public RegionId Region;
        public TerrainId Terrain;
        public ResourceId Resource;
        public StructureId Structure;
        public WorldTile Tile;
        public byte Biome;
        public GeneratedCellFlags Flags;
        public CellTopology Topology;

        public GeneratedCell(WorldTile tile, byte biome)
            : this(
                new RegionId(biome),
                new TerrainId((byte)tile),
                default(ResourceId),
                default(StructureId),
                tile,
                biome,
                GeneratedCellFlags.None,
                tile == WorldTile.Empty ? CellTopology.Empty : CellTopology.Solid)
        {
        }

        public GeneratedCell(
            RegionId region,
            TerrainId terrain,
            ResourceId resource,
            StructureId structure,
            WorldTile tile,
            byte biome,
            GeneratedCellFlags flags = GeneratedCellFlags.None,
            CellTopology topology = CellTopology.Solid)
        {
            Region = region;
            Terrain = terrain;
            Resource = resource;
            Structure = structure;
            Tile = tile;
            Biome = biome;
            Flags = flags;
            Topology = topology;
        }

        public void SetRegion(RegionId region)
        {
            Region = region;
            Biome = region.Value;
        }

        public void SetTerrain(TerrainId terrain, WorldTile tile)
        {
            Terrain = terrain;
            Tile = tile;
            if (Topology == CellTopology.Empty && tile != WorldTile.Empty)
                Topology = CellTopology.Solid;
        }

        public void SetTopology(CellTopology topology, WorldTile tile)
        {
            Topology = topology;
            Tile = tile;
            if (topology == CellTopology.Chasm)
                Flags |= GeneratedCellFlags.Chasm;
            else
                Flags &= ~GeneratedCellFlags.Chasm;
        }

        public void SetResource(ResourceId resource)
        {
            Resource = resource;
            Flags |= GeneratedCellFlags.HasResource;
        }

        public void ClearResource()
        {
            Resource = default(ResourceId);
            Flags &= ~GeneratedCellFlags.HasResource;
        }

        public void SetStructure(StructureId structure)
        {
            Structure = structure;
            Flags |= GeneratedCellFlags.HasStructure;
        }

        public void ClearStructure()
        {
            Structure = default(StructureId);
            Flags &= ~GeneratedCellFlags.HasStructure;
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
