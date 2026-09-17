using NUnit.Framework;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldGenerationExecutionLeasesTests
    {
        [Test]
        public void ClaimCreatesDeterministicLeaseAndKeepsDependentsBlocked()
        {
            var graph = new WorldGenerationWorkGraph();
            var source = new WorldGenerationWorkKey(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan);
            var dependent = new WorldGenerationWorkKey(new ChunkCoord(1, 0), WorldGenerationWorkKind.Realization);
            graph.AddDependency(source, dependent);

            WorldGenerationExecutionLeaseSchedule schedule = graph.Claim(1, "worker-a", 10, 5);

            Assert.IsTrue(schedule.Succeeded);
            Assert.AreEqual(1, schedule.Count);
            Assert.AreEqual(source, schedule.Leases[0].WorkKey);
            Assert.AreEqual("worker-a", schedule.Leases[0].OwnerId);
            Assert.AreEqual(1, schedule.Leases[0].Attempt);
            Assert.AreEqual(15, schedule.Leases[0].ExpiresAtTick);
            Assert.AreEqual(WorldGenerationWorkStatus.Running, graph.GetStatus(source));
            Assert.AreEqual(1, graph.PendingCount);
            Assert.AreEqual(1, graph.RunningCount);
            Assert.AreEqual(0, graph.BuildSchedule().Count);
        }

        [Test]
        public void RenewalExtendsLeaseButCannotResurrectAnExpiredLease()
        {
            var graph = new WorldGenerationWorkGraph();
            var key = new WorldGenerationWorkKey(new ChunkCoord(2, -3), WorldGenerationWorkKind.Materialization);
            graph.Request(key.Chunk, key.Kind);

            WorldGenerationExecutionLease lease = graph.Claim(1, "worker-a", 10, 5).Leases[0];
            Assert.IsTrue(graph.Renew(lease, 12, 10));
            Assert.IsTrue(graph.TryGetLease(key, out WorldGenerationExecutionLease renewed));
            Assert.AreEqual(22, renewed.ExpiresAtTick);
            Assert.AreEqual(lease.LeaseId, renewed.LeaseId);
            Assert.AreEqual(lease.Attempt, renewed.Attempt);
            Assert.IsFalse(graph.Renew(lease, 12, 10));
            Assert.IsFalse(graph.Renew(renewed, 22, 1));
        }

        [Test]
        public void ExpiredLeaseIsReclaimedWithANewAttempt()
        {
            var graph = new WorldGenerationWorkGraph();
            var key = new WorldGenerationWorkKey(new ChunkCoord(-4, 7), WorldGenerationWorkKind.Realization);
            graph.Request(key.Chunk, key.Kind);

            WorldGenerationExecutionLease first = graph.Claim(1, "worker-a", 100, 10).Leases[0];
            Assert.AreEqual(1, graph.RecoverExpired(110));
            Assert.AreEqual(WorldGenerationWorkStatus.Pending, graph.GetStatus(key));

            WorldGenerationExecutionLease second = graph.Claim(1, "worker-b", 110, 10).Leases[0];
            Assert.AreEqual(2, second.Attempt);
            Assert.AreNotEqual(first.LeaseId, second.LeaseId);
            Assert.AreEqual(WorldGenerationWorkStatus.Running, graph.GetStatus(key));
        }

        [Test]
        public void StaleWorkerCannotCompleteAReclaimedExecution()
        {
            var graph = new WorldGenerationWorkGraph();
            var key = new WorldGenerationWorkKey(new ChunkCoord(3, 3), WorldGenerationWorkKind.Plan);
            graph.Request(key.Chunk, key.Kind);

            WorldGenerationExecutionLease stale = graph.Claim(1, "worker-a", 0, 1).Leases[0];
            graph.RecoverExpired(1);
            WorldGenerationExecutionLease current = graph.Claim(1, "worker-b", 1, 10).Leases[0];

            Assert.IsFalse(graph.Complete(stale));
            Assert.AreEqual(WorldGenerationWorkStatus.Running, graph.GetStatus(key));
            Assert.IsTrue(graph.Complete(current));
            Assert.AreEqual(WorldGenerationWorkStatus.Completed, graph.GetStatus(key));
        }

        [Test]
        public void ClaimOrderIsDeterministicRegardlessOfRequestOrder()
        {
            var a = new WorldGenerationWorkGraph();
            a.Request(new ChunkCoord(3, 0), WorldGenerationWorkKind.Materialization, 1);
            a.Request(new ChunkCoord(-1, 2), WorldGenerationWorkKind.Realization, 3);
            a.Request(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan, 3);

            var b = new WorldGenerationWorkGraph();
            b.Request(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan, 3);
            b.Request(new ChunkCoord(3, 0), WorldGenerationWorkKind.Materialization, 1);
            b.Request(new ChunkCoord(-1, 2), WorldGenerationWorkKind.Realization, 3);

            WorldGenerationExecutionLeaseSchedule sa = a.Claim(3, "worker-a", 50, 20);
            WorldGenerationExecutionLeaseSchedule sb = b.Claim(3, "worker-a", 50, 20);
            Assert.IsTrue(sa.Succeeded);
            Assert.AreEqual(sa.Count, sb.Count);
            for (int i = 0; i < sa.Count; i++)
            {
                Assert.AreEqual(sa.Items[i], sb.Items[i]);
                Assert.AreEqual(sa.Leases[i], sb.Leases[i]);
            }
        }

        [Test]
        public void RunnerMarksSuccessfulWorkCompleted()
        {
            var graph = new WorldGenerationWorkGraph();
            var key = new WorldGenerationWorkKey(new ChunkCoord(4, 1), WorldGenerationWorkKind.Plan);
            graph.Request(key.Chunk, key.Kind);
            var coordinator = new WorldGenerationExecutionLeaseCoordinator(graph);
            var executor = new RecordingExecutor(false);
            var runner = new WorldGenerationExecutionLeaseRunner(coordinator, executor);

            Assert.AreEqual(1, runner.Run(1, "worker", 0, 10));
            Assert.AreEqual(1, executor.ExecutionCount);
            Assert.AreEqual(WorldGenerationWorkStatus.Completed, graph.GetStatus(key));
        }

        [Test]
        public void RunnerMarksFailedWorkFailedAndReleasesLease()
        {
            var graph = new WorldGenerationWorkGraph();
            var key = new WorldGenerationWorkKey(new ChunkCoord(4, 2), WorldGenerationWorkKind.Realization);
            graph.Request(key.Chunk, key.Kind);
            var coordinator = new WorldGenerationExecutionLeaseCoordinator(graph);
            var executor = new RecordingExecutor(true);
            var runner = new WorldGenerationExecutionLeaseRunner(coordinator, executor);

            Assert.Throws<System.InvalidOperationException>(() => runner.Run(1, "worker", 0, 10));
            Assert.AreEqual(WorldGenerationWorkStatus.Failed, graph.GetStatus(key));
            Assert.IsFalse(graph.TryGetLease(key, out WorldGenerationExecutionLease ignored));
            Assert.IsTrue(graph.Retry(key));
            Assert.AreEqual(WorldGenerationWorkStatus.Pending, graph.GetStatus(key));
        }

        private sealed class RecordingExecutor : IWorldGenerationLeasedWorkExecutor
        {
            private readonly bool shouldFail;
            public int ExecutionCount { get; private set; }

            public RecordingExecutor(bool shouldFail)
            {
                this.shouldFail = shouldFail;
            }

            public void Execute(WorldGenerationWorkItem item, WorldGenerationExecutionLease lease)
            {
                ExecutionCount++;
                if (shouldFail) throw new System.InvalidOperationException("Synthetic execution failure.");
            }
        }
    }
}
