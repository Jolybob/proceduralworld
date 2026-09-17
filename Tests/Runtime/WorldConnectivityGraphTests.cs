using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldConnectivityGraphTests
    {
        [Test]
        public void Builder_IsDeterministicRegardlessOfInputOrder()
        {
            var first = new[]
            {
                Placement(3, 0, 0),
                Placement(1, 10, 0),
                Placement(2, 5, 0),
                Placement(4, 50, 0)
            };
            var second = new[]
            {
                first[3],
                first[1],
                first[0],
                first[2]
            };
            var settings = new WorldConnectivitySettings(6, 2);
            var builder = new WorldConnectivityGraphBuilder();

            WorldConnectivityGraph a = builder.Build(first, settings);
            WorldConnectivityGraph b = builder.Build(second, settings);

            Assert.AreEqual(a.Nodes.Count, b.Nodes.Count);
            Assert.AreEqual(a.Edges.Count, b.Edges.Count);
            for (int i = 0; i < a.Nodes.Count; i++)
            {
                Assert.AreEqual(a.Nodes[i], b.Nodes[i]);
                Assert.AreEqual(a.Edges[i], b.Edges[i]);
            }
        }

        [Test]
        public void Builder_BuildsConnectedBackboneWithinDistance()
        {
            var placements = new[]
            {
                Placement(1, 0, 0),
                Placement(2, 5, 0),
                Placement(3, 10, 0),
                Placement(4, 15, 0)
            };

            WorldConnectivityGraph graph = new WorldConnectivityGraphBuilder().Build(
                placements,
                new WorldConnectivitySettings(5, 2));

            Assert.AreEqual(1, graph.GetConnectedComponentCount());
            Assert.AreEqual(3, graph.Edges.Count);
        }

        [Test]
        public void Builder_RespectsMaximumConnectionsPerNode()
        {
            var placements = new[]
            {
                Placement(1, 0, 0),
                Placement(2, 2, 0),
                Placement(3, -2, 0),
                Placement(4, 0, 2)
            };

            WorldConnectivityGraph graph = new WorldConnectivityGraphBuilder().Build(
                placements,
                new WorldConnectivitySettings(3, 2));

            for (int i = 0; i < graph.Nodes.Count; i++)
                Assert.LessOrEqual(graph.GetNeighbors(i).Count, 2);
        }

        [Test]
        public void Builder_DeduplicatesIdenticalPlacements()
        {
            WorldFeaturePlacement placement = Placement(1, 10, 10);
            var placements = new[] { placement, placement, placement };

            WorldConnectivityGraph graph = new WorldConnectivityGraphBuilder().Build(
                placements,
                new WorldConnectivitySettings(8, 2));

            Assert.AreEqual(1, graph.Nodes.Count);
            Assert.AreEqual(0, graph.Edges.Count);
        }

        [Test]
        public void Builder_HandlesNegativeWorldCoordinatesWithoutBucketWrap()
        {
            var placements = new[]
            {
                Placement(1, int.MinValue + 2, 0),
                Placement(2, int.MinValue + 5, 0),
                Placement(3, -1, 0),
                Placement(4, 2, 0)
            };

            WorldConnectivityGraph graph = new WorldConnectivityGraphBuilder().Build(
                placements,
                new WorldConnectivitySettings(4, 2));

            Assert.AreEqual(2, graph.GetConnectedComponentCount());
        }

        [Test]
        public void BuildFromIndex_UsesWorldRectangleQueries()
        {
            var source = new StaticQuerySource(new[]
            {
                new WorldFeaturePlacement(1, new WorldPosition(2, 2), new ChunkCoord(0, 0), 1, 1),
                new WorldFeaturePlacement(1, new WorldPosition(7, 2), new ChunkCoord(0, 0), 1, 1)
            });
            var index = new WorldFeaturePlacementIndex(1234, 8, source);

            WorldConnectivityGraph graph = new WorldConnectivityGraphBuilder().Build(
                index,
                new WorldPosition(0, 0),
                new WorldPosition(8, 8),
                new WorldConnectivitySettings(6, 2));

            Assert.AreEqual(2, graph.Nodes.Count);
            Assert.AreEqual(1, graph.Edges.Count);
        }

        private static WorldFeaturePlacement Placement(int id, int x, int y)
        {
            return new WorldFeaturePlacement(
                id,
                new WorldPosition(x, y),
                new ChunkCoord(FloorDiv(x, 64), FloorDiv(y, 64)),
                1,
                1);
        }

        private static int FloorDiv(int value, int divisor)
        {
            long quotient = value / divisor;
            long remainder = value % divisor;
            if (remainder != 0L && value < 0) quotient--;
            return checked((int)quotient);
        }

        private sealed class StaticQuerySource : IWorldFeaturePlacementQuerySource
        {
            private readonly WorldFeaturePlacement[] placements;

            public StaticQuerySource(WorldFeaturePlacement[] placements)
            {
                this.placements = placements;
            }

            public void Collect(WorldFeaturePlacementQueryContext context, WorldFeaturePlacementSet output)
            {
                for (int i = 0; i < placements.Length; i++)
                {
                    if (placements[i].Intersects(context.ChunkCoordinate, context.ChunkSize))
                        output.Add(placements[i]);
                }
            }
        }
    }
}
