using System;
using System.Collections.Generic;
using System.Globalization;

namespace Jolybob.ProceduralWorld
{
    /// <summary>Result produced by a transactional generation executor before world commit.</summary>
    public readonly struct WorldGenerationExecutionResult : IEquatable<WorldGenerationExecutionResult>
    {
        public WorldGenerationWorkKey WorkKey { get; }
        public WorldRealizationBatch Realization { get; }
        public string Fingerprint { get; }

        public WorldGenerationExecutionResult(WorldGenerationWorkKey workKey, WorldRealizationBatch realization)
        {
            WorkKey = workKey;
            Realization = realization ?? throw new ArgumentNullException(nameof(realization));
            Fingerprint = WorldGenerationExecutionFingerprint.Compute(workKey, realization);
        }

        public WorldGenerationExecutionResult(WorldGenerationWorkKey workKey, WorldRealizationBatch realization, string fingerprint)
        {
            WorkKey = workKey;
            Realization = realization ?? throw new ArgumentNullException(nameof(realization));
            Fingerprint = string.IsNullOrWhiteSpace(fingerprint) ? WorldGenerationExecutionFingerprint.Compute(workKey, realization) : fingerprint;
        }

        public bool Equals(WorldGenerationExecutionResult other) =>
            WorkKey.Equals(other.WorkKey) &&
            string.Equals(Fingerprint, other.Fingerprint, StringComparison.Ordinal) &&
            Realization.Count == other.Realization.Count;

        public override bool Equals(object obj) => obj is WorldGenerationExecutionResult other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(WorkKey, Fingerprint, Realization.Count);
    }

    /// <summary>A stable receipt proving that a generation result was committed to world state.</summary>
    public readonly struct WorldGenerationExecutionReceipt : IEquatable<WorldGenerationExecutionReceipt>
    {
        public WorldGenerationWorkKey WorkKey { get; }
        public string Fingerprint { get; }
        public int EditCount { get; }

        public WorldGenerationExecutionReceipt(WorldGenerationWorkKey workKey, string fingerprint, int editCount)
        {
            if (string.IsNullOrWhiteSpace(fingerprint)) throw new ArgumentException("Execution fingerprint must not be empty.", nameof(fingerprint));
            if (editCount < 0) throw new ArgumentOutOfRangeException(nameof(editCount));
            WorkKey = workKey;
            Fingerprint = fingerprint;
            EditCount = editCount;
        }

        public bool Equals(WorldGenerationExecutionReceipt other) =>
            WorkKey.Equals(other.WorkKey) &&
            string.Equals(Fingerprint, other.Fingerprint, StringComparison.Ordinal) &&
            EditCount == other.EditCount;

        public override bool Equals(object obj) => obj is WorldGenerationExecutionReceipt other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(WorkKey, Fingerprint, EditCount);
    }

    /// <summary>Persistent boundary for committed generation receipts.</summary>
    public interface IWorldGenerationExecutionReceiptStore
    {
        bool TryGet(WorldGenerationWorkKey workKey, out WorldGenerationExecutionReceipt receipt);
        bool TryRecord(WorldGenerationExecutionReceipt receipt);
    }

    /// <summary>In-memory receipt store for runtime use and tests; persistence adapters can implement the same contract.</summary>
    public sealed class InMemoryWorldGenerationExecutionReceiptStore : IWorldGenerationExecutionReceiptStore
    {
        private readonly Dictionary<WorldGenerationWorkKey, WorldGenerationExecutionReceipt> receipts = new Dictionary<WorldGenerationWorkKey, WorldGenerationExecutionReceipt>();

        public int Count => receipts.Count;

        public bool TryGet(WorldGenerationWorkKey workKey, out WorldGenerationExecutionReceipt receipt) => receipts.TryGetValue(workKey, out receipt);

        public bool TryRecord(WorldGenerationExecutionReceipt receipt)
        {
            if (!receipts.TryGetValue(receipt.WorkKey, out WorldGenerationExecutionReceipt existing))
            {
                receipts.Add(receipt.WorkKey, receipt);
                return true;
            }

            return existing.Equals(receipt);
        }

        public void Clear() => receipts.Clear();
    }

    public readonly struct WorldRealizationBatchCommitResult
    {
        public bool Succeeded { get; }
        public int AddedCount { get; }
        public int AlreadyPresentCount { get; }
        public bool HasConflict { get; }
        public WorldRealizationConflict Conflict { get; }

        public WorldRealizationBatchCommitResult(bool succeeded, int addedCount, int alreadyPresentCount, WorldRealizationConflict conflict, bool hasConflict)
        {
            Succeeded = succeeded;
            AddedCount = addedCount;
            AlreadyPresentCount = alreadyPresentCount;
            Conflict = conflict;
            HasConflict = hasConflict;
        }
    }

    /// <summary>
    /// Applies a realization batch atomically. Exact replays are treated as no-ops, while
    /// conflicting edits fail before any new edit is written.
    /// </summary>
    public sealed class WorldRealizationBatchCommitter
    {
        private readonly WorldRealizationMap map;

        public WorldRealizationBatchCommitter(WorldRealizationMap map)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public WorldRealizationBatchCommitResult TryCommit(WorldRealizationBatch batch)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));

            var toAdd = new List<WorldRealizationEdit>();
            var seenSlots = new HashSet<RealizationSlot>();
            int alreadyPresentCount = 0;

            for (int i = 0; i < batch.Count; i++)
            {
                WorldRealizationEdit edit = batch.Edits[i];
                if (map.TryGet(edit.Id, out WorldRealizationEdit existingById))
                {
                    if (!existingById.Equals(edit)) return Conflict(existingById, edit);
                    alreadyPresentCount++;
                    continue;
                }

                var slot = new RealizationSlot(edit.Position, edit.Kind);
                if (!seenSlots.Add(slot))
                {
                    for (int prior = 0; prior < toAdd.Count; prior++)
                        if (new RealizationSlot(toAdd[prior].Position, toAdd[prior].Kind).Equals(slot)) return Conflict(toAdd[prior], edit);
                }

                var existing = new List<WorldRealizationEdit>();
                map.Collect(edit.Position, edit.Kind, existing);
                if (existing.Count > 0) return Conflict(existing[0], edit);
                toAdd.Add(edit);
            }

            var added = new List<WorldRealizationEdit>();
            for (int i = 0; i < toAdd.Count; i++)
            {
                if (!map.TryAdd(toAdd[i], out WorldRealizationConflict conflict))
                {
                    for (int rollback = 0; rollback < added.Count; rollback++) map.Remove(added[rollback].Id);
                    return new WorldRealizationBatchCommitResult(false, 0, alreadyPresentCount, conflict, true);
                }
                added.Add(toAdd[i]);
            }

            return new WorldRealizationBatchCommitResult(true, added.Count, alreadyPresentCount, default(WorldRealizationConflict), false);
        }

        private static WorldRealizationBatchCommitResult Conflict(WorldRealizationEdit existing, WorldRealizationEdit requested) =>
            new WorldRealizationBatchCommitResult(false, 0, 0, new WorldRealizationConflict(existing, requested), true);

        private readonly struct RealizationSlot : IEquatable<RealizationSlot>
        {
            private readonly WorldPosition position;
            private readonly string kind;

            public RealizationSlot(WorldPosition position, string kind)
            {
                this.position = position;
                this.kind = kind ?? string.Empty;
            }

            public bool Equals(RealizationSlot other) => position.Equals(other.position) && string.Equals(kind, other.kind, StringComparison.Ordinal);
            public override bool Equals(object obj) => obj is RealizationSlot other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(position, kind);
        }
    }

    /// <summary>Executor contract that produces a deterministic world-space result for a claimed work item.</summary>
    public interface IWorldGenerationTransactionalWorkExecutor
    {
        WorldGenerationExecutionResult Execute(WorldGenerationWorkItem item);
    }

    /// <summary>
    /// Bridges dependency execution state to world realization. Work is completed only after the
    /// world batch is committed and its receipt is recorded.
    /// </summary>
    public sealed class WorldGenerationTransactionalRunner
    {
        private readonly WorldGenerationWorkGraph graph;
        private readonly IWorldGenerationTransactionalWorkExecutor executor;
        private readonly WorldRealizationBatchCommitter committer;
        private readonly IWorldGenerationExecutionReceiptStore receipts;

        public WorldGenerationTransactionalRunner(
            WorldGenerationWorkGraph graph,
            IWorldGenerationTransactionalWorkExecutor executor,
            WorldRealizationMap realizationMap,
            IWorldGenerationExecutionReceiptStore receipts)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this.committer = new WorldRealizationBatchCommitter(realizationMap ?? throw new ArgumentNullException(nameof(realizationMap)));
            this.receipts = receipts ?? throw new ArgumentNullException(nameof(receipts));
        }

        public int Run(int maxItems)
        {
            if (maxItems < 0) throw new ArgumentOutOfRangeException(nameof(maxItems));

            int completed = 0;
            while (completed < maxItems)
            {
                WorldGenerationDependencySchedule schedule = graph.Dequeue(1);
                if (!schedule.Succeeded) throw new InvalidOperationException(schedule.Issues[0].Message);
                if (schedule.Count == 0) break;

                WorldGenerationWorkItem item = schedule.Items[0];
                WorldGenerationWorkKey key = new WorldGenerationWorkKey(item.Chunk, item.Kind);
                try
                {
                    WorldGenerationExecutionResult result = executor.Execute(item);
                    if (!result.WorkKey.Equals(key))
                        throw new InvalidOperationException("Transactional executor returned a result for a different generation work key.");

                    WorldRealizationBatchCommitResult commit = committer.TryCommit(result.Realization);
                    if (!commit.Succeeded)
                        throw new InvalidOperationException("World realization commit conflicted with existing world state.");

                    var receipt = new WorldGenerationExecutionReceipt(key, result.Fingerprint, result.Realization.Count);
                    if (!receipts.TryRecord(receipt))
                        throw new InvalidOperationException("Generation execution receipt conflicts with an existing receipt.");

                    if (!graph.Complete(key))
                        throw new InvalidOperationException("Generation work could not be marked completed after commit.");
                    completed++;
                }
                catch
                {
                    graph.Fail(key);
                    throw;
                }
            }

            return completed;
        }
    }

    /// <summary>Stable, process-independent fingerprint for a generation work result.</summary>
    public static class WorldGenerationExecutionFingerprint
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public static string Compute(WorldGenerationWorkKey key, WorldRealizationBatch batch)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            ulong hash = OffsetBasis;
            AddInt(ref hash, key.Chunk.X);
            AddInt(ref hash, key.Chunk.Y);
            AddInt(ref hash, (int)key.Kind);
            AddInt(ref hash, batch.Count);
            for (int i = 0; i < batch.Count; i++)
            {
                WorldRealizationEdit edit = batch.Edits[i];
                AddString(ref hash, edit.Id);
                AddString(ref hash, edit.SourceId);
                AddString(ref hash, edit.Kind);
                AddString(ref hash, edit.Value);
                AddInt(ref hash, edit.Priority);
                AddInt(ref hash, edit.Position.X);
                AddInt(ref hash, edit.Position.Y);
            }
            return hash.ToString("X16", CultureInfo.InvariantCulture);
        }

        private static void AddInt(ref ulong hash, int value)
        {
            unchecked
            {
                uint bits = (uint)value;
                for (int i = 0; i < 4; i++)
                {
                    hash ^= (byte)(bits >> (i * 8));
                    hash *= Prime;
                }
            }
        }

        private static void AddString(ref ulong hash, string value)
        {
            string text = value ?? string.Empty;
            AddInt(ref hash, text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                hash ^= (byte)character;
                hash *= Prime;
                hash ^= (byte)(character >> 8);
                hash *= Prime;
            }
        }
    }
}
