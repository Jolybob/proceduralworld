using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Receives successful world-cell changes after they have been recorded by a journal.
    /// </summary>
    public interface IWorldChangeListener
    {
        void OnWorldChanged(WorldCellChange change);
    }

    /// <summary>
    /// Adds deterministic change notifications on top of an existing journal.
    /// The wrapped journal remains the source of truth for history.
    /// </summary>
    public sealed class WorldChangeObserverJournal : IWorldChangeBatchJournal
    {
        private readonly IWorldChangeJournal inner;
        private readonly List<IWorldChangeListener> listeners = new List<IWorldChangeListener>();
        private readonly List<IWorldChangeBatchListener> batchListeners = new List<IWorldChangeBatchListener>();

        public IWorldChangeJournal Inner => inner;
        public IReadOnlyList<WorldCellChange> Changes => inner.Changes;
        public int ListenerCount => listeners.Count;
        public int BatchListenerCount => batchListeners.Count;

        public WorldChangeObserverJournal(IWorldChangeJournal inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public void Record(WorldCellChange change)
        {
            inner.Record(change);
            NotifyChange(change);
        }

        public void RecordBatch(IReadOnlyList<WorldCellChange> changes)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));
            if (changes.Count == 0)
                return;

            for (int i = 0; i < changes.Count; i++)
                inner.Record(changes[i]);

            if (batchListeners.Count == 0)
                return;

            var batch = new WorldChangeBatch(changes);
            IWorldChangeBatchListener[] snapshot = batchListeners.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i].OnWorldChanged(batch);
        }

        public void Clear()
        {
            inner.Clear();
        }

        public IDisposable Subscribe(IWorldChangeListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            listeners.Add(listener);
            return new ChangeSubscription(this, listener);
        }

        public IDisposable SubscribeBatch(IWorldChangeBatchListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            batchListeners.Add(listener);
            return new BatchSubscription(this, listener);
        }

        private void NotifyChange(WorldCellChange change)
        {
            if (listeners.Count == 0)
                return;

            IWorldChangeListener[] snapshot = listeners.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i].OnWorldChanged(change);
        }

        private void Unsubscribe(IWorldChangeListener listener)
        {
            listeners.Remove(listener);
        }

        private void UnsubscribeBatch(IWorldChangeBatchListener listener)
        {
            batchListeners.Remove(listener);
        }

        private sealed class ChangeSubscription : IDisposable
        {
            private WorldChangeObserverJournal owner;
            private IWorldChangeListener listener;

            public ChangeSubscription(WorldChangeObserverJournal owner, IWorldChangeListener listener)
            {
                this.owner = owner;
                this.listener = listener;
            }

            public void Dispose()
            {
                if (owner == null)
                    return;

                owner.Unsubscribe(listener);
                owner = null;
                listener = null;
            }
        }

        private sealed class BatchSubscription : IDisposable
        {
            private WorldChangeObserverJournal owner;
            private IWorldChangeBatchListener listener;

            public BatchSubscription(WorldChangeObserverJournal owner, IWorldChangeBatchListener listener)
            {
                this.owner = owner;
                this.listener = listener;
            }

            public void Dispose()
            {
                if (owner == null)
                    return;

                owner.UnsubscribeBatch(listener);
                owner = null;
                listener = null;
            }
        }
    }
}
