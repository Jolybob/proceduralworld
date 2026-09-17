using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPresentationRegionDemandCoordinatorTests
    {
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
            var received = new List<IReadOnlyList<int>>();

            coordinator.DemandChanged += change => received.Add(change.DemandedRegions);
            coordinator.SetDemandedRegions(new[] { 1, 3 });
            coordinator.SetDemandedRegions(new[] { 8 });

            Assert.AreEqual(2, received.Count);
            CollectionAssert.AreEqual(new[] { 1, 3 }, received[0]);
            CollectionAssert.AreEqual(new[] { 8 }, received[1]);
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
