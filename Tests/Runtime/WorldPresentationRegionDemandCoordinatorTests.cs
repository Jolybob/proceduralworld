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

        [Test]
        public void DemandChangedPublishesDeterministicSnapshot()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var coordinator = new WorldPresentationRegionDemandCoordinator(residency);
            var received = new List<IReadOnlyList<int>>();

            coordinator.DemandChanged += change => received.Add(change.DemandedRegions);
            coordinator.SetDemandedRegions(new[] { 7, 4, 7, 2 });
            coordinator.SetDemandedRegions(new[] { 2, 9 });

            Assert.AreEqual(2, received.Count);
            CollectionAssert.AreEqual(new[] { 7, 4, 2 }, received[0]);
            CollectionAssert.AreEqual(new[] { 2, 9 }, received[1]);
            CollectionAssert.AreEqual(new[] { 2, 9 }, coordinator.DemandedRegions);
        }

        [Test]
        public void DemandChangeSnapshotRemainsStableAfterLaterReconciliation()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var coordinator = new WorldPresentationRegionDemandCoordinator(residency);
            IReadOnlyList<int> received = null;

            coordinator.DemandChanged += change => received = change.DemandedRegions;
            coordinator.SetDemandedRegions(new[] { 1, 3 });
            coordinator.SetDemandedRegions(new[] { 8 });

            CollectionAssert.AreEqual(new[] { 1, 3 }, received);
        }

        [Test]
        public void RepeatedReconciliationDoesNotPublishRedundantChanges()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var coordinator = new WorldPresentationRegionDemandCoordinator(residency);
            var notifications = 0;

            coordinator.DemandChanged += _ => notifications++;
            coordinator.SetDemandedRegions(new[] { 2, 4 });
            coordinator.Reconcile();
            coordinator.SetDemandedRegions(new[] { 2, 4 });

            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void DisposedCoordinatorDoesNotPublishFurtherChanges()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var coordinator = new WorldPresentationRegionDemandCoordinator(residency);
            var notifications = 0;

            coordinator.DemandChanged += _ => notifications++;
            coordinator.SetDemandedRegions(new[] { 2 });
            coordinator.Dispose();
            coordinator.SetDemandedRegions(new[] { 5 });
            coordinator.Reconcile();

            Assert.AreEqual(1, notifications);
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
