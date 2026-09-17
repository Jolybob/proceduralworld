using System;

namespace Jolybob.ProceduralWorld
{
    public interface IWorldPresentationRegionLifecycle
    {
        void LoadRegion(int regionId);
        void UnloadRegion(int regionId);
    }

    public sealed class WorldPresentationRegionLifecycleCoordinator : IDisposable
    {
        private readonly IWorldPresentationRegionLifecycle lifecycle;
        private bool disposed;

        public bool IsDisposed => disposed;

        public WorldPresentationRegionLifecycleCoordinator(IWorldPresentationRegionLifecycle lifecycle)
        {
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        }

        public void Load(int regionId)
        {
            if (disposed) return;
            lifecycle.LoadRegion(regionId);
        }

        public void Unload(int regionId)
        {
            if (disposed) return;
            lifecycle.UnloadRegion(regionId);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
        }
    }
}
