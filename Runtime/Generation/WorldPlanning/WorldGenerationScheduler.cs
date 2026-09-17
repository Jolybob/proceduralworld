using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public enum WorldGenerationWorkKind
    {
        Plan = 0,
        Realization = 1,
        Materialization = 2
    }

    public readonly struct WorldGenerationWorkItem : IEquatable<WorldGenerationWorkItem>
    {
        public ChunkCoord Chunk { get; }
        public WorldGenerationWorkKind Kind { get; }
        public int Priority { get; }
        public long Sequence { get; }

        public WorldGenerationWorkItem(ChunkCoord chunk, WorldGenerationWorkKind kind, int priority = 0, long sequence = 0)
        {
            Chunk = chunk;
            Kind = kind;
            Priority = priority;
            Sequence = sequence;
        }

        public bool Equals(WorldGenerationWorkItem other) => Chunk.Equals(other.Chunk) && Kind == other.Kind && Priority == other.Priority;
        public override bool Equals(object obj) => obj is WorldGenerationWorkItem other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Chunk, Kind, Priority);
    }

    public sealed class WorldGenerationSchedule
    {
        private readonly List<WorldGenerationWorkItem> items;
        public IReadOnlyList<WorldGenerationWorkItem> Items => items;
        public int Count => items.Count;

        public WorldGenerationSchedule(IEnumerable<WorldGenerationWorkItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            this.items = new List<WorldGenerationWorkItem>(items);
            this.items.Sort(Compare);
        }

        private static int Compare(WorldGenerationWorkItem a, WorldGenerationWorkItem b)
        {
            int c = b.Priority.CompareTo(a.Priority);
            if (c != 0) return c;
            c = a.Kind.CompareTo(b.Kind);
            if (c != 0) return c;
            c = a.Chunk.X.CompareTo(b.Chunk.X);
            if (c != 0) return c;
            c = a.Chunk.Y.CompareTo(b.Chunk.Y);
            if (c != 0) return c;
            return a.Sequence.CompareTo(b.Sequence);
        }
    }

    public sealed class WorldGenerationScheduler
    {
        private readonly Dictionary<ChunkCoord, WorldGenerationWorkItem> pending = new Dictionary<ChunkCoord, WorldGenerationWorkItem>();
        private long nextSequence;

        public int PendingCount => pending.Count;

        public void Request(ChunkCoord chunk, WorldGenerationWorkKind kind, int priority = 0)
        {
            var request = new WorldGenerationWorkItem(chunk, kind, priority, nextSequence++);
            if (!pending.TryGetValue(chunk, out WorldGenerationWorkItem existing) || IsHigherPriority(request, existing))
                pending[chunk] = request;
        }

        public bool Cancel(ChunkCoord chunk) => pending.Remove(chunk);

        public WorldGenerationSchedule BuildSchedule(int maxItems = int.MaxValue)
        {
            if (maxItems < 0) throw new ArgumentOutOfRangeException(nameof(maxItems));
            var result = new List<WorldGenerationWorkItem>(pending.Values);
            result.Sort(CompareCanonical);
            if (result.Count > maxItems) result.RemoveRange(maxItems, result.Count - maxItems);
            return new WorldGenerationSchedule(result);
        }

        public WorldGenerationSchedule Dequeue(int maxItems)
        {
            if (maxItems < 0) throw new ArgumentOutOfRangeException(nameof(maxItems));
            WorldGenerationSchedule schedule = BuildSchedule(maxItems);
            for (int i = 0; i < schedule.Count; i++) pending.Remove(schedule.Items[i].Chunk);
            return schedule;
        }

        public void Clear() { pending.Clear(); }

        private static bool IsHigherPriority(WorldGenerationWorkItem a, WorldGenerationWorkItem b)
        {
            if (a.Priority != b.Priority) return a.Priority > b.Priority;
            return a.Kind < b.Kind;
        }

        private static int CompareCanonical(WorldGenerationWorkItem a, WorldGenerationWorkItem b)
        {
            int c = b.Priority.CompareTo(a.Priority);
            if (c != 0) return c;
            c = a.Kind.CompareTo(b.Kind);
            if (c != 0) return c;
            c = a.Chunk.X.CompareTo(b.Chunk.X);
            if (c != 0) return c;
            return a.Chunk.Y.CompareTo(b.Chunk.Y);
        }
    }

    public interface IWorldGenerationWorkExecutor
    {
        void Execute(WorldGenerationWorkItem item);
    }

    public sealed class WorldGenerationSchedulerRunner
    {
        private readonly WorldGenerationScheduler scheduler;
        private readonly IWorldGenerationWorkExecutor executor;

        public WorldGenerationSchedulerRunner(WorldGenerationScheduler scheduler, IWorldGenerationWorkExecutor executor)
        {
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public int Run(int maxItems)
        {
            WorldGenerationSchedule schedule = scheduler.Dequeue(maxItems);
            for (int i = 0; i < schedule.Count; i++) executor.Execute(schedule.Items[i]);
            return schedule.Count;
        }
    }
}
