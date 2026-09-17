using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public enum WorldGenerationExecutionStepStatus
    {
        Continue = 0,
        Completed = 1,
        Failed = 2
    }

    public readonly struct WorldGenerationExecutionCheckpoint : IEquatable<WorldGenerationExecutionCheckpoint>
    {
        public WorldGenerationWorkKey WorkKey { get; }
        public string LeaseId { get; }
        public int Attempt { get; }
        public string Phase { get; }
        public int Step { get; }
        public long ProgressOrdinal { get; }
        public long CompletedUnits { get; }
        public long TotalUnits { get; }

        public WorldGenerationExecutionCheckpoint(
            WorldGenerationWorkKey workKey,
            string leaseId,
            int attempt,
            string phase,
            int step,
            long progressOrdinal,
            long completedUnits,
            long totalUnits = 0)
        {
            if (string.IsNullOrWhiteSpace(leaseId)) throw new ArgumentException("Checkpoint lease ID must not be empty.", nameof(leaseId));
            if (attempt <= 0) throw new ArgumentOutOfRangeException(nameof(attempt));
            if (string.IsNullOrWhiteSpace(phase)) throw new ArgumentException("Checkpoint phase must not be empty.", nameof(phase));
            if (step < 0) throw new ArgumentOutOfRangeException(nameof(step));
            if (progressOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(progressOrdinal));
            if (completedUnits < 0) throw new ArgumentOutOfRangeException(nameof(completedUnits));
            if (totalUnits < 0) throw new ArgumentOutOfRangeException(nameof(totalUnits));
            if (totalUnits > 0 && completedUnits > totalUnits) throw new ArgumentException("Completed units must not exceed total units.");
            WorkKey = workKey;
            LeaseId = leaseId;
            Attempt = attempt;
            Phase = phase;
            Step = step;
            ProgressOrdinal = progressOrdinal;
            CompletedUnits = completedUnits;
            TotalUnits = totalUnits;
        }

        public bool Equals(WorldGenerationExecutionCheckpoint other) =>
            WorkKey.Equals(other.WorkKey) &&
            string.Equals(LeaseId, other.LeaseId, StringComparison.Ordinal) &&
            Attempt == other.Attempt &&
            string.Equals(Phase, other.Phase, StringComparison.Ordinal) &&
            Step == other.Step &&
            ProgressOrdinal == other.ProgressOrdinal &&
            CompletedUnits == other.CompletedUnits &&
            TotalUnits == other.TotalUnits;

        public override bool Equals(object obj) => obj is WorldGenerationExecutionCheckpoint other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(WorkKey, LeaseId, Attempt, Phase, Step, ProgressOrdinal, CompletedUnits, TotalUnits);
    }

    public readonly struct WorldGenerationExecutionStep : IEquatable<WorldGenerationExecutionStep>
    {
        public WorldGenerationExecutionStepStatus Status { get; }
        public WorldGenerationExecutionCheckpoint Checkpoint { get; }
        public string FailureCode { get; }
        public string FailureMessage { get; }

        public WorldGenerationExecutionStep(
            WorldGenerationExecutionStepStatus status,
            WorldGenerationExecutionCheckpoint checkpoint,
            string failureCode = null,
            string failureMessage = null)
        {
            if (status == WorldGenerationExecutionStepStatus.Failed && string.IsNullOrWhiteSpace(failureMessage))
                throw new ArgumentException("A failed execution step must provide a failure message.", nameof(failureMessage));
            Status = status;
            Checkpoint = checkpoint;
            FailureCode = failureCode ?? string.Empty;
            FailureMessage = failureMessage ?? string.Empty;
        }

        public static WorldGenerationExecutionStep Continue(WorldGenerationExecutionCheckpoint checkpoint) =>
            new WorldGenerationExecutionStep(WorldGenerationExecutionStepStatus.Continue, checkpoint);

        public static WorldGenerationExecutionStep Complete(WorldGenerationExecutionCheckpoint checkpoint) =>
            new WorldGenerationExecutionStep(WorldGenerationExecutionStepStatus.Completed, checkpoint);

        public static WorldGenerationExecutionStep Fail(WorldGenerationExecutionCheckpoint checkpoint, string failureCode, string failureMessage) =>
            new WorldGenerationExecutionStep(WorldGenerationExecutionStepStatus.Failed, checkpoint, failureCode, failureMessage);

        public bool Equals(WorldGenerationExecutionStep other) =>
            Status == other.Status && Checkpoint.Equals(other.Checkpoint) &&
            string.Equals(FailureCode, other.FailureCode, StringComparison.Ordinal) &&
            string.Equals(FailureMessage, other.FailureMessage, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is WorldGenerationExecutionStep other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Status, Checkpoint, FailureCode, FailureMessage);
    }

    public sealed class WorldGenerationExecutionCheckpointStore
    {
        private readonly Dictionary<WorldGenerationWorkKey, WorldGenerationExecutionCheckpoint> checkpoints =
            new Dictionary<WorldGenerationWorkKey, WorldGenerationExecutionCheckpoint>();

        public int Count => checkpoints.Count;

        public bool TryGet(WorldGenerationWorkKey key, out WorldGenerationExecutionCheckpoint checkpoint) =>
            checkpoints.TryGetValue(key, out checkpoint);

        public WorldGenerationExecutionCheckpoint Prepare(WorldGenerationExecutionLease lease)
        {
            if (checkpoints.TryGetValue(lease.WorkKey, out WorldGenerationExecutionCheckpoint existing))
            {
                var rebound = new WorldGenerationExecutionCheckpoint(
                    lease.WorkKey,
                    lease.LeaseId,
                    lease.Attempt,
                    existing.Phase,
                    existing.Step,
                    existing.ProgressOrdinal,
                    existing.CompletedUnits,
                    existing.TotalUnits);
                checkpoints[lease.WorkKey] = rebound;
                return rebound;
            }

            var initial = new WorldGenerationExecutionCheckpoint(
                lease.WorkKey,
                lease.LeaseId,
                lease.Attempt,
                "Start",
                0,
                0,
                0,
                0);
            checkpoints.Add(lease.WorkKey, initial);
            return initial;
        }

        public bool Save(
            WorldGenerationExecutionLease lease,
            WorldGenerationExecutionCheckpoint checkpoint,
            WorldGenerationExecutionLeaseCoordinator coordinator,
            long currentTick)
        {
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));
            if (!checkpoints.TryGetValue(lease.WorkKey, out WorldGenerationExecutionCheckpoint current)) return false;
            if (!coordinator.TryGet(lease.WorkKey, out WorldGenerationExecutionLease active) || !active.Equals(lease)) return false;
            if (active.IsExpired(currentTick)) return false;
            if (!checkpoint.WorkKey.Equals(lease.WorkKey) ||
                !string.Equals(checkpoint.LeaseId, lease.LeaseId, StringComparison.Ordinal) ||
                checkpoint.Attempt != lease.Attempt) return false;
            if (checkpoint.ProgressOrdinal < current.ProgressOrdinal) return false;
            if (checkpoint.ProgressOrdinal == current.ProgressOrdinal && checkpoint.CompletedUnits < current.CompletedUnits) return false;
            checkpoints[lease.WorkKey] = checkpoint;
            return true;
        }

        public bool Remove(WorldGenerationWorkKey key) => checkpoints.Remove(key);

        public void Clear() => checkpoints.Clear();

        public void CollectOwnedActive(
            string ownerId,
            WorldGenerationExecutionLeaseCoordinator coordinator,
            List<WorldGenerationExecutionLease> output)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner ID must not be empty.", nameof(ownerId));
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));
            if (output == null) throw new ArgumentNullException(nameof(output));
            foreach (KeyValuePair<WorldGenerationWorkKey, WorldGenerationExecutionCheckpoint> pair in checkpoints)
            {
                if (!coordinator.TryGet(pair.Key, out WorldGenerationExecutionLease lease)) continue;
                if (!string.Equals(lease.OwnerId, ownerId, StringComparison.Ordinal)) continue;
                output.Add(lease);
            }
            output.Sort(CompareLeases);
        }

        private static int CompareLeases(WorldGenerationExecutionLease a, WorldGenerationExecutionLease b)
        {
            int c = a.WorkKey.Chunk.X.CompareTo(b.WorkKey.Chunk.X);
            if (c != 0) return c;
            c = a.WorkKey.Chunk.Y.CompareTo(b.WorkKey.Chunk.Y);
            if (c != 0) return c;
            c = a.WorkKey.Kind.CompareTo(b.WorkKey.Kind);
            if (c != 0) return c;
            c = string.CompareOrdinal(a.OwnerId, b.OwnerId);
            if (c != 0) return c;
            return a.Attempt.CompareTo(b.Attempt);
        }
    }

    public sealed class WorldGenerationResumableExecutionCoordinator
    {
        private readonly WorldGenerationExecutionLeaseCoordinator leases;
        private readonly WorldGenerationExecutionCheckpointStore checkpoints;

        public WorldGenerationResumableExecutionCoordinator(
            WorldGenerationExecutionLeaseCoordinator leases,
            WorldGenerationExecutionCheckpointStore checkpoints)
        {
            this.leases = leases ?? throw new ArgumentNullException(nameof(leases));
            this.checkpoints = checkpoints ?? throw new ArgumentNullException(nameof(checkpoints));
        }

        public WorldGenerationExecutionLeaseSchedule Claim(
            int maxItems,
            string ownerId,
            long currentTick,
            long leaseDurationTicks)
        {
            WorldGenerationExecutionLeaseSchedule schedule = leases.Claim(maxItems, ownerId, currentTick, leaseDurationTicks);
            if (!schedule.Succeeded) return schedule;
            for (int i = 0; i < schedule.Count; i++) checkpoints.Prepare(schedule.Leases[i]);
            return schedule;
        }

        public int RecoverExpired(long currentTick) => leases.RecoverExpired(currentTick);

        public bool Renew(WorldGenerationExecutionLease lease, long currentTick, long leaseDurationTicks) =>
            leases.Renew(lease, currentTick, leaseDurationTicks);

        public bool TryGetCheckpoint(WorldGenerationWorkKey key, out WorldGenerationExecutionCheckpoint checkpoint) =>
            checkpoints.TryGet(key, out checkpoint);

        public bool SaveCheckpoint(
            WorldGenerationExecutionLease lease,
            WorldGenerationExecutionCheckpoint checkpoint,
            long currentTick) =>
            checkpoints.Save(lease, checkpoint, leases, currentTick);

        public bool Complete(WorldGenerationExecutionLease lease)
        {
            bool completed = leases.Complete(lease);
            if (completed) checkpoints.Remove(lease.WorkKey);
            return completed;
        }

        public bool Fail(WorldGenerationExecutionLease lease) => leases.Fail(lease);

        public void CollectOwnedActive(
            string ownerId,
            List<WorldGenerationExecutionLease> output) =>
            checkpoints.CollectOwnedActive(ownerId, leases, output);
    }

    public interface IWorldGenerationResumableWorkExecutor
    {
        WorldGenerationExecutionStep Execute(
            WorldGenerationWorkItem item,
            WorldGenerationExecutionLease lease,
            WorldGenerationExecutionCheckpoint checkpoint);
    }

    public sealed class WorldGenerationResumableExecutionRunner
    {
        private readonly WorldGenerationResumableExecutionCoordinator coordinator;
        private readonly IWorldGenerationResumableWorkExecutor executor;

        public WorldGenerationResumableExecutionRunner(
            WorldGenerationResumableExecutionCoordinator coordinator,
            IWorldGenerationResumableWorkExecutor executor)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public int Run(
            int maxSteps,
            string ownerId,
            long currentTick,
            long leaseDurationTicks)
        {
            if (maxSteps < 0) throw new ArgumentOutOfRangeException(nameof(maxSteps));
            if (maxSteps == 0) return 0;

            coordinator.RecoverExpired(currentTick);
            var active = new List<WorldGenerationExecutionLease>();
            coordinator.CollectOwnedActive(ownerId, active);

            int executed = 0;
            for (int i = 0; i < active.Count && executed < maxSteps; i++)
            {
                ExecuteOne(active[i], currentTick);
                executed++;
            }

            if (executed >= maxSteps) return executed;

            WorldGenerationExecutionLeaseSchedule schedule = coordinator.Claim(
                maxSteps - executed,
                ownerId,
                currentTick,
                leaseDurationTicks);
            if (!schedule.Succeeded) throw new InvalidOperationException(schedule.Issues[0].Message);

            for (int i = 0; i < schedule.Count && executed < maxSteps; i++)
            {
                ExecuteOne(schedule.Leases[i], currentTick);
                executed++;
            }

            return executed;
        }

        private void ExecuteOne(WorldGenerationExecutionLease lease, long currentTick)
        {
            if (!coordinator.TryGetCheckpoint(lease.WorkKey, out WorldGenerationExecutionCheckpoint checkpoint))
                checkpoint = new WorldGenerationExecutionCheckpoint(lease.WorkKey, lease.LeaseId, lease.Attempt, "Start", 0, 0, 0, 0);

            WorldGenerationWorkItem item = new WorldGenerationWorkItem(lease.WorkKey.Chunk, lease.WorkKey.Kind);
            try
            {
                WorldGenerationExecutionStep step = executor.Execute(item, lease, checkpoint);
                if (!step.Checkpoint.WorkKey.Equals(lease.WorkKey) ||
                    !string.Equals(step.Checkpoint.LeaseId, lease.LeaseId, StringComparison.Ordinal) ||
                    step.Checkpoint.Attempt != lease.Attempt)
                    throw new InvalidOperationException("Resumable executor returned a checkpoint for a different execution lease.");

                if (step.Status == WorldGenerationExecutionStepStatus.Continue)
                {
                    if (!coordinator.SaveCheckpoint(lease, step.Checkpoint, currentTick))
                        throw new InvalidOperationException("Generation checkpoint could not be saved because the execution lease is no longer current.");
                    return;
                }

                if (step.Status == WorldGenerationExecutionStepStatus.Completed)
                {
                    if (!coordinator.SaveCheckpoint(lease, step.Checkpoint, currentTick) || !coordinator.Complete(lease))
                        throw new InvalidOperationException("Generation work could not be completed because its execution lease is no longer current.");
                    return;
                }

                coordinator.SaveCheckpoint(lease, step.Checkpoint, currentTick);
                coordinator.Fail(lease);
                throw new InvalidOperationException(
                    string.IsNullOrEmpty(step.FailureCode)
                        ? step.FailureMessage
                        : step.FailureCode + ": " + step.FailureMessage);
            }
            catch
            {
                coordinator.Fail(lease);
                throw;
            }
        }
    }
}
