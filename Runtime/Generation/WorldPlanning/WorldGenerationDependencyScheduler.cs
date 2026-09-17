using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public readonly struct WorldGenerationWorkKey : IEquatable<WorldGenerationWorkKey>
    {
        public ChunkCoord Chunk { get; }
        public WorldGenerationWorkKind Kind { get; }

        public WorldGenerationWorkKey(ChunkCoord chunk, WorldGenerationWorkKind kind)
        {
            Chunk = chunk;
            Kind = kind;
        }

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
            Prerequisite = prerequisite;
            Dependent = dependent;
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

        public WorldGenerationDependencyIssue(string code, string message, WorldGenerationWorkKey key)
        {
            Code = code;
            Message = message;
            Key = key;
        }
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
        private readonly HashSet<WorldGenerationDependency> dependencies = new HashSet<WorldGenerationDependency>();

        public int Count => items.Count;
        public int DependencyCount => dependencies.Count;

        public void Request(ChunkCoord chunk, WorldGenerationWorkKind kind, int priority = 0)
        {
            var key = new WorldGenerationWorkKey(chunk, kind);
            if (!items.TryGetValue(key, out WorldGenerationWorkItem existing) || priority > existing.Priority)
                items[key] = new WorldGenerationWorkItem(chunk, kind, priority);
        }

        public bool Contains(WorldGenerationWorkKey key) => items.ContainsKey(key);

        public void AddDependency(WorldGenerationWorkKey prerequisite, WorldGenerationWorkKey dependent)
        {
            if (prerequisite.Equals(dependent)) throw new ArgumentException("A work item cannot depend on itself.");
            RequestIfMissing(prerequisite);
            RequestIfMissing(dependent);
            dependencies.Add(new WorldGenerationDependency(prerequisite, dependent));
        }

        public bool Remove(WorldGenerationWorkKey key)
        {
            if (!items.Remove(key)) return false;
            dependencies.RemoveWhere(d => d.Prerequisite.Equals(key) || d.Dependent.Equals(key));
            return true;
        }

        public void Clear()
        {
            items.Clear();
            dependencies.Clear();
        }

        public WorldGenerationDependencySchedule BuildSchedule(int maxItems = int.MaxValue)
        {
            if (maxItems < 0) throw new ArgumentOutOfRangeException(nameof(maxItems));
            var issues = new List<WorldGenerationDependencyIssue>();
            var indegree = new Dictionary<WorldGenerationWorkKey, int>();
            var outgoing = new Dictionary<WorldGenerationWorkKey, List<WorldGenerationWorkKey>>();
            foreach (WorldGenerationWorkKey key in items.Keys)
            {
                indegree[key] = 0;
                outgoing[key] = new List<WorldGenerationWorkKey>();
            }

            foreach (WorldGenerationDependency dependency in dependencies)
            {
                if (!items.ContainsKey(dependency.Prerequisite) || !items.ContainsKey(dependency.Dependent))
                {
                    issues.Add(new WorldGenerationDependencyIssue("MissingDependencyNode", "A dependency references a work item that is not scheduled.", dependency.Dependent));
                    continue;
                }
                outgoing[dependency.Prerequisite].Add(dependency.Dependent);
                indegree[dependency.Dependent]++;
            }

            var ready = new List<WorldGenerationWorkKey>();
            foreach (KeyValuePair<WorldGenerationWorkKey, int> pair in indegree)
                if (pair.Value == 0) ready.Add(pair.Key);
            ready.Sort(CompareReady);

            var result = new List<WorldGenerationWorkItem>();
            while (ready.Count > 0 && result.Count < maxItems)
            {
                WorldGenerationWorkKey key = ready[0];
                ready.RemoveAt(0);
                result.Add(items[key]);

                List<WorldGenerationWorkKey> next = outgoing[key];
                next.Sort(CompareReady);
                for (int i = 0; i < next.Count; i++)
                {
                    WorldGenerationWorkKey dependent = next[i];
                    indegree[dependent]--;
                    if (indegree[dependent] == 0)
                    {
                        ready.Add(dependent);
                        ready.Sort(CompareReady);
                    }
                }
            }

            if (result.Count < items.Count && maxItems >= items.Count)
                issues.Add(new WorldGenerationDependencyIssue("DependencyCycle", "The generation dependency graph contains a cycle, so no complete schedule exists.", FindCycleKey(indegree)));

            return new WorldGenerationDependencySchedule(result, issues);
        }

        public WorldGenerationDependencySchedule Dequeue(int maxItems)
        {
            if (maxItems < 0) throw new ArgumentOutOfRangeException(nameof(maxItems));
            WorldGenerationDependencySchedule schedule = BuildSchedule(maxItems);
            if (!schedule.Succeeded) return schedule;
            for (int i = 0; i < schedule.Count; i++) Remove(new WorldGenerationWorkKey(schedule.Items[i].Chunk, schedule.Items[i].Kind));
            return schedule;
        }

        private void RequestIfMissing(WorldGenerationWorkKey key)
        {
            if (!items.ContainsKey(key)) items.Add(key, new WorldGenerationWorkItem(key.Chunk, key.Kind));
        }

        private static int CompareReady(WorldGenerationWorkKey a, WorldGenerationWorkKey b)
        {
            int c = a.Kind.CompareTo(b.Kind);
            if (c != 0) return c;
            c = a.Chunk.X.CompareTo(b.Chunk.X);
            if (c != 0) return c;
            return a.Chunk.Y.CompareTo(b.Chunk.Y);
        }

        private static WorldGenerationWorkKey FindCycleKey(Dictionary<WorldGenerationWorkKey, int> indegree)
        {
            WorldGenerationWorkKey found = default(WorldGenerationWorkKey);
            bool hasFound = false;
            foreach (KeyValuePair<WorldGenerationWorkKey, int> pair in indegree)
            {
                if (pair.Value <= 0) continue;
                if (!hasFound || CompareReady(pair.Key, found) < 0)
                {
                    found = pair.Key;
                    hasFound = true;
                }
            }
            return found;
        }
    }

    public sealed class WorldGenerationDependencySchedulerRunner
    {
        private readonly WorldGenerationWorkGraph graph;
        private readonly IWorldGenerationWorkExecutor executor;

        public WorldGenerationDependencySchedulerRunner(WorldGenerationWorkGraph graph, IWorldGenerationWorkExecutor executor)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public int Run(int maxItems)
        {
            WorldGenerationDependencySchedule schedule = graph.Dequeue(maxItems);
            if (!schedule.Succeeded) throw new InvalidOperationException(schedule.Issues[0].Message);
            for (int i = 0; i < schedule.Count; i++) executor.Execute(schedule.Items[i]);
            return schedule.Count;
        }
    }
}
