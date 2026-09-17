using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public sealed class WorldPresentationRegionResidencyCoordinator : IDisposable
    {
        private readonly IWorldPresentationRegionLifecycle lifecycle;
        private readonly HashSet<int> loadedRegions = new HashSet<int>();
        private readonly List<int> loadOrder = new List<int>();
        private bool disposed;

        public bool IsDisposed => disposed;
        public int LoadedRegionCount => loadedRegions.Count;
        public IReadOnlyList<int> LoadedRegions => loadOrder;

        public WorldPresentationRegionResidencyCoordinator(IWorldPresentationRegionLifecycle lifecycle)
        {
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        }

        public bool IsLoaded(int regionId) => loadedRegions.Contains(regionId);

        public bool EnsureLoaded(int regionId)
        {
            if (disposed || !loadedRegions.Add(regionId)) return false;
            loadOrder.Add(regionId);
            lifecycle.LoadRegion(regionId);
            return true;
        }

        public bool EnsureUnloaded(int regionId)
        {
            if (disposed || !loadedRegions.Remove(regionId)) return false;
            loadOrder.Remove(regionId);
            lifecycle.UnloadRegion(regionId);
            return true;
        }

        public void Clear()
        {
            if (disposed) return;
            for (var i = loadOrder.Count - 1; i >= 0; i--) lifecycle.UnloadRegion(loadOrder[i]);
            loadOrder.Clear();
            loadedRegions.Clear();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            loadOrder.Clear();
            loadedRegions.Clear();
        }
    }
}
