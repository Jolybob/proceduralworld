using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>Ownership token for one running generation work item.</summary>
    public readonly struct WorldGenerationExecutionLease : IEquatable<WorldGenerationExecutionLease>
    {
        public WorldGenerationWorkKey WorkKey { get; }
        public string OwnerId { get; }
        public string LeaseId { get; }
        public int Attempt { get; }
        public long AcquiredAtTick { get; }
        public long ExpiresAtTick { get; }

        public WorldGenerationExecutionLease(
            WorldGenerationWorkKey workKey,
            string ownerId,
            string leaseId,
            int attempt,
            long acquiredAtTick,
            long expiresAtTick)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Lease owner ID must not be empty.", nameof(ownerId));
            if (string.IsNullOrWhiteSpace(leaseId)) throw new ArgumentException("Lease ID must not be empty.", nameof(leaseId));
            if (attempt <= 0) throw new ArgumentOutOfRangeException(nameof(attempt));
            if (expiresAtTick < acquiredAtTick) throw new ArgumentException("Lease expiry must not precede acquisition.", nameof(expiresAtTick));
            WorkKey = workKey;
            OwnerId = ownerId;
            LeaseId = leaseId;
            Attempt = attempt;
            AcquiredAtTick = acquiredAtTick;
            ExpiresAtTick = expiresAtTick;
        }

        public bool IsExpired(long currentTick) => currentTick >= ExpiresAtTick;

        public bool Equals(WorldGenerationExecutionLease other) =>
            WorkKey.Equals(other.WorkKey) &&
            string.Equals(OwnerId, other.OwnerId, StringComparison.Ordinal) &&
            string.Equals(LeaseId, other.LeaseId, StringComparison.Ordinal) &&
            Attempt == other.Attempt &&
            AcquiredAtTick == other.AcquiredAtTick &&
            ExpiresAtTick == other.ExpiresAtTick;

        public override bool Equals(object obj) => obj is WorldGenerationExecutionLease other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(WorkKey, OwnerId, LeaseId, Attempt, AcquiredAtTick, ExpiresAtTick);
    }

    public sealed class WorldGenerationExecutionLeaseIssue
    {
        public string Code { get; }
        public string Message { get; }
        public WorldGenerationWorkKey Key { get; }

        public WorldGenerationExecutionLeaseIssue(string code, string message, WorldGenerationWorkKey key)
        {
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Key = key;
        }
    }

    /// <summary>Dependency-ready work paired with the execution leases that own it.</summary>
    public sealed class WorldGenerationExecutionLeaseSchedule
    {
        private readonly List<WorldGenerationWorkItem> items;
        private readonly List<WorldGenerationExecutionLease> leases;

        public IReadOnlyList<WorldGenerationWorkItem> Items => items;
        public IReadOnlyList<WorldGenerationExecutionLease> Leases => leases;
        public IReadOnlyList<WorldGenerationExecutionLeaseIssue> Issues { get; }
        public bool Succeeded => Issues.Count == 0;
        public int Count => items.Count;

        public WorldGenerationExecutionLeaseSchedule(
            IEnumerable<WorldGenerationWorkItem> items,
            IEnumerable<WorldGenerationExecutionLease> leases,
            IEnumerable<WorldGenerationExecutionLeaseIssue> issues = null)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (leases == null) throw new ArgumentNullException(nameof(leases));
            this.items = new List<WorldGenerationWorkItem>(items);
            this.leases = new List<WorldGenerationExecutionLease>(leases);
            if (this.items.Count != this.leases.Count) throw new ArgumentException("Every leased work item must have exactly one lease.");
            Issues = new List<WorldGenerationExecutionLeaseIssue>(issues ?? new WorldGenerationExecutionLeaseIssue[0]);
        }
    }

    /// <summary>Coordinates worker ownership around the dependency graph's execution state.</summary>
    public sealed class WorldGenerationExecutionLeaseCoordinator
    {
        private readonly WorldGenerationWorkGraph graph;

        public WorldGenerationExecutionLeaseCoordinator(WorldGenerationWorkGraph graph)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public WorldGenerationExecutionLeaseSchedule Claim(int maxItems, string ownerId, long currentTick, long leaseDurationTicks) =>
            graph.Claim(maxItems, ownerId, currentTick, leaseDurationTicks);

        public bool Renew(WorldGenerationExecutionLease lease, long currentTick, long leaseDurationTicks) =>
            graph.Renew(lease, currentTick, leaseDurationTicks);

        public bool Complete(WorldGenerationExecutionLease lease) => graph.Complete(lease);

        public bool Fail(WorldGenerationExecutionLease lease) => graph.Fail(lease);

        public int RecoverExpired(long currentTick) => graph.RecoverExpired(currentTick);

        public bool TryGet(WorldGenerationWorkKey key, out WorldGenerationExecutionLease lease) => graph.TryGetLease(key, out lease);
    }

    /// <summary>Executor contract that receives an ownership token valid only for the current attempt.</summary>
    public interface IWorldGenerationLeasedWorkExecutor
    {
        void Execute(WorldGenerationWorkItem item, WorldGenerationExecutionLease lease);
    }

    /// <summary>Runs leased generation work and commits completion/failure through the owning lease.</summary>
    public sealed class WorldGenerationExecutionLeaseRunner
    {
        private readonly WorldGenerationExecutionLeaseCoordinator coordinator;
        private readonly IWorldGenerationLeasedWorkExecutor executor;

        public WorldGenerationExecutionLeaseRunner(
            WorldGenerationExecutionLeaseCoordinator coordinator,
            IWorldGenerationLeasedWorkExecutor executor)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public int Run(int maxItems, string ownerId, long currentTick, long leaseDurationTicks)
        {
            WorldGenerationExecutionLeaseSchedule schedule = coordinator.Claim(maxItems, ownerId, currentTick, leaseDurationTicks);
            if (!schedule.Succeeded) throw new InvalidOperationException(schedule.Issues[0].Message);

            int completed = 0;
            for (int i = 0; i < schedule.Count; i++)
            {
                WorldGenerationWorkItem item = schedule.Items[i];
                WorldGenerationExecutionLease lease = schedule.Leases[i];
                try
                {
                    executor.Execute(item, lease);
                    if (!coordinator.Complete(lease))
                        throw new InvalidOperationException("Generation work could not be completed because its execution lease is no longer current.");
                    completed++;
                }
                catch
                {
                    coordinator.Fail(lease);
                    throw;
                }
            }

            return completed;
        }
    }

    internal static class WorldGenerationExecutionLeaseId
    {
        public static string Create(WorldGenerationWorkKey key, string ownerId, int attempt)
        {
            return ownerId + ":" + ((int)key.Kind).ToString() + ":" + key.Chunk.X.ToString() + ":" + key.Chunk.Y.ToString() + ":" + attempt.ToString();
        }
    }
}
