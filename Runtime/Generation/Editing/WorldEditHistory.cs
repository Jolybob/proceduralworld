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
        private int journalCursor;

        public IWorldChunkAccess Access => access;
        public IWorldChangeJournal Journal => journal;
        public IReadOnlyList<WorldEditHistoryEntry> UndoEntries => undoStack;
        public IReadOnlyList<WorldEditHistoryEntry> RedoEntries => redoStack;
        public bool CanUndo => undoStack.Count > 0 || journal.Changes.Count > journalCursor;
        public bool CanRedo => redoStack.Count > 0 && journal.Changes.Count == journalCursor;

        public WorldEditHistory(IWorldChunkAccess access, IWorldChangeJournal journal)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
            journalCursor = journal.Changes.Count;
        }

        /// <summary>
        /// Captures changes recorded since the previous history cursor as one named entry.
        /// </summary>
        public WorldEditHistoryEntry Commit(string name = null)
        {
            ResetIfJournalWasCleared();

            int end = journal.Changes.Count;
            if (end <= journalCursor)
                return null;

            var captured = new List<WorldCellChange>(end - journalCursor);
            for (int i = journalCursor; i < end; i++)
                captured.Add(journal.Changes[i]);

            var entry = new WorldEditHistoryEntry(name, captured);
            undoStack.Add(entry);
            redoStack.Clear();
            journalCursor = end;
            return entry;
        }

        public bool Undo()
        {
            ResetIfJournalWasCleared();
            CommitPendingChanges();

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
            ResetIfJournalWasCleared();
            CommitPendingChanges();

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
            journalCursor = journal.Changes.Count;
        }

        private void CommitPendingChanges()
        {
            if (journal.Changes.Count > journalCursor)
                Commit();
        }

        private void ResetIfJournalWasCleared()
        {
            if (journal.Changes.Count >= journalCursor)
                return;

            undoStack.Clear();
            redoStack.Clear();
            journalCursor = journal.Changes.Count;
        }
    }
}
