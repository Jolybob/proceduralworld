using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPresentationRegionDemandSourceTests
    {
        [Test]
        public void RegisterRefreshesSourceDemand()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var demand = new WorldPresentationRegionDemandCoordinator(residency);
            var sources = new WorldPresentationRegionDemandSourceCoordinator(demand);

            sources.Register(new TestSource(7, 2, 5));

            Assert.IsTrue(sources.HasSource(7));
            Assert.AreEqual(1, sources.SourceCount);
            CollectionAssert.AreEqual(new[] { 2, 5 }, lifecycle.LoadedRegions);
        }

        [Test]
        public void DemandChangedRefreshesOnlyThatSource()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var demand = new WorldPresentationRegionDemandCoordinator(residency);
            var sources = new WorldPresentationRegionDemandSourceCoordinator(demand);
            var first = new TestSource(1, 2);
            var second = new TestSource(2, 5);

            sources.Register(first);
            sources.Register(second);
            first.SetRegions(3);

            CollectionAssert.AreEqual(new[] { 2, 5, 3 }, lifecycle.LoadedRegions);
            CollectionAssert.AreEqual(new[] { 2 }, lifecycle.UnloadedRegions);
        }

        [Test]
        public void BatchCoalescesRepeatedSourceChanges()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var demand = new WorldPresentationRegionDemandCoordinator(residency);
            var sources = new WorldPresentationRegionDemandSourceCoordinator(demand);
            var source = new TestSource(1, 2);

            sources.Register(source);
            using (sources.BeginBatch())
            {
                source.SetRegions(3);
                source.SetRegions(4);
                source.SetRegions(5);
                Assert.IsTrue(sources.IsBatching);
                CollectionAssert.AreEqual(new[] { 2 }, lifecycle.LoadedRegions);
            }

            Assert.IsFalse(sources.IsBatching);
            CollectionAssert.AreEqual(new[] { 2, 5 }, lifecycle.LoadedRegions);
            CollectionAssert.AreEqual(new[] { 2 }, lifecycle.UnloadedRegions);
        }

        [Test]
        public void NestedBatchesFlushOnlyWhenOuterBatchEnds()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var demand = new WorldPresentationRegionDemandCoordinator(residency);
            var sources = new WorldPresentationRegionDemandSourceCoordinator(demand);
            var source = new TestSource(1, 2);

            sources.Register(source);
            using (sources.BeginBatch())
            {
                source.SetRegions(3);
                using (sources.BeginBatch())
                {
                    source.SetRegions(4);
                }

                CollectionAssert.AreEqual(new[] { 2 }, lifecycle.LoadedRegions);
            }

            CollectionAssert.AreEqual(new[] { 2, 4 }, lifecycle.LoadedRegions);
            CollectionAssert.AreEqual(new[] { 2 }, lifecycle.UnloadedRegions);
        }

        [Test]
        public void RefreshAllWithinBatchIsDeferred()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var demand = new WorldPresentationRegionDemandCoordinator(residency);
            var sources = new WorldPresentationRegionDemandSourceCoordinator(demand);
            var source = new TestSource(1, 2);

            sources.Register(source);
            source.SetRegionsWithoutNotification(5);

            using (sources.BeginBatch())
            {
                sources.RefreshAll();
                CollectionAssert.AreEqual(new[] { 2 }, lifecycle.LoadedRegions);
            }

            CollectionAssert.AreEqual(new[] { 2, 5 }, lifecycle.LoadedRegions);
            CollectionAssert.AreEqual(new[] { 2 }, lifecycle.UnloadedRegions);
        }

        [Test]
        public void UnregisteredSourceCannotRefreshDemand()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var demand = new WorldPresentationRegionDemandCoordinator(residency);
            var sources = new WorldPresentationRegionDemandSourceCoordinator(demand);
            var source = new TestSource(1, 2);

            sources.Register(source);
            sources.Unregister(1);
            source.SetRegions(5);

            CollectionAssert.AreEqual(new[] { 2 }, lifecycle.LoadedRegions);
            CollectionAssert.AreEqual(new[] { 2 }, lifecycle.UnloadedRegions);
        }

        [Test]
        public void ReplacingSourceDetachesOldReactiveSubscription()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var demand = new WorldPresentationRegionDemandCoordinator(residency);
            var sources = new WorldPresentationRegionDemandSourceCoordinator(demand);
            var oldSource = new TestSource(1, 2);
            var newSource = new TestSource(1, 4);

            sources.Register(oldSource);
            sources.Register(newSource);
            oldSource.SetRegions(3);

            CollectionAssert.AreEqual(new[] { 2, 4 }, lifecycle.LoadedRegions);
            CollectionAssert.AreEqual(new[] { 2 }, lifecycle.UnloadedRegions);
        }

        [Test]
        public void ReplacingSourceKeepsRegistrationOrder()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var demand = new WorldPresentationRegionDemandCoordinator(residency);
            var sources = new WorldPresentationRegionDemandSourceCoordinator(demand);

            sources.Register(new TestSource(1, 2));
            sources.Register(new TestSource(2, 4));
            sources.Register(new TestSource(1, 3));

            sources.RefreshAll();

            Assert.AreEqual(2, sources.SourceCount);
            CollectionAssert.AreEqual(new[] { 2, 4, 3 }, lifecycle.LoadedRegions);
            CollectionAssert.AreEqual(new[] { 2 }, lifecycle.UnloadedRegions);
        }

        [Test]
        public void UnregisterReleasesOnlyThatSourceDemand()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var demand = new WorldPresentationRegionDemandCoordinator(residency);
            var sources = new WorldPresentationRegionDemandSourceCoordinator(demand);

            sources.Register(new TestSource(1, 2, 8));
            sources.Register(new TestSource(2, 8, 9));
            sources.Unregister(1);

            CollectionAssert.AreEqual(new[] { 2, 8, 9 }, lifecycle.LoadedRegions);
            CollectionAssert.AreEqual(new[] { 2 }, lifecycle.UnloadedRegions);
        }

        [Test]
        public void ClearRemovesAllRegisteredSources()
        {
            var lifecycle = new RecordingLifecycle();
            var residency = new WorldPresentationRegionResidencyCoordinator(lifecycle);
            var demand = new WorldPresentationRegionDemandCoordinator(residency);
            var sources = new WorldPresentationRegionDemandSourceCoordinator(demand);

            sources.Register(new TestSource(1, 2));
            sources.Register(new TestSource(2, 5));
            sources.Clear();

            Assert.AreEqual(0, sources.SourceCount);
            CollectionAssert.AreEqual(new[] { 5, 2 }, lifecycle.UnloadedRegions);
        }

        private sealed class TestSource : IWorldPresentationRegionDemandSource
        {
            private int[] regions;

            public int SourceId { get; }
            public event Action<int> DemandChanged;

            public TestSource(int sourceId, params int[] regions)
            {
                SourceId = sourceId;
                this.regions = regions;
            }

            public IEnumerable<int> GetDemandedRegions() => regions;

            public void SetRegions(params int[] nextRegions)
            {
                regions = nextRegions;
                DemandChanged?.Invoke(SourceId);
            }

            public void SetRegionsWithoutNotification(params int[] nextRegions)
            {
                regions = nextRegions;
            }
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
