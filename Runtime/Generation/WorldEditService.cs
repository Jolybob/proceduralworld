using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// High-level mutation boundary for player or gameplay edits to loaded world cells.
    /// </summary>
    public sealed class WorldEditService
    {
        private readonly IWorldChunkAccess access;
        private readonly int chunkSize;

        public IWorldChunkAccess Access => access;
        public int ChunkSize => chunkSize;

        public WorldEditService(IWorldChunkAccess access, int chunkSize)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            this.chunkSize = chunkSize;
        }

        public bool TryGetCell(WorldPosition position, out GeneratedCell cell)
        {
            return access.TryGetCell(position, out cell);
        }

        public bool TrySetCell(WorldPosition position, GeneratedCell cell)
        {
            return access.SetCell(position, cell);
        }

        public bool TrySetTile(WorldPosition position, WorldTile tile)
        {
            if (!access.TryGetCell(position, out GeneratedCell cell))
                return false;

            cell.SetTerrain(cell.Terrain, tile);
            return access.SetCell(position, cell);
        }

        public bool TrySetResource(WorldPosition position, ResourceId resource)
        {
            if (!access.TryGetCell(position, out GeneratedCell cell))
                return false;

            cell.SetResource(resource);
            return access.SetCell(position, cell);
        }

        public bool TryClearResource(WorldPosition position)
        {
            if (!access.TryGetCell(position, out GeneratedCell cell))
                return false;

            cell.ClearResource();
            return access.SetCell(position, cell);
        }

        public bool TrySetStructure(WorldPosition position, StructureId structure)
        {
            if (!access.TryGetCell(position, out GeneratedCell cell))
                return false;

            cell.SetStructure(structure);
            return access.SetCell(position, cell);
        }

        public bool TryClearStructure(WorldPosition position)
        {
            if (!access.TryGetCell(position, out GeneratedCell cell))
                return false;

            cell.ClearStructure();
            return access.SetCell(position, cell);
        }
    }
}
