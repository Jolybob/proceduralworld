using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// High-level mutation boundary for player or gameplay edits to loaded world cells.
    /// Successful mutations can optionally be recorded in a change journal.
    /// </summary>
    public sealed class WorldEditService
    {
        private readonly IWorldChunkAccess access;
        private readonly int chunkSize;
        private readonly IWorldChangeJournal journal;

        public IWorldChunkAccess Access => access;
        public int ChunkSize => chunkSize;
        public IWorldChangeJournal Journal => journal;

        public WorldEditService(IWorldChunkAccess access, int chunkSize)
            : this(access, chunkSize, null)
        {
        }

        public WorldEditService(
            IWorldChunkAccess access,
            int chunkSize,
            IWorldChangeJournal journal)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            this.chunkSize = chunkSize;
            this.journal = journal;
        }

        public bool TryGetCell(WorldPosition position, out GeneratedCell cell)
        {
            return access.TryGetCell(position, out cell);
        }

        public bool TrySetCell(WorldPosition position, GeneratedCell cell)
        {
            if (!access.TryGetCell(position, out GeneratedCell before))
                return false;

            if (WorldPersistenceUtility.AreEqual(before, cell))
                return true;

            if (!access.SetCell(position, cell))
                return false;

            Record(position, before, cell, WorldEditOperationKind.SetCell);
            return true;
        }

        public bool TrySetTile(WorldPosition position, WorldTile tile)
        {
            if (!access.TryGetCell(position, out GeneratedCell before))
                return false;

            GeneratedCell after = before;
            after.SetTerrain(after.Terrain, tile);
            return TryCommit(position, before, after, WorldEditOperationKind.SetTile);
        }

        public bool TrySetResource(WorldPosition position, ResourceId resource)
        {
            if (!access.TryGetCell(position, out GeneratedCell before))
                return false;

            GeneratedCell after = before;
            after.SetResource(resource);
            return TryCommit(position, before, after, WorldEditOperationKind.SetResource);
        }

        public bool TryClearResource(WorldPosition position)
        {
            if (!access.TryGetCell(position, out GeneratedCell before))
                return false;

            GeneratedCell after = before;
            after.ClearResource();
            return TryCommit(position, before, after, WorldEditOperationKind.ClearResource);
        }

        public bool TrySetStructure(WorldPosition position, StructureId structure)
        {
            if (!access.TryGetCell(position, out GeneratedCell before))
                return false;

            GeneratedCell after = before;
            after.SetStructure(structure);
            return TryCommit(position, before, after, WorldEditOperationKind.SetStructure);
        }

        public bool TryClearStructure(WorldPosition position)
        {
            if (!access.TryGetCell(position, out GeneratedCell before))
                return false;

            GeneratedCell after = before;
            after.ClearStructure();
            return TryCommit(position, before, after, WorldEditOperationKind.ClearStructure);
        }

        private bool TryCommit(
            WorldPosition position,
            GeneratedCell before,
            GeneratedCell after,
            WorldEditOperationKind operation)
        {
            if (WorldPersistenceUtility.AreEqual(before, after))
                return true;

            if (!access.SetCell(position, after))
                return false;

            Record(position, before, after, operation);
            return true;
        }

        private void Record(
            WorldPosition position,
            GeneratedCell before,
            GeneratedCell after,
            WorldEditOperationKind operation)
        {
            if (journal != null)
                journal.Record(new WorldCellChange(position, before, after, operation));
        }
    }
}
