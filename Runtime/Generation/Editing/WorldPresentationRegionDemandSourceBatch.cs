using System;

namespace Jolybob.ProceduralWorld
{
    public sealed class WorldPresentationRegionDemandSourceBatch : IDisposable
    {
        private readonly WorldPresentationRegionDemandSourceCoordinator coordinator;
        private bool disposed;

        internal WorldPresentationRegionDemandSourceBatch(WorldPresentationRegionDemandSourceCoordinator coordinator)
        {
            this.coordinator = coordinator;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            coordinator.EndBatch();
        }
    }
}
