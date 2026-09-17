using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Executes multiple world edits as one isolated transaction.
    /// Changes remain local until Commit and can be discarded with Rollback.
    /// </summary>
    public sealed class WorldEditTransaction
    {
        private readonly IWorldChunkAccess access;
        private readonly WorldEditService edits;
        private readonly InMemoryWorldChangeJournal localJournal;
        private readonly IWorldChangeJournal targetJournal;
        private bool completed;

        public IWorldChunkAccess Access => access;
        public WorldEditService Edits => edits;
        public IReadOnlyList<WorldCellChange> Changes => localJournal.Changes;
        public bool IsCompleted => completed;

        public WorldEditTransaction(
            IWorldChunkAccess access,
            int chunkSize,
            IWorldChangeJournal targetJournal)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            targetJournal = targetJournal ?? throw new ArgumentNullException(nameof(targetJournal));

            localJournal = new InMemoryWorldChangeJournal();
            edits = new WorldEditService(access, chunkSize, localJournal);
            this.targetJournal = targetJournal;
        }

        public bool TryGetCell(WorldPosition position, out GeneratedCell cell)
        {
            EnsureOpen();
            return edits.TryGetCell(position, out cell);
        }

        public bool TrySetCell(WorldPosition position, GeneratedCell cell)
        {
            EnsureOpen();
            return edits.TrySetCell(position, cell);
        }

        public bool TrySetTile(WorldPosition position, WorldTile tile)
        {
            EnsureOpen();
            return edits.TrySetTile(position, tile);
        }

        public bool TrySetResource(WorldPosition position, ResourceId resource)
        {
            EnsureOpen();
            return edits.TrySetResource(position, resource);
        }

        public bool TryClearResource(WorldPosition position)
        {
            EnsureOpen();
            return edits.TryClearResource(position);
        }

        public bool TrySetStructure(WorldPosition position, StructureId structure)
        {
            EnsureOpen();
            return edits.TrySetStructure(position, structure);
        }

        public bool TryClearStructure(WorldPosition position)
        {
            EnsureOpen();
            return edits.TryClearStructure(position);
        }

        /// <summary>
        /// Publishes all transaction changes to the target journal and closes the transaction.
        /// </summary>
        public bool Commit()
        {
            EnsureOpen();
            completed = true;

            for (int i = 0; i < localJournal.Changes.Count; i++)
                targetJournal.Record(localJournal.Changes[i]);

            return localJournal.Changes.Count > 0;
        }

        /// <summary>
        /// Restores every cell changed by the transaction to its original state and closes it.
        /// </summary>
        public bool Rollback()
        {
            EnsureOpen();

            for (int i = localJournal.Changes.Count - 1; i >= 0; i--)
            {
                WorldCellChange change = localJournal.Changes[i];
                if (!access.SetCell(change.Position, change.Before))
                    return false;
            }

            completed = true;
            return localJournal.Changes.Count > 0;
        }

        private void EnsureOpen()
        {
            if (completed)
                throw new InvalidOperationException("The world edit transaction has already been completed.");
        }
    }
}
