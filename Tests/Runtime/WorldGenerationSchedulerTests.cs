using NUnit.Framework;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldGenerationSchedulerTests
    {
        [Test]
        public void ScheduleIsDeterministicRegardlessOfRequestOrder()
        {
            var a = new WorldGenerationScheduler();
            a.Request(new ChunkCoord(2, 0), WorldGenerationWorkKind.Materialization, 1);
            a.Request(new ChunkCoord(-1, 3), WorldGenerationWorkKind.Realization, 2);
            a.Request(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan, 2);

            var b = new WorldGenerationScheduler();
            b.Request(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan, 2);
            b.Request(new ChunkCoord(2, 0), WorldGenerationWorkKind.Materialization, 1);
            b.Request(new ChunkCoord(-1, 3), WorldGenerationWorkKind.Realization, 2);

            WorldGenerationSchedule sa = a.BuildSchedule();
            WorldGenerationSchedule sb = b.BuildSchedule();
            Assert.AreEqual(sa.Count, sb.Count);
            for (int i = 0; i < sa.Count; i++) Assert.AreEqual(sa.Items[i], sb.Items[i]);
        }

        [Test]
        public void DuplicateChunkKeepsHighestPriorityWork()
        {
            var scheduler = new WorldGenerationScheduler();
            scheduler.Request(new ChunkCoord(1, 1), WorldGenerationWorkKind.Materialization, 1);
            scheduler.Request(new ChunkCoord(1, 1), WorldGenerationWorkKind.Realization, 2);

            WorldGenerationSchedule schedule = scheduler.BuildSchedule();
            Assert.AreEqual(1, schedule.Count);
            Assert.AreEqual(WorldGenerationWorkKind.Realization, schedule.Items[0].Kind);
            Assert.AreEqual(2, schedule.Items[0].Priority);
        }

        [Test]
        public void DequeueRespectsBudgetAndLeavesRemainingWorkPending()
        {
            var scheduler = new WorldGenerationScheduler();
            scheduler.Request(new ChunkCoord(0, 0), WorldGenerationWorkKind.Materialization);
            scheduler.Request(new ChunkCoord(1, 0), WorldGenerationWorkKind.Materialization);
            scheduler.Request(new ChunkCoord(2, 0), WorldGenerationWorkKind.Materialization);

            WorldGenerationSchedule first = scheduler.Dequeue(2);
            Assert.AreEqual(2, first.Count);
            Assert.AreEqual(1, scheduler.PendingCount);
        }

        [Test]
        public void CancellationRemovesOnlyRequestedChunk()
        {
            var scheduler = new WorldGenerationScheduler();
            scheduler.Request(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan);
            scheduler.Request(new ChunkCoord(1, 0), WorldGenerationWorkKind.Plan);

            Assert.IsTrue(scheduler.Cancel(new ChunkCoord(0, 0)));
            Assert.IsFalse(scheduler.Cancel(new ChunkCoord(0, 0)));
            Assert.AreEqual(1, scheduler.PendingCount);
        }
    }
}
