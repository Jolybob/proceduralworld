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
    public sealed class WorldChangeObserverJournal : IWorldChangeJournal
    {
        private readonly IWorldChangeJournal inner;
        private readonly List<IWorldChangeListener> listeners = new List<IWorldChangeListener>();

        public IWorldChangeJournal Inner => inner;
        public IReadOnlyList<WorldCellChange> Changes => inner.Changes;
        public int ListenerCount => listeners.Count;

        public WorldChangeObserverJournal(IWorldChangeJournal inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public void Record(WorldCellChange change)
        {
            inner.Record(change);

            if (listeners.Count == 0)
                return;

            IWorldChangeListener[] snapshot = listeners.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i].OnWorldChanged(change);
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
            return new Subscription(this, listener);
        }

        private void Unsubscribe(IWorldChangeListener listener)
        {
            listeners.Remove(listener);
        }

        private sealed class Subscription : IDisposable
        {
            private WorldChangeObserverJournal owner;
            private IWorldChangeListener listener;

            public Subscription(WorldChangeObserverJournal owner, IWorldChangeListener listener)
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
    }
}
