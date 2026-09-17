using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Buffers world-change notifications and flushes one deterministic, coalesced presentation batch.
    /// This keeps mutation notification separate from presentation scheduling and avoids duplicate redraws.
    /// </summary>
    public sealed class WorldPresentationFlushCoordinator : IDisposable, IWorldChangeListener, IWorldChangeBatchListener
    {
        private readonly WorldPresentationDirtySet dirtySet;
        private readonly IWorldChangeRenderer renderer;
        private IDisposable changeSubscription;
        private IDisposable batchSubscription;

        public bool IsDisposed { get; private set; }
        public int DirtyCount => dirtySet.Count;

        public WorldPresentationFlushCoordinator(
            WorldChangeObserverJournal journal,
            IWorldChangeRenderer renderer)
            : this(journal, renderer, new WorldPresentationDirtySet())
        {
        }

        public WorldPresentationFlushCoordinator(
            WorldChangeObserverJournal journal,
            IWorldChangeRenderer renderer,
            WorldPresentationDirtySet dirtySet)
        {
            if (journal == null)
                throw new ArgumentNullException(nameof(journal));

            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            this.dirtySet = dirtySet ?? throw new ArgumentNullException(nameof(dirtySet));

            changeSubscription = journal.Subscribe(this);
            batchSubscription = journal.SubscribeBatch(this);
        }

        public void OnWorldChanged(WorldCellChange change)
        {
            if (IsDisposed)
                return;

            dirtySet.Mark(change);
        }

        public void OnWorldChanged(WorldChangeBatch batch)
        {
            if (IsDisposed)
                return;

            dirtySet.MarkBatch(batch);
        }

        /// <summary>
        /// Flushes all pending changes as one coalesced batch. Empty flushes are no-ops.
        /// </summary>
        public void Flush()
        {
            if (IsDisposed || dirtySet.Count == 0)
                return;

            renderer.RenderBatch(dirtySet.Drain());
        }

        public void Clear()
        {
            dirtySet.Clear();
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            changeSubscription.Dispose();
            batchSubscription.Dispose();
            changeSubscription = null;
            batchSubscription = null;
            dirtySet.Clear();
        }
    }
}
