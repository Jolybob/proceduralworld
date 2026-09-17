using System.Collections.Generic;
using NUnit.Framework;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldReservationMapTests
    {
        [Test]
        public void CrossChunkReservationIsQueryableWithoutDuplicates()
        {
            var map = new WorldReservationMap(4);
            var reservation = new WorldReservation("room", "node:a", "Feature", 0, new WorldPosition(3, 3), new WorldPosition(5, 5));
            Assert.IsTrue(map.TryReserve(reservation, out WorldReservationConflict ignored));

            var results = new List<WorldReservation>();
            map.CollectIntersecting(new WorldPosition(0, 0), new WorldPosition(7, 7), results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(reservation, results[0]);
        }

        [Test]
        public void OverlapIsRejectedButSameOwnerCanQueryItsOwnReservation()
        {
            var map = new WorldReservationMap(8);
            Assert.IsTrue(map.TryReserve(new WorldReservation("a", "node:a", "Feature", 0, new WorldPosition(0, 0), new WorldPosition(2, 2)), out WorldReservationConflict ignored));
            Assert.IsFalse(map.TryReserve(new WorldReservation("b", "node:b", "Feature", 0, new WorldPosition(2, 2), new WorldPosition(4, 4)), out WorldReservationConflict conflict));
            Assert.AreEqual("a", conflict.Existing.Id);
            Assert.IsTrue(map.CanReserve(new WorldPosition(0, 0), new WorldPosition(2, 2), "node:a"));
        }

        [Test]
        public void NegativeCoordinatesUseMathematicalChunkDivision()
        {
            var map = new WorldReservationMap(4);
            Assert.IsTrue(map.TryReserve(new WorldReservation("negative", "node", "Feature", 0, new WorldPosition(-4, -4), new WorldPosition(-1, -1)), out WorldReservationConflict ignored));
            var results = new List<WorldReservation>();
            map.CollectContaining(new WorldPosition(-1, -1), results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("negative", results[0].Id);
        }

        [Test]
        public void CorridorReservationRollsBackOnConflict()
        {
            var map = new WorldReservationMap(4);
            Assert.IsTrue(map.TryReserve(new WorldReservation("block", "protected", "Protected", 0, new WorldPosition(2, 0), new WorldPosition(2, 0)), out WorldReservationConflict ignored));
            var corridor = new WorldPlanCorridor("connection", "a", "b", new[]
            {
                new WorldPosition(1, 0),
                new WorldPosition(2, 0),
                new WorldPosition(3, 0)
            });

            Assert.IsFalse(WorldPlanCorridorReservationWriter.TryReserve(map, corridor));
            Assert.IsFalse(map.IsReserved(new WorldPosition(1, 0), "other"));
            Assert.IsTrue(map.IsReserved(new WorldPosition(2, 0), "other"));
            Assert.IsFalse(map.IsReserved(new WorldPosition(3, 0), "other"));
        }
    }
}
