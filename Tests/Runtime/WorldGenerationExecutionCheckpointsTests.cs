using System;
using System.Collections.Generic;
using NUnit.Framework;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldGenerationExecutionCheckpointsTests
    {
        private sealed class StepExecutor : IWorldGenerationResumableWorkExecutor
        {
            public readonly List<WorldGenerationExecutionCheckpoint> Received = new List<WorldGenerationExecutionCheckpoint>();
            public int Calls;
            public int CompleteOnCall = int.MaxValue;
            public bool FailOnCall;

            public WorldGenerationExecutionStep Execute(
                WorldGenerationWorkItem item,
                WorldGenerationExecutionLease lease,
                WorldGenerationExecutionCheckpoint checkpoint)
            {
                Received.Add(checkpoint);
                Calls++;
                if (FailOnCall)
                {
                    var failed = new WorldGenerationExecutionCheckpoint(
                        lease.WorkKey, lease.LeaseId, lease.Attempt, "Failed", checkpoint.Step,
                        checkpoint.ProgressOrdinal, checkpoint.CompletedUnits, checkpoint.TotalUnits);
                    return WorldGenerationExecutionStep.Fail(failed, "TestFailure", "Synthetic execution failure.");
                }

                bool complete = Calls >= CompleteOnCall;
                var next = new WorldGenerationExecutionCheckpoint(
                    lease.WorkKey,
                    lease.LeaseId,
                    lease.Attempt,
                    complete ? "Complete" : "Terrain",
                    checkpoint.Step + 1,
                    checkpoint.ProgressOrdinal + 1,
                    checkpoint.CompletedUnits + 1,
                    checkpoint.TotalUnits == 0 ? 2 : checkpoint.TotalUnits);
                return complete
                    ? WorldGenerationExecutionStep.Complete(next)
                    : WorldGenerationExecutionStep.Continue(next);
            }
        }

        private static WorldGenerationWorkKey Key(int x, int y, WorldGenerationWorkKind kind = WorldGenerationWorkKind.Plan) =>
            new WorldGenerationWorkKey(new ChunkCoord(x, y), kind);

        [Test]
        public void ResumableRunnerPreservesCheckpointAcrossBudgets()
        {
            var graph = new WorldGenerationWorkGraph();
            graph.Request(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan);
            var leases = new WorldGenerationExecutionLeaseCoordinator(graph);
            var checkpoints = new WorldGenerationExecutionCheckpointStore();
            var coordinator = new WorldGenerationResumableExecutionCoordinator(leases, checkpoints);
            var executor = new StepExecutor { CompleteOnCall = 2 };
            var runner = new WorldGenerationResumableExecutionRunner(coordinator, executor);

            Assert.AreEqual(1, runner.Run(1, "worker", 0, 10));
            Assert.AreEqual(WorldGenerationWorkStatus.Running, graph.GetStatus(Key(0, 0)));
            Assert.AreEqual(0, executor.Received[0].Step);

            Assert.AreEqual(1, runner.Run(1, "worker", 1, 10));
            Assert.AreEqual(WorldGenerationWorkStatus.Completed, graph.GetStatus(Key(0, 0)));
            Assert.AreEqual(2, executor.Received.Count);
            Assert.AreEqual(1, executor.Received[1].CompletedUnits);
        }

        [Test]
        public void ExpiredLeaseRebindsCheckpointWithoutLosingProgress()
        {
            var graph = new WorldGenerationWorkGraph();
            graph.Request(new ChunkCoord(2, -1), WorldGenerationWorkKind.Realization);
            var leases = new WorldGenerationExecutionLeaseCoordinator(graph);
            var checkpoints = new WorldGenerationExecutionCheckpointStore();
            var coordinator = new WorldGenerationResumableExecutionCoordinator(leases, checkpoints);

            WorldGenerationExecutionLeaseSchedule first = coordinator.Claim(1, "worker-a", 0, 5);
            WorldGenerationExecutionLease leaseA = first.Leases[0];
            WorldGenerationExecutionCheckpoint checkpoint = checkpoints.Prepare(leaseA);
            checkpoint = new WorldGenerationExecutionCheckpoint(
                checkpoint.WorkKey, checkpoint.LeaseId, checkpoint.Attempt, "Dungeon", 7, 7, 70, 100);
            Assert.IsTrue(coordinator.SaveCheckpoint(leaseA, checkpoint, 1));
            Assert.AreEqual(1, coordinator.RecoverExpired(5));

            WorldGenerationExecutionLeaseSchedule second = coordinator.Claim(1, "worker-b", 5, 5);
            WorldGenerationExecutionLease leaseB = second.Leases[0];
            Assert.AreEqual(2, leaseB.Attempt);
            Assert.AreNotEqual(leaseA.LeaseId, leaseB.LeaseId);
            Assert.IsTrue(coordinator.TryGetCheckpoint(leaseB.WorkKey, out WorldGenerationExecutionCheckpoint rebound));
            Assert.AreEqual("Dungeon", rebound.Phase);
            Assert.AreEqual(7, rebound.Step);
            Assert.AreEqual(70, rebound.CompletedUnits);
            Assert.AreEqual(leaseB.LeaseId, rebound.LeaseId);
        }

        [Test]
        public void StaleWorkerCannotWriteCheckpointAfterReclaim()
        {
            var graph = new WorldGenerationWorkGraph();
            graph.Request(new ChunkCoord(1, 1), WorldGenerationWorkKind.Plan);
            var leases = new WorldGenerationExecutionLeaseCoordinator(graph);
            var checkpoints = new WorldGenerationExecutionCheckpointStore();
            var coordinator = new WorldGenerationResumableExecutionCoordinator(leases, checkpoints);

            WorldGenerationExecutionLeaseSchedule first = coordinator.Claim(1, "worker-a", 0, 2);
            WorldGenerationExecutionLease stale = first.Leases[0];
            Assert.AreEqual(1, coordinator.RecoverExpired(2));
            WorldGenerationExecutionLease current = coordinator.Claim(1, "worker-b", 2, 2).Leases[0];
            WorldGenerationExecutionCheckpoint checkpoint = checkpoints.Prepare(current);

            var staleCheckpoint = new WorldGenerationExecutionCheckpoint(
                stale.WorkKey, stale.LeaseId, stale.Attempt, "Stale", 1, 1, 1, 2);
            Assert.IsFalse(coordinator.SaveCheckpoint(stale, staleCheckpoint, 2));
            Assert.IsTrue(coordinator.SaveCheckpoint(current, checkpoint, 2));
        }

        [Test]
        public void ActiveWorkIsResumedBeforeNewWorkIsClaimed()
        {
            var graph = new WorldGenerationWorkGraph();
            graph.Request(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan, 10);
            graph.Request(new ChunkCoord(1, 0), WorldGenerationWorkKind.Plan, 1);
            var leases = new WorldGenerationExecutionLeaseCoordinator(graph);
            var checkpoints = new WorldGenerationExecutionCheckpointStore();
            var coordinator = new WorldGenerationResumableExecutionCoordinator(leases, checkpoints);
            var executor = new StepExecutor { CompleteOnCall = 100 };
            var runner = new WorldGenerationResumableExecutionRunner(coordinator, executor);

            Assert.AreEqual(1, runner.Run(1, "worker", 0, 10));
            Assert.AreEqual(new ChunkCoord(0, 0), executor.Received[0].WorkKey.Chunk);
            Assert.AreEqual(1, runner.Run(1, "worker", 1, 10));
            Assert.AreEqual(2, executor.Received.Count);
            Assert.AreEqual(new ChunkCoord(0, 0), executor.Received[1].WorkKey.Chunk);
        }

        [Test]
        public void FailedStepLeavesCheckpointForExplicitRetry()
        {
            var graph = new WorldGenerationWorkGraph();
            graph.Request(new ChunkCoord(3, 3), WorldGenerationWorkKind.Materialization);
            var leases = new WorldGenerationExecutionLeaseCoordinator(graph);
            var checkpoints = new WorldGenerationExecutionCheckpointStore();
            var coordinator = new WorldGenerationResumableExecutionCoordinator(leases, checkpoints);
            var executor = new StepExecutor { FailOnCall = true };
            var runner = new WorldGenerationResumableExecutionRunner(coordinator, executor);

            Assert.Throws<InvalidOperationException>(() => runner.Run(1, "worker", 0, 10));
            Assert.AreEqual(WorldGenerationWorkStatus.Failed, graph.GetStatus(Key(3, 3, WorldGenerationWorkKind.Materialization)));
            Assert.AreEqual(1, checkpoints.Count);
            Assert.IsTrue(graph.Retry(Key(3, 3, WorldGenerationWorkKind.Materialization)));
        }
    }
}
