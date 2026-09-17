using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Presentation boundary for canonical world changes.
    /// Implementations translate world-data changes into rendering or other presentation updates.
    /// </summary>
    public interface IWorldChangeRenderer
    {
        void Render(WorldCellChange change);
        void RenderBatch(WorldChangeBatch batch);
    }

    /// <summary>
    /// Subscribes a presentation renderer to both single-change and logical-batch notifications.
    /// Direct edits arrive through Render; transaction commits arrive through RenderBatch.
    /// </summary>
    public sealed class WorldChangeRenderObserver : IDisposable, IWorldChangeListener, IWorldChangeBatchListener
    {
        private readonly WorldChangeObserverJournal journal;
        private readonly IWorldChangeRenderer renderer;
        private IDisposable changeSubscription;
        private IDisposable batchSubscription;

        public bool IsDisposed { get; private set; }

        public WorldChangeRenderObserver(
            WorldChangeObserverJournal journal,
            IWorldChangeRenderer renderer)
        {
            this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            changeSubscription = journal.Subscribe(this);
            batchSubscription = journal.SubscribeBatch(this);
        }

        public void OnWorldChanged(WorldCellChange change)
        {
            if (IsDisposed)
                return;

            renderer.Render(change);
        }

        public void OnWorldChanged(WorldChangeBatch batch)
        {
            if (IsDisposed)
                return;

            renderer.RenderBatch(batch);
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
        }
    }
}
