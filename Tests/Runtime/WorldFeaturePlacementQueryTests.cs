using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldFeaturePlacementQueryTests
    {
        [Test]
        public void Index_CachesEachChunkQuery()
        {
            var source = new CountingSource();
            var index = new WorldFeaturePlacementIndex(1234, 64, source);

            var first = index.GetChunk(new ChunkCoord(2, -3));
            var second = index.GetChunk(new ChunkCoord(2, -3));

            Assert.AreSame(first, second);
            Assert.AreEqual(1, source.Calls);
            Assert.AreEqual(1, index.CachedChunkCount);
        }

        [Test]
        public void PointQuery_UsesMathematicalFloorDivisionForNegativeCoordinates()
        {
            var placement = new WorldFeaturePlacement(
                7,
                new WorldPosition(-2, -1),
                new ChunkCoord(-1, -1),
                3,
                1);
            var source = new CountingSource(placement);
            var index = new WorldFeaturePlacementIndex(1234, 64, source);
            var output = new WorldFeaturePlacementSet();

            int added = index.CollectContaining(new WorldPosition(-1, -1), output);

            Assert.AreEqual(1, added);
            Assert.AreEqual(1, output.Count);
            Assert.AreEqual(new ChunkCoord(-1, -1), source.LastContext.ChunkCoordinate);
            Assert.IsTrue(output.Placements[0].Contains(new WorldPosition(-1, -1)));
        }

        [Test]
        public void AreaQuery_DeduplicatesPlacementDiscoveredThroughMultipleChunks()
        {
            var placement = new WorldFeaturePlacement(
                9,
                new WorldPosition(63, 2),
                new ChunkCoord(0, 0),
                2,
                2);
            var source = new CountingSource(placement);
            var index = new WorldFeaturePlacementIndex(1234, 64, source);
            var output = new WorldFeaturePlacementSet();

            int added = index.CollectIntersecting(
                new WorldPosition(63, 2),
                new WorldPosition(64, 3),
                output);

            Assert.AreEqual(1, added);
            Assert.AreEqual(1, output.Count);
            Assert.AreEqual(2, source.Calls);
        }

        [Test]
        public void Invalidate_CausesTheChunkToBePlannedAgain()
        {
            var source = new CountingSource();
            var index = new WorldFeaturePlacementIndex(1234, 64, source);
            var coordinate = new ChunkCoord(-2, 5);

            index.GetChunk(coordinate);
            Assert.IsTrue(index.Invalidate(coordinate));
            index.GetChunk(coordinate);

            Assert.AreEqual(2, source.Calls);
            Assert.AreEqual(1, index.CachedChunkCount);
        }

        private sealed class CountingSource : IWorldFeaturePlacementQuerySource
        {
            private readonly WorldFeaturePlacement placement;
            public int Calls { get; private set; }
            public WorldFeaturePlacementQueryContext LastContext { get; private set; }

            public CountingSource()
                : this(null)
            {
            }

            public CountingSource(WorldFeaturePlacement? placement)
            {
                this.placement = placement ?? default(WorldFeaturePlacement);
                HasPlacement = placement.HasValue;
            }

            public bool HasPlacement { get; }

            public void Collect(WorldFeaturePlacementQueryContext context, WorldFeaturePlacementSet output)
            {
                Calls++;
                LastContext = context;

                if (HasPlacement)
                    output.Add(placement);
            }
        }
    }
}
