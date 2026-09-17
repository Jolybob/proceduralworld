using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPresentationRegionResidencyTests
    {
        [Test]
        public void EnsureLoadedIsIdempotent()
        {
            var lifecycle = new RecordingLifecycle();
            var coordinator = new WorldPresentationRegionResidencyCoordinator(lifecycle);

            Assert.IsTrue(coordinator.EnsureLoaded(4));
            Assert.IsFalse(coordinator.EnsureLoaded(4));

            Assert.IsTrue(coordinator.IsLoaded(4));
            Assert.AreEqual(1, coordinator.LoadedRegionCount);
            Assert.AreEqual(1, lifecycle.LoadCount);
        }

        [Test]
        public void EnsureUnloadedIsIdempotent()
        {
            var lifecycle = new RecordingLifecycle();
            var coordinator = new WorldPresentationRegionResidencyCoordinator(lifecycle);

            Assert.IsFalse(coordinator.EnsureUnloaded(4));
            coordinator.EnsureLoaded(4);

            Assert.IsTrue(coordinator.EnsureUnloaded(4));
            Assert.IsFalse(coordinator.EnsureUnloaded(4));

            Assert.IsFalse(coordinator.IsLoaded(4));
            Assert.AreEqual(0, coordinator.LoadedRegionCount);
            Assert.AreEqual(1, lifecycle.UnloadCount);
        }

        [Test]
        public void ClearUnloadsInReverseLoadOrder()
        {
            var lifecycle = new RecordingLifecycle();
            var coordinator = new WorldPresentationRegionResidencyCoordinator(lifecycle);

            coordinator.EnsureLoaded(2);
            coordinator.EnsureLoaded(5);
            coordinator.EnsureLoaded(9);
            coordinator.Clear();

            CollectionAssert.AreEqual(new[] { 9, 5, 2 }, lifecycle.UnloadedRegions);
            Assert.AreEqual(0, coordinator.LoadedRegionCount);
        }

        [Test]
        public void DisposeStopsResidencyOperations()
        {
            var lifecycle = new RecordingLifecycle();
            var coordinator = new WorldPresentationRegionResidencyCoordinator(lifecycle);

            coordinator.EnsureLoaded(7);
            coordinator.Dispose();

            Assert.IsTrue(coordinator.IsDisposed);
            Assert.IsFalse(coordinator.EnsureLoaded(8));
            Assert.IsFalse(coordinator.EnsureUnloaded(7));
            Assert.AreEqual(0, coordinator.LoadedRegionCount);
            Assert.AreEqual(0, lifecycle.UnloadCount);
        }

        private sealed class RecordingLifecycle : IWorldPresentationRegionLifecycle
        {
            public int LoadCount { get; private set; }
            public int UnloadCount { get; private set; }
            public System.Collections.Generic.List<int> UnloadedRegions { get; } = new System.Collections.Generic.List<int>();

            public void LoadRegion(int regionId) => LoadCount++;

            public void UnloadRegion(int regionId)
            {
                UnloadCount++;
                UnloadedRegions.Add(regionId);
            }
        }
    }
}
