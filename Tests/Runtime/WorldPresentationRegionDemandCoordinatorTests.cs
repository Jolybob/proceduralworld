using NUnit.Framework;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPresentationRegionDemandCoordinatorTests
    {
        [Test]
        public void ReconcileLoadsOnlyNewlyDemandedRegions()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var coordinator = new WorldPresentationRegionDemandCoordinator(residency);

            coordinator.SetDemandedRegions(new[] { 2, 5 });
            coordinator.SetDemandedRegions(new[] { 5, 9 });

            CollectionAssert.AreEqual(new[] { 2, 5, 9 }, lifecycle.LoadedRegions);
            CollectionAssert.AreEqual(new[] { 2 }, lifecycle.UnloadedRegions);
        }

        [Test]
        public void DuplicateDemandIsIgnoredAndOrderIsStable()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var coordinator = new WorldPresentationRegionDemandCoordinator(residency);

            coordinator.SetDemandedRegions(new[] { 7, 3, 7, 3 });

            Assert.AreEqual(2, coordinator.DemandedRegionCount);
            CollectionAssert.AreEqual(new[] { 7, 3 }, lifecycle.LoadedRegions);
        }

        [Test]
        public void EmptyDemandUnloadsAllRegionsInReverseDemandOrder()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var coordinator = new WorldPresentationRegionDemandCoordinator(residency);

            coordinator.SetDemandedRegions(new[] { 1, 4, 8 });
            coordinator.ClearDemand();

            CollectionAssert.AreEqual(new[] { 8, 4, 1 }, lifecycle.UnloadedRegions);
            Assert.AreEqual(0, coordinator.DemandedRegionCount);
        }

        [Test]
        public void MultipleSourcesShareRegionsAndReconcileAsOneDemandSet()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var coordinator = new WorldPresentationRegionDemandCoordinator(residency);

            coordinator.SetSourceDemand(10, new[] { 2, 5 });
            coordinator.SetSourceDemand(20, new[] { 5, 9 });
            coordinator.RemoveSourceDemand(10);

            Assert.AreEqual(1, coordinator.DemandSourceCount);
            Assert.AreEqual(2, coordinator.DemandedRegionCount);
            CollectionAssert.AreEqual(new[] { 2, 5, 9 }, lifecycle.LoadedRegions);
            CollectionAssert.AreEqual(new[] { 2 }, lifecycle.UnloadedRegions);
        }

        [Test]
        public void RemovingOneSourceDoesNotUnloadSharedRegion()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var coordinator = new WorldPresentationRegionDemandCoordinator(residency);

            coordinator.SetSourceDemand(1, new[] { 4, 8 });
            coordinator.SetSourceDemand(2, new[] { 8, 12 });
            coordinator.RemoveSourceDemand(1);

            Assert.IsTrue(coordinator.IsDemanded(8));
            CollectionAssert.AreEqual(new[] { 4 }, lifecycle.UnloadedRegions);
        }

        [Test]
        public void ReconcileIsIdempotentWithoutNewDemand()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var coordinator = new WorldPresentationRegionDemandCoordinator(residency);

            coordinator.SetSourceDemand(1, new[] { 3, 6 });
            coordinator.Reconcile();

            CollectionAssert.AreEqual(new[] { 3, 6 }, lifecycle.LoadedRegions);
            Assert.AreEqual(0, lifecycle.UnloadedRegions.Count);
        }

        [Test]
        public void DisposeStopsFutureSourceDemandChanges()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var coordinator = new WorldPresentationRegionDemandCoordinator(residency);

            coordinator.SetSourceDemand(1, new[] { 6 });
            coordinator.Dispose();
            coordinator.SetSourceDemand(2, new[] { 9 });

            Assert.IsTrue(coordinator.IsDisposed);
            CollectionAssert.AreEqual(new[] { 6 }, lifecycle.UnloadedRegions);
            CollectionAssert.DoesNotContain(lifecycle.LoadedRegions, 9);
        }

        private sealed class RecordingLifecycle : IWorldPresentationRegionLifecycle
        {
            public List<int> LoadedRegions { get; } = new List<int>();
            public List<int> UnloadedRegions { get; } = new List<int>();

            public void LoadRegion(int regionId) => LoadedRegions.Add(regionId);
            public void UnloadRegion(int regionId) => UnloadedRegions.Add(regionId);
        }
    }
}
