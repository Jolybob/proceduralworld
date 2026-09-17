using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Immutable snapshot of related cell changes published as one logical world operation.
    /// </summary>
    public sealed class WorldChangeBatch
    {
        private readonly IReadOnlyList<WorldCellChange> changes;

        public IReadOnlyList<WorldCellChange> Changes => changes;
        public int Count => changes.Count;

        public WorldChangeBatch(IReadOnlyList<WorldCellChange> changes)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));

            var copy = new List<WorldCellChange>(changes.Count);
            for (int i = 0; i < changes.Count; i++)
                copy.Add(changes[i]);

            this.changes = copy.AsReadOnly();
        }
    }

    /// <summary>
    /// Receives a group of successful world changes as one logical operation.
    /// </summary>
    public interface IWorldChangeBatchListener
    {
        void OnWorldChanged(WorldChangeBatch batch);
    }

    /// <summary>
    /// Optional journal capability for publishing several recorded changes as one notification batch.
    /// </summary>
    public interface IWorldChangeBatchJournal : IWorldChangeJournal
    {
        void RecordBatch(IReadOnlyList<WorldCellChange> changes);
    }
}
