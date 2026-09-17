using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPresentationRegionDemandAggregatorTests
    {
        [Test]
        public void AggregatesSourcesInDeterministicFirstSeenOrder()
        {
            var aggregator = new WorldPresentationRegionDemandAggregator();

            aggregator.SetSourceDemand(10, new[] { 4, 7, 4 });
            aggregator.SetSourceDemand(20, new[] { 7, 9, 2 });

            CollectionAssert.AreEqual(new[] { 4, 7, 9, 2 }, aggregator.DemandedRegions);
        }

        [Test]
        public void UpdatingSourcePreservesSourceOrder()
        {
            var aggregator = new WorldPresentationRegionDemandAggregator();

            aggregator.SetSourceDemand(10, new[] { 1, 2 });
            aggregator.SetSourceDemand(20, new[] { 3, 4 });
            aggregator.SetSourceDemand(10, new[] { 5, 4 });

            CollectionAssert.AreEqual(new[] { 5, 4, 3 }, aggregator.DemandedRegions);
        }

        [Test]
        public void RemovingSourceReleasesOnlyItsUniqueRegions()
        {
            var aggregator = new WorldPresentationRegionDemandAggregator();

            aggregator.SetSourceDemand(10, new[] { 1, 2 });
            aggregator.SetSourceDemand(20, new[] { 2, 3 });
            aggregator.RemoveSource(10);

            CollectionAssert.AreEqual(new[] { 2, 3 }, aggregator.DemandedRegions);
            Assert.AreEqual(1, aggregator.SourceCount);
        }

        [Test]
        public void SharedRegionRemainsDemandedUntilLastSourceIsRemoved()
        {
            var aggregator = new WorldPresentationRegionDemandAggregator();

            aggregator.SetSourceDemand(10, new[] { 5 });
            aggregator.SetSourceDemand(20, new[] { 5 });
            aggregator.RemoveSource(10);

            CollectionAssert.AreEqual(new[] { 5 }, aggregator.DemandedRegions);
            Assert.IsTrue(aggregator.HasSource(20));
        }

        [Test]
        public void ClearingAllSourcesProducesEmptyDemand()
        {
            var aggregator = new WorldPresentationRegionDemandAggregator();

            aggregator.SetSourceDemand(10, new[] { 1, 2 });
            aggregator.SetSourceDemand(20, new[] { 3 });
            aggregator.Clear();

            Assert.AreEqual(0, aggregator.SourceCount);
            Assert.AreEqual(0, aggregator.DemandedRegions.Count);
        }

        [Test]
        public void MissingSourceRemovalIsNoOp()
        {
            var aggregator = new WorldPresentationRegionDemandAggregator();

            Assert.IsFalse(aggregator.RemoveSource(42));
            Assert.AreEqual(0, aggregator.SourceCount);
            Assert.AreEqual(0, aggregator.DemandedRegions.Count);
        }
    }
}
