using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Schedules presentation updates by caller-defined regions, allowing chunk-aware sinks to redraw only affected regions.
    /// </summary>
    public sealed class WorldPresentationRegionFlushCoordinator : IDisposable, IWorldChangeListener, IWorldChangeBatchListener
    {
        private readonly WorldPresentationDirtyRegionSet dirtyRegions;
        private readonly IWorldPresentationRegionRenderer renderer;
        private IDisposable changeSubscription;
        private IDisposable batchSubscription;

        public bool IsDisposed { get; private set; }
        public int DirtyRegionCount => dirtyRegions.Count;

        public WorldPresentationRegionFlushCoordinator(
            WorldChangeObserverJournal journal,
            IWorldPresentationRegionResolver resolver,
            IWorldPresentationRegionRenderer renderer)
            : this(journal, resolver, renderer, null)
        {
        }

        public WorldPresentationRegionFlushCoordinator(
            WorldChangeObserverJournal journal,
            IWorldPresentationRegionResolver resolver,
            IWorldPresentationRegionRenderer renderer,
            WorldPresentationDirtyRegionSet dirtyRegions)
        {
            if (journal == null)
                throw new ArgumentNullException(nameof(journal));

            this.dirtyRegions = dirtyRegions ?? new WorldPresentationDirtyRegionSet(resolver);
            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            changeSubscription = journal.Subscribe(this);
            batchSubscription = journal.SubscribeBatch(this);
        }

        public void OnWorldChanged(WorldCellChange change)
        {
            if (!IsDisposed)
                dirtyRegions.Mark(change);
        }

        public void OnWorldChanged(WorldChangeBatch batch)
        {
            if (!IsDisposed)
                dirtyRegions.MarkBatch(batch);
        }

        public void Flush()
        {
            if (IsDisposed || dirtyRegions.Count == 0)
                return;

            var batches = dirtyRegions.Drain();
            for (int i = 0; i < batches.Count; i++)
                renderer.RenderRegion(batches[i].RegionId, batches[i].Changes);
        }

        public void Clear()
        {
            dirtyRegions.Clear();
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
            dirtyRegions.Clear();
        }
    }
}
