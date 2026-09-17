using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPresentationRegionLifecycleTests
    {
        [Test]
        public void LoadAndUnloadForwardRegionIdentity()
        {
            var lifecycle = new RecordingLifecycle();
            var coordinator = new WorldPresentationRegionLifecycleCoordinator(lifecycle);

            coordinator.Load(7);
            coordinator.Unload(7);

            Assert.AreEqual(7, lifecycle.LoadedRegion);
            Assert.AreEqual(7, lifecycle.UnloadedRegion);
        }

        [Test]
        public void DisposeStopsLifecycleOperations()
        {
            var lifecycle = new RecordingLifecycle();
            var coordinator = new WorldPresentationRegionLifecycleCoordinator(lifecycle);

            coordinator.Dispose();
            coordinator.Load(3);
            coordinator.Unload(3);

            Assert.IsTrue(coordinator.IsDisposed);
            Assert.AreEqual(-1, lifecycle.LoadedRegion);
            Assert.AreEqual(-1, lifecycle.UnloadedRegion);
        }

        private sealed class RecordingLifecycle : IWorldPresentationRegionLifecycle
        {
            public int LoadedRegion { get; private set; } = -1;
            public int UnloadedRegion { get; private set; } = -1;

            public void LoadRegion(int regionId) => LoadedRegion = regionId;
            public void UnloadRegion(int regionId) => UnloadedRegion = regionId;
        }
    }
}
