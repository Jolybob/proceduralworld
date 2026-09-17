using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// A named group of world changes that can be applied or reverted as one history entry.
    /// </summary>
    public sealed class WorldEditHistoryEntry
    {
        private readonly List<WorldCellChange> changes;

        public string Name { get; }
        public IReadOnlyList<WorldCellChange> Changes => changes;

        public WorldEditHistoryEntry(string name, IEnumerable<WorldCellChange> changes)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));

            Name = string.IsNullOrEmpty(name) ? "World Edit" : name;
            this.changes = new List<WorldCellChange>(changes);
        }
    }

    /// <summary>
    /// Provides grouped undo/redo over changes recorded by an <see cref="IWorldChangeJournal"/>.
    /// </summary>
    public sealed class WorldEditHistory
    {
        private readonly IWorldChunkAccess access;
        private readonly IWorldChangeJournal journal;
        private readonly List<WorldEditHistoryEntry> undoStack = new List<WorldEditHistoryEntry>();
        private readonly List<WorldEditHistoryEntry> redoStack = new List<WorldEditHistoryEntry>();
        private int observedChangeCount;

        public IWorldChunkAccess Access => access;
        public IWorldChangeJournal Journal => journal;
        public IReadOnlyList<WorldEditHistoryEntry> UndoEntries => undoStack;
        public IReadOnlyList<WorldEditHistoryEntry> RedoEntries => redoStack;
        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        public WorldEditHistory(IWorldChunkAccess access, IWorldChangeJournal journal)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
            observedChangeCount = journal.Changes.Count;
        }

        /// <summary>
        /// Captures journal changes created since the last synchronization as one history entry.
        /// </summary>
        public WorldEditHistoryEntry Commit(string name = null)
        {
            SyncJournal();
            if (observedChangeCount == 0 || observedChangeCount <= undoChangeTotal)
                return null;

            int start = GetUndoChangeTotal();
            int end = journal.Changes.Count;
            var captured = new List<WorldCellChange>(Math.Max(0, end - start));
            for (int i = start; i < end; i++)
                captured.Add(journal.Changes[i]);

            if (captured.Count == 0)
                return null;

            var entry = new WorldEditHistoryEntry(name, captured);
            undoStack.Add(entry);
            redoStack.Clear();
            return entry;
        }

        public bool Undo()
        {
            SyncJournal();
            if (undoStack.Count == 0)
                return false;

            WorldEditHistoryEntry entry = undoStack[undoStack.Count - 1];
            for (int i = entry.Changes.Count - 1; i >= 0; i--)
            {
                WorldCellChange change = entry.Changes[i];
                if (!access.SetCell(change.Position, change.Before))
                    return false;
            }

            undoStack.RemoveAt(undoStack.Count - 1);
            redoStack.Add(entry);
            return true;
        }

        public bool Redo()
        {
            SyncJournal();
            if (redoStack.Count == 0)
                return false;

            WorldEditHistoryEntry entry = redoStack[redoStack.Count - 1];
            for (int i = 0; i < entry.Changes.Count; i++)
            {
                WorldCellChange change = entry.Changes[i];
                if (!access.SetCell(change.Position, change.After))
                    return false;
            }

            redoStack.RemoveAt(redoStack.Count - 1);
            undoStack.Add(entry);
            return true;
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            observedChangeCount = journal.Changes.Count;
        }

        private int undoChangeTotal
        {
            get
            {
                int total = 0;
                for (int i = 0; i < undoStack.Count; i++)
                    total += undoStack[i].Changes.Count;
                return total;
            }
        }

        private int GetUndoChangeTotal()
        {
            int total = 0;
            for (int i = 0; i < undoStack.Count; i++)
                total += undoStack[i].Changes.Count;
            return total;
        }

        private void SyncJournal()
        {
            if (journal.Changes.Count < observedChangeCount)
            {
                undoStack.Clear();
                redoStack.Clear();
            }

            observedChangeCount = journal.Changes.Count;
        }
    }
}
