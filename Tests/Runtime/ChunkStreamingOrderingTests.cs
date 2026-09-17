using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class ChunkStreamingOrderingTests
    {
        [Test]
        public void DefaultOrderLoadsNearestChunksFirst()
        {
            var planner = new ChunkStreamingPlanner(2);

            ChunkStreamingDelta delta = planner.Update(new ChunkCoord(10, 20));

            Assert.AreEqual(new ChunkCoord(10, 20), delta.ToLoad[0]);
            for (int i = 1; i < delta.ToLoad.Count; i++)
            {
                Assert.LessOrEqual(
                    Distance(delta.ToLoad[i - 1], new ChunkCoord(10, 20)),
                    Distance(delta.ToLoad[i], new ChunkCoord(10, 20)));
            }
        }

        [Test]
        public void EqualPriorityUsesDeterministicYXOrder()
        {
            var planner = new ChunkStreamingPlanner(1);
            ChunkStreamingDelta delta = planner.Update(new ChunkCoord(0, 0));

            CollectionAssert.AreEqual(
                new[]
                {
                    new ChunkCoord(0, 0),
                    new ChunkCoord(-1, 0),
                    new ChunkCoord(1, 0),
                    new ChunkCoord(0, -1),
                    new ChunkCoord(0, 1),
                    new ChunkCoord(-1, -1),
                    new ChunkCoord(1, -1),
                    new ChunkCoord(-1, 1),
                    new ChunkCoord(1, 1)
                },
                delta.ToLoad);
        }

        [Test]
        public void CustomOrderIsUsedWithoutChangingActiveSet()
        {
            var order = new ReverseXThenYOrder();
            var planner = new ChunkStreamingPlanner(1, -1, order);

            ChunkStreamingDelta delta = planner.Update(new ChunkCoord(0, 0));

            Assert.AreSame(order, planner.LoadOrder);
            CollectionAssert.AreEqual(
                new[]
                {
                    new ChunkCoord(1, 1),
                    new ChunkCoord(1, 0),
                    new ChunkCoord(1, -1),
                    new ChunkCoord(0, 1),
                    new ChunkCoord(0, 0),
                    new ChunkCoord(0, -1),
                    new ChunkCoord(-1, 1),
                    new ChunkCoord(-1, 0),
                    new ChunkCoord(-1, -1)
                },
                delta.ToLoad);
            Assert.AreEqual(9, planner.ActiveChunks.Count);
        }

        [Test]
        public void OrderingHandlesLargeCoordinatesWithoutOverflow()
        {
            var planner = new ChunkStreamingPlanner(1);
            ChunkStreamingDelta delta = planner.Update(new ChunkCoord(int.MaxValue, int.MinValue));

            Assert.AreEqual(new ChunkCoord(int.MaxValue, int.MinValue), delta.ToLoad[0]);
            Assert.AreEqual(9, delta.ToLoad.Count);
        }

        private static long Distance(ChunkCoord coordinate, ChunkCoord center)
        {
            return System.Math.Abs((long)coordinate.X - center.X) +
                   System.Math.Abs((long)coordinate.Y - center.Y);
        }

        private sealed class ReverseXThenYOrder : IChunkStreamingOrder
        {
            public void Sort(IList<ChunkCoord> coordinates, ChunkCoord center)
            {
                for (int i = 0; i < coordinates.Count - 1; i++)
                {
                    for (int j = i + 1; j < coordinates.Count; j++)
                    {
                        if (Compare(coordinates[i], coordinates[j]) <= 0)
                            continue;

                        ChunkCoord temp = coordinates[i];
                        coordinates[i] = coordinates[j];
                        coordinates[j] = temp;
                    }
                }
            }

            private static int Compare(ChunkCoord left, ChunkCoord right)
            {
                int x = right.X.CompareTo(left.X);
                return x != 0 ? x : right.Y.CompareTo(left.Y);
            }
        }
    }
}
