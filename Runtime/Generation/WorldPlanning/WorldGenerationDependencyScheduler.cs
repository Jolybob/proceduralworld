using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public enum WorldGenerationWorkStatus { Pending = 0, Running = 1, Completed = 2, Failed = 3 }

    public readonly struct WorldGenerationWorkKey : IEquatable<WorldGenerationWorkKey>
    {
        public ChunkCoord Chunk { get; }
        public WorldGenerationWorkKind Kind { get; }
        public WorldGenerationWorkKey(ChunkCoord chunk, WorldGenerationWorkKind kind) { Chunk = chunk; Kind = kind; }
        public bool Equals(WorldGenerationWorkKey other) => Chunk.Equals(other.Chunk) && Kind == other.Kind;
        public override bool Equals(object obj) => obj is WorldGenerationWorkKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Chunk, Kind);
    }

    public readonly struct WorldGenerationDependency : IEquatable<WorldGenerationDependency>
    {
        public WorldGenerationWorkKey Prerequisite { get; }
        public WorldGenerationWorkKey Dependent { get; }
        public WorldGenerationDependency(WorldGenerationWorkKey prerequisite, WorldGenerationWorkKey dependent)
        {
            if (prerequisite.Equals(dependent)) throw new ArgumentException("A work item cannot depend on itself.");
            Prerequisite = prerequisite; Dependent = dependent;
        }
        public bool Equals(WorldGenerationDependency other) => Prerequisite.Equals(other.Prerequisite) && Dependent.Equals(other.Dependent);
        public override bool Equals(object obj) => obj is WorldGenerationDependency other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Prerequisite, Dependent);
    }

    public sealed class WorldGenerationDependencyIssue
    {
        public string Code { get; }
        public string Message { get; }
        public WorldGenerationWorkKey Key { get; }
        public WorldGenerationDependencyIssue(string code, string message, WorldGenerationWorkKey key) { Code = code; Message = message; Key = key; }
    }

    public sealed class WorldGenerationDependencySchedule
    {
        private readonly List<WorldGenerationWorkItem> items;
        public IReadOnlyList<WorldGenerationWorkItem> Items => items;
        public IReadOnlyList<WorldGenerationDependencyIssue> Issues { get; }
        public bool Succeeded => Issues.Count == 0;
        public int Count => items.Count;
        public WorldGenerationDependencySchedule(IEnumerable<WorldGenerationWorkItem> items, IEnumerable<WorldGenerationDependencyIssue> issues = null)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            this.items = new List<WorldGenerationWorkItem>(items);
            Issues = new List<WorldGenerationDependencyIssue>(issues ?? new WorldGenerationDependencyIssue[0]);
        }
    }

    public sealed class WorldGenerationWorkGraph
    {
        private readonly Dictionary<WorldGenerationWorkKey, WorldGenerationWorkItem> items = new Dictionary<WorldGenerationWorkKey, WorldGenerationWorkItem>();
        private readonly Dictionary<WorldGenerationWorkKey, WorldGenerationWorkStatus> statuses = new Dictionary<WorldGenerationWorkKey, WorldGenerationWorkStatus>();
        private readonly Dictionary<WorldGenerationWorkKey, WorldGenerationExecutionLease> leases = new Dictionary<WorldGenerationWorkKey, WorldGenerationExecutionLease>();
        private readonly Dictionary<WorldGenerationWorkKey, int> attempts = new Dictionary<WorldGenerationWorkKey, int>();
        private readonly HashSet<WorldGenerationDependency> dependencies = new HashSet<WorldGenerationDependency>();

        public int Count => items.Count;
        public int DependencyCount => dependencies.Count;
        public int PendingCount => CountStatus(WorldGenerationWorkStatus.Pending);
        public int RunningCount => CountStatus(WorldGenerationWorkStatus.Running);
        public int CompletedCount => CountStatus(WorldGenerationWorkStatus.Completed);
        public int FailedCount => CountStatus(WorldGenerationWorkStatus.Failed);

        public void Request(ChunkCoord chunk, WorldGenerationWorkKind kind, int priority = 0)
        {
            var key = new WorldGenerationWorkKey(chunk, kind);
            if (!items.TryGetValue(key, out WorldGenerationWorkItem existing))
            {
                items[key] = new WorldGenerationWorkItem(chunk, kind, priority);
                statuses[key] = WorldGenerationWorkStatus.Pending;
            }
            else if (priority > existing.Priority && statuses[key] != WorldGenerationWorkStatus.Running)
            {
                items[key] = new WorldGenerationWorkItem(chunk, kind, priority);
                if (statuses[key] == WorldGenerationWorkStatus.Completed || statuses[key] == WorldGenerationWorkStatus.Failed)
                    statuses[key] = WorldGenerationWorkStatus.Pending;
            }
        }

        public bool Contains(WorldGenerationWorkKey key) => items.ContainsKey(key);
        public WorldGenerationWorkStatus GetStatus(WorldGenerationWorkKey key)
        {
            if (!statuses.TryGetValue(key, out WorldGenerationWorkStatus status)) throw new KeyNotFoundException("Generation work key was not found.");
            return status;
        }

        public bool TryGetLease(WorldGenerationWorkKey key, out WorldGenerationExecutionLease lease) => leases.TryGetValue(key, out lease);

        public void AddDependency(WorldGenerationWorkKey prerequisite, WorldGenerationWorkKey dependent)
        {
            if (prerequisite.Equals(dependent)) throw new ArgumentException("A work item cannot depend on itself.");
            RequestIfMissing(prerequisite); RequestIfMissing(dependent); dependencies.Add(new WorldGenerationDependency(prerequisite, dependent));
        }

        public bool Remove(WorldGenerationWorkKey key)
        {
            if (!items.Remove(key)) return false;
            statuses.Remove(key);
            leases.Remove(key);
            dependencies.RemoveWhere(d => d.Prerequisite.Equals(key) || d.Dependent.Equals(key));
            return true;
        }

        public void Clear() { items.Clear(); statuses.Clear(); leases.Clear(); attempts.Clear(); dependencies.Clear(); }

        public bool Complete(WorldGenerationWorkKey key)
        {
            if (leases.ContainsKey(key)) return false;
            if (!statuses.TryGetValue(key, out WorldGenerationWorkStatus status) || status != WorldGenerationWorkStatus.Running) return false;
            statuses[key] = WorldGenerationWorkStatus.Completed; return true;
        }

        public bool Fail(WorldGenerationWorkKey key)
        {
            if (leases.ContainsKey(key)) return false;
            if (!statuses.TryGetValue(key, out WorldGenerationWorkStatus status) || status != WorldGenerationWorkStatus.Running) return false;
            statuses[key] = WorldGenerationWorkStatus.Failed; return true;
        }

        public bool Complete(WorldGenerationExecutionLease lease)
        {
            if (!TryTakeOwnership(lease)) return false;
            leases.Remove(lease.WorkKey);
            statuses[lease.WorkKey] = WorldGenerationWorkStatus.Completed;
            return true;
        }

        public bool Fail(WorldGenerationExecutionLease lease)
        {
            if (!TryTakeOwnership(lease)) return false;
            leases.Remove(lease.WorkKey);
            statuses[lease.WorkKey] = WorldGenerationWorkStatus.Failed;
            return true;
        }

        public bool Retry(WorldGenerationWorkKey key)
        {
            if (!statuses.TryGetValue(key, out WorldGenerationWorkStatus status) || status != WorldGenerationWorkStatus.Failed) return false;
            statuses[key] = WorldGenerationWorkStatus.Pending; return true;
        }

        public WorldGenerationExecutionLeaseSchedule Claim(int maxItems, string ownerId, long currentTick, long leaseDurationTicks)
        {
            if (maxItems < 0) throw new ArgumentOutOfRangeException(nameof(maxItems));
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Lease owner ID must not be empty.", nameof(ownerId));
            if (leaseDurationTicks <= 0) throw new ArgumentOutOfRangeException(nameof(leaseDurationTicks));

            RecoverExpired(currentTick);
            WorldGenerationDependencySchedule schedule = BuildSchedule(maxItems);
            if (!schedule.Succeeded)
            {
                var issues = new List<WorldGenerationExecutionLeaseIssue>();
                for (int i = 0; i < schedule.Issues.Count; i++)
                    issues.Add(new WorldGenerationExecutionLeaseIssue(schedule.Issues[i].Code, schedule.Issues[i].Message, schedule.Issues[i].Key));
                return new WorldGenerationExecutionLeaseSchedule(
                    new WorldGenerationWorkItem[0],
                    new WorldGenerationExecutionLease[0],
                    issues);
            }

            var claimedItems = new List<WorldGenerationWorkItem>(schedule.Count);
            var claimedLeases = new List<WorldGenerationExecutionLease>(schedule.Count);
            for (int i = 0; i < schedule.Count; i++)
            {
                WorldGenerationWorkItem item = schedule.Items[i];
                WorldGenerationWorkKey key = new WorldGenerationWorkKey(item.Chunk, item.Kind);
                int attempt = attempts.TryGetValue(key, out int previousAttempt) ? checked(previousAttempt + 1) : 1;
                long expiresAtTick = checked(currentTick + leaseDurationTicks);
                var lease = new WorldGenerationExecutionLease(
                    key,
                    ownerId,
                    WorldGenerationExecutionLeaseId.Create(key, ownerId, attempt),
                    attempt,
                    currentTick,
                    expiresAtTick);
                attempts[key] = attempt;
                leases[key] = lease;
                statuses[key] = WorldGenerationWorkStatus.Running;
                claimedItems.Add(item);
                claimedLeases.Add(lease);
            }

            return new WorldGenerationExecutionLeaseSchedule(claimedItems, claimedLeases);
        }

        public bool Renew(WorldGenerationExecutionLease lease, long currentTick, long leaseDurationTicks)
        {
            if (leaseDurationTicks <= 0) return false;
            if (!leases.TryGetValue(lease.WorkKey, out WorldGenerationExecutionLease active) || !active.Equals(lease)) return false;
            if (statuses[lease.WorkKey] != WorldGenerationWorkStatus.Running || active.IsExpired(currentTick)) return false;

            long expiresAtTick;
            try { expiresAtTick = checked(currentTick + leaseDurationTicks); }
            catch (OverflowException) { return false; }

            leases[lease.WorkKey] = new WorldGenerationExecutionLease(
                active.WorkKey,
                active.OwnerId,
                active.LeaseId,
                active.Attempt,
                active.AcquiredAtTick,
                expiresAtTick);
            return true;
        }

        public int RecoverExpired(long currentTick)
        {
            var expired = new List<WorldGenerationExecutionLease>();
            foreach (KeyValuePair<WorldGenerationWorkKey, WorldGenerationExecutionLease> pair in leases)
                if (pair.Value.IsExpired(currentTick)) expired.Add(pair.Value);
            expired.Sort(CompareLeases);

            int recovered = 0;
            for (int i = 0; i < expired.Count; i++)
            {
                WorldGenerationExecutionLease lease = expired[i];
                if (!leases.TryGetValue(lease.WorkKey, out WorldGenerationExecutionLease active) || !active.Equals(lease)) continue;
                if (statuses[lease.WorkKey] != WorldGenerationWorkStatus.Running) continue;
                leases.Remove(lease.WorkKey);
                statuses[lease.WorkKey] = WorldGenerationWorkStatus.Pending;
                recovered++;
            }
            return recovered;
        }

        public WorldGenerationDependencySchedule BuildSchedule(int maxItems = int.MaxValue)
        {
            if (maxItems < 0) throw new ArgumentOutOfRangeException(nameof(maxItems));
            var issues = new List<WorldGenerationDependencyIssue>();
            var pending = new List<WorldGenerationWorkKey>();
            foreach (KeyValuePair<WorldGenerationWorkKey, WorldGenerationWorkItem> pair in items)
                if (statuses[pair.Key] == WorldGenerationWorkStatus.Pending) pending.Add(pair.Key);

            var unresolved = new Dictionary<WorldGenerationWorkKey, int>();
            foreach (WorldGenerationWorkKey key in pending) unresolved[key] = 0;
            foreach (WorldGenerationDependency dependency in dependencies)
            {
                if (!items.ContainsKey(dependency.Prerequisite) || !items.ContainsKey(dependency.Dependent))
                {
                    issues.Add(new WorldGenerationDependencyIssue("MissingDependencyNode", "A dependency references a work item that is not scheduled.", dependency.Dependent));
                    continue;
                }
                if (!unresolved.ContainsKey(dependency.Dependent)) continue;
                WorldGenerationWorkStatus status = statuses[dependency.Prerequisite];
                if (status == WorldGenerationWorkStatus.Failed)
                    issues.Add(new WorldGenerationDependencyIssue("FailedDependency", "A pending work item depends on failed generation work.", dependency.Dependent));
                else if (status != WorldGenerationWorkStatus.Completed) unresolved[dependency.Dependent]++;
            }

            var ready = new List<WorldGenerationWorkKey>();
            foreach (KeyValuePair<WorldGenerationWorkKey, int> pair in unresolved) if (pair.Value == 0) ready.Add(pair.Key);
            ready.Sort(CompareReady);
            var result = new List<WorldGenerationWorkItem>();
            while (ready.Count > 0 && result.Count < maxItems)
            {
                WorldGenerationWorkKey key = ready[0]; ready.RemoveAt(0); result.Add(items[key]);
                foreach (WorldGenerationDependency dependency in dependencies)
                {
                    if (!dependency.Prerequisite.Equals(key) || !unresolved.ContainsKey(dependency.Dependent)) continue;
                    unresolved[dependency.Dependent]--;
                    if (unresolved[dependency.Dependent] == 0) { ready.Add(dependency.Dependent); ready.Sort(CompareReady); }
                }
            }

            if (result.Count < pending.Count && maxItems >= pending.Count)
            {
                WorldGenerationWorkKey cycle;
                if (TryFindPendingCycle(pending, out cycle))
                    issues.Add(new WorldGenerationDependencyIssue("DependencyCycle", "The generation dependency graph contains a cycle, so no complete schedule exists.", cycle));
            }
            return new WorldGenerationDependencySchedule(result, issues);
        }

        public WorldGenerationDependencySchedule Dequeue(int maxItems)
        {
            if (maxItems < 0) throw new ArgumentOutOfRangeException(nameof(maxItems));
            WorldGenerationDependencySchedule schedule = BuildSchedule(maxItems);
            if (!schedule.Succeeded) return schedule;
            for (int i = 0; i < schedule.Count; i++) statuses[new WorldGenerationWorkKey(schedule.Items[i].Chunk, schedule.Items[i].Kind)] = WorldGenerationWorkStatus.Running;
            return schedule;
        }

        private void RequestIfMissing(WorldGenerationWorkKey key)
        {
            if (!items.ContainsKey(key)) { items.Add(key, new WorldGenerationWorkItem(key.Chunk, key.Kind)); statuses.Add(key, WorldGenerationWorkStatus.Pending); }
        }

        private bool TryTakeOwnership(WorldGenerationExecutionLease lease)
        {
            if (!statuses.TryGetValue(lease.WorkKey, out WorldGenerationWorkStatus status) || status != WorldGenerationWorkStatus.Running) return false;
            return leases.TryGetValue(lease.WorkKey, out WorldGenerationExecutionLease active) && active.Equals(lease);
        }

        private int CountStatus(WorldGenerationWorkStatus status)
        {
            int count = 0; foreach (WorldGenerationWorkStatus value in statuses.Values) if (value == status) count++; return count;
        }

        private int CompareReady(WorldGenerationWorkKey a, WorldGenerationWorkKey b)
        {
            int c = items[b].Priority.CompareTo(items[a].Priority); if (c != 0) return c;
            c = a.Kind.CompareTo(b.Kind); if (c != 0) return c;
            c = a.Chunk.X.CompareTo(b.Chunk.X); if (c != 0) return c;
            return a.Chunk.Y.CompareTo(b.Chunk.Y);
        }

        private int CompareLeases(WorldGenerationExecutionLease a, WorldGenerationExecutionLease b)
        {
            int c = a.WorkKey.Chunk.X.CompareTo(b.WorkKey.Chunk.X); if (c != 0) return c;
            c = a.WorkKey.Chunk.Y.CompareTo(b.WorkKey.Chunk.Y); if (c != 0) return c;
            c = a.WorkKey.Kind.CompareTo(b.WorkKey.Kind); if (c != 0) return c;
            c = string.CompareOrdinal(a.OwnerId, b.OwnerId); if (c != 0) return c;
            return a.Attempt.CompareTo(b.Attempt);
        }

        private bool TryFindPendingCycle(List<WorldGenerationWorkKey> pending, out WorldGenerationWorkKey cycleKey)
        {
            var pendingSet = new HashSet<WorldGenerationWorkKey>(pending);
            var state = new Dictionary<WorldGenerationWorkKey, int>();
            cycleKey = default(WorldGenerationWorkKey);
            pending.Sort(CompareReady);
            for (int i = 0; i < pending.Count; i++)
            {
                WorldGenerationWorkKey current = pending[i];
                if (state.ContainsKey(current)) continue;
                if (VisitForCycle(current, pendingSet, state, out cycleKey)) return true;
            }
            return false;
        }

        private bool VisitForCycle(WorldGenerationWorkKey key, HashSet<WorldGenerationWorkKey> pending, Dictionary<WorldGenerationWorkKey, int> state, out WorldGenerationWorkKey cycleKey)
        {
            cycleKey = default(WorldGenerationWorkKey);
            state[key] = 1;
            var next = new List<WorldGenerationWorkKey>();
            foreach (WorldGenerationDependency dependency in dependencies)
                if (dependency.Prerequisite.Equals(key) && pending.Contains(dependency.Dependent)) next.Add(dependency.Dependent);
            next.Sort(CompareReady);
            for (int i = 0; i < next.Count; i++)
            {
                WorldGenerationWorkKey dependent = next[i];
                if (!state.TryGetValue(dependent, out int dependentState))
                {
                    if (VisitForCycle(dependent, pending, state, out cycleKey)) return true;
                }
                else if (dependentState == 1) { cycleKey = dependent; return true; }
            }
            state[key] = 2;
            return false;
        }
    }

    public sealed class WorldGenerationDependencySchedulerRunner
    {
        private readonly WorldGenerationWorkGraph graph;
        private readonly IWorldGenerationWorkExecutor executor;
        public WorldGenerationDependencySchedulerRunner(WorldGenerationWorkGraph graph, IWorldGenerationWorkExecutor executor)
        { this.graph = graph ?? throw new ArgumentNullException(nameof(graph)); this.executor = executor ?? throw new ArgumentNullException(nameof(executor)); }
        public int Run(int maxItems)
        {
            WorldGenerationDependencySchedule schedule = graph.Dequeue(maxItems);
            if (!schedule.Succeeded) throw new InvalidOperationException(schedule.Issues[0].Message);
            for (int i = 0; i < schedule.Count; i++)
            {
                WorldGenerationWorkKey key = new WorldGenerationWorkKey(schedule.Items[i].Chunk, schedule.Items[i].Kind);
                try { executor.Execute(schedule.Items[i]); graph.Complete(key); }
                catch { graph.Fail(key); throw; }
            }
            return schedule.Count;
        }
    }
}
