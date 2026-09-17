using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Coalesces world changes by position so presentation sinks only need to process the latest state.
    /// The first observed change preserves the original Before state; the latest change supplies After and Operation.
    /// </summary>
    public sealed class WorldPresentationDirtySet
    {
        private readonly Dictionary<WorldPosition, WorldCellChange> changesByPosition =
            new Dictionary<WorldPosition, WorldCellChange>();
        private readonly List<WorldPosition> positions = new List<WorldPosition>();

        public int Count => positions.Count;

        public void Mark(WorldCellChange change)
        {
            WorldCellChange existing;
            if (changesByPosition.TryGetValue(change.Position, out existing))
            {
                changesByPosition[change.Position] = new WorldCellChange(
                    change.Position,
                    existing.Before,
                    change.After,
                    change.Operation);
                return;
            }

            changesByPosition.Add(change.Position, change);
            positions.Add(change.Position);
        }

        public void MarkBatch(WorldChangeBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));

            for (int i = 0; i < batch.Changes.Count; i++)
                Mark(batch.Changes[i]);
        }

        public WorldChangeBatch Drain()
        {
            var changes = new List<WorldCellChange>(positions.Count);
            for (int i = 0; i < positions.Count; i++)
                changes.Add(changesByPosition[positions[i]]);

            Clear();
            return new WorldChangeBatch(changes);
        }

        public void Clear()
        {
            changesByPosition.Clear();
            positions.Clear();
        }
    }
}
