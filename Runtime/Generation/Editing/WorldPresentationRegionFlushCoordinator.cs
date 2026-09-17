using System;

namespace Jolybob.ProceduralWorld
{
    public sealed class WorldPresentationRegionFlushCoordinator : IDisposable, IWorldChangeListener, IWorldChangeBatchListener
    {
        private readonly WorldPresentationDirtyRegionSet dirtyRegions;
        private readonly IWorldPresentationRegionRenderer renderer;
        private IDisposable changeSubscription;
        private IDisposable batchSubscription;

        public bool IsDisposed { get; private set; }
        public int DirtyRegionCount => dirtyRegions.Count;

        public WorldPresentationRegionFlushCoordinator(WorldChangeObserverJournal journal, IWorldPresentationRegionResolver resolver, IWorldPresentationRegionRenderer renderer)
            : this(journal, new WorldPresentationDirtyRegionSet(resolver), renderer) { }

        public WorldPresentationRegionFlushCoordinator(WorldChangeObserverJournal journal, IWorldPresentationRegionImpactResolver impactResolver, IWorldPresentationRegionRenderer renderer)
            : this(journal, new WorldPresentationDirtyRegionSet(impactResolver), renderer) { }

        public WorldPresentationRegionFlushCoordinator(WorldChangeObserverJournal journal, IWorldPresentationRegionResolver resolver, IWorldPresentationRegionRenderer renderer, WorldPresentationDirtyRegionSet dirtyRegions)
            : this(journal, dirtyRegions ?? new WorldPresentationDirtyRegionSet(resolver), renderer) { }

        private WorldPresentationRegionFlushCoordinator(WorldChangeObserverJournal journal, WorldPresentationDirtyRegionSet dirtyRegions, IWorldPresentationRegionRenderer renderer)
        {
            if (journal == null) throw new ArgumentNullException(nameof(journal));
            this.dirtyRegions = dirtyRegions ?? throw new ArgumentNullException(nameof(dirtyRegions));
            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            changeSubscription = journal.Subscribe(this);
            batchSubscription = journal.SubscribeBatch(this);
        }

        public void OnWorldChanged(WorldCellChange change)
        {
            if (!IsDisposed) dirtyRegions.Mark(change);
        }

        public void OnWorldChanged(WorldChangeBatch batch)
        {
            if (!IsDisposed) dirtyRegions.MarkBatch(batch);
        }

        public void Flush()
        {
            if (IsDisposed || dirtyRegions.Count == 0) return;
            var batches = dirtyRegions.Drain();
            for (int i = 0; i < batches.Count; i++) renderer.RenderRegion(batches[i].RegionId, batches[i].Changes);
        }

        public void Clear() { dirtyRegions.Clear(); }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            changeSubscription.Dispose();
            batchSubscription.Dispose();
            changeSubscription = null;
            batchSubscription = null;
            dirtyRegions.Clear();
        }
    }
}
