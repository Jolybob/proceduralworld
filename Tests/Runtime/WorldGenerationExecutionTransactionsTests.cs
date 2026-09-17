using System;
using NUnit.Framework;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldGenerationExecutionTransactionsTests
    {
        private static WorldRealizationEdit Edit(string id, int x, int y, string kind = "Tile", string value = "1") =>
            new WorldRealizationEdit(id, "generator", kind, value, 0, new WorldPosition(x, y));

        private static WorldRealizationBatch Batch(params WorldRealizationEdit[] edits) =>
            new WorldRealizationBatch(edits);

        [Test]
        public void BatchCommitIsIdempotentForExactReplay()
        {
            var map = new WorldRealizationMap(4);
            var committer = new WorldRealizationBatchCommitter(map);
            WorldRealizationBatch batch = Batch(Edit("a", 0, 0), Edit("b", 5, 0));

            WorldRealizationBatchCommitResult first = committer.TryCommit(batch);
            WorldRealizationBatchCommitResult second = committer.TryCommit(batch);

            Assert.IsTrue(first.Succeeded);
            Assert.AreEqual(2, first.AddedCount);
            Assert.AreEqual(0, first.AlreadyPresentCount);
            Assert.IsTrue(second.Succeeded);
            Assert.AreEqual(0, second.AddedCount);
            Assert.AreEqual(2, second.AlreadyPresentCount);
            Assert.AreEqual(2, map.Count);
        }

        [Test]
        public void ConflictingBatchIsAtomicAndWritesNothing()
        {
            var map = new WorldRealizationMap(4);
            Assert.IsTrue(map.TryAdd(Edit("existing", 1, 0), out WorldRealizationConflict ignored));
            var committer = new WorldRealizationBatchCommitter(map);
            WorldRealizationBatch batch = Batch(Edit("new", 0, 0), Edit("conflict", 1, 0));

            WorldRealizationBatchCommitResult result = committer.TryCommit(batch);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.HasConflict);
            Assert.AreEqual(1, map.Count);
            Assert.IsFalse(map.TryGet("new", out WorldRealizationEdit missing));
        }

        [Test]
        public void FingerprintIsIndependentOfBatchInputOrder()
        {
            var key = new WorldGenerationWorkKey(new ChunkCoord(-2, 7), WorldGenerationWorkKind.Realization);
            WorldRealizationBatch first = Batch(Edit("a", 2, 3), Edit("b", -1, 4));
            WorldRealizationBatch second = Batch(Edit("b", -1, 4), Edit("a", 2, 3));

            Assert.AreEqual(
                WorldGenerationExecutionFingerprint.Compute(key, first),
                WorldGenerationExecutionFingerprint.Compute(key, second));
        }

        [Test]
        public void TransactionCompletesGraphOnlyAfterCommitAndReceipt()
        {
            var graph = new WorldGenerationWorkGraph();
            var chunk = new ChunkCoord(0, 0);
            graph.Request(chunk, WorldGenerationWorkKind.Realization);
            var key = new WorldGenerationWorkKey(chunk, WorldGenerationWorkKind.Realization);
            var map = new WorldRealizationMap(4);
            var receipts = new InMemoryWorldGenerationExecutionReceiptStore();
            var runner = new WorldGenerationTransactionalRunner(graph, new FixedExecutor(key, Batch(Edit("feature", 0, 0))), map, receipts);

            Assert.AreEqual(1, runner.Run(1));
            Assert.AreEqual(WorldGenerationWorkStatus.Completed, graph.GetStatus(key));
            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(1, receipts.Count);
            Assert.IsTrue(receipts.TryGet(key, out WorldGenerationExecutionReceipt receipt));
            Assert.AreEqual(1, receipt.EditCount);
        }

        [Test]
        public void FailedTransactionLeavesLaterWorkPending()
        {
            var graph = new WorldGenerationWorkGraph();
            var firstChunk = new ChunkCoord(0, 0);
            var secondChunk = new ChunkCoord(1, 0);
            graph.Request(firstChunk, WorldGenerationWorkKind.Realization);
            graph.Request(secondChunk, WorldGenerationWorkKind.Realization);
            var firstKey = new WorldGenerationWorkKey(firstChunk, WorldGenerationWorkKind.Realization);
            var secondKey = new WorldGenerationWorkKey(secondChunk, WorldGenerationWorkKind.Realization);
            var executor = new ThrowingFirstExecutor(firstKey);
            var runner = new WorldGenerationTransactionalRunner(graph, executor, new WorldRealizationMap(4), new InMemoryWorldGenerationExecutionReceiptStore());

            Assert.Throws<InvalidOperationException>(() => runner.Run(2));
            Assert.AreEqual(WorldGenerationWorkStatus.Failed, graph.GetStatus(firstKey));
            Assert.AreEqual(WorldGenerationWorkStatus.Pending, graph.GetStatus(secondKey));
        }

        [Test]
        public void RetryAfterReceiptFailureReplaysWorldCommitWithoutDuplication()
        {
            var graph = new WorldGenerationWorkGraph();
            var chunk = new ChunkCoord(2, -1);
            var key = new WorldGenerationWorkKey(chunk, WorldGenerationWorkKind.Realization);
            graph.Request(chunk, WorldGenerationWorkKind.Realization);
            var map = new WorldRealizationMap(4);
            var receipts = new FailOnceReceiptStore();
            var executor = new FixedExecutor(key, Batch(Edit("feature", 8, -5)));
            var runner = new WorldGenerationTransactionalRunner(graph, executor, map, receipts);

            Assert.Throws<InvalidOperationException>(() => runner.Run(1));
            Assert.AreEqual(WorldGenerationWorkStatus.Failed, graph.GetStatus(key));
            Assert.AreEqual(1, map.Count);

            Assert.IsTrue(graph.Retry(key));
            Assert.AreEqual(1, runner.Run(1));
            Assert.AreEqual(WorldGenerationWorkStatus.Completed, graph.GetStatus(key));
            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(1, receipts.Count);
        }

        private sealed class FixedExecutor : IWorldGenerationTransactionalWorkExecutor
        {
            private readonly WorldGenerationWorkKey key;
            private readonly WorldRealizationBatch batch;

            public FixedExecutor(WorldGenerationWorkKey key, WorldRealizationBatch batch)
            {
                this.key = key;
                this.batch = batch;
            }

            public WorldGenerationExecutionResult Execute(WorldGenerationWorkItem item) => new WorldGenerationExecutionResult(key, batch);
        }

        private sealed class ThrowingFirstExecutor : IWorldGenerationTransactionalWorkExecutor
        {
            private readonly WorldGenerationWorkKey failingKey;

            public ThrowingFirstExecutor(WorldGenerationWorkKey failingKey) { this.failingKey = failingKey; }

            public WorldGenerationExecutionResult Execute(WorldGenerationWorkItem item)
            {
                var key = new WorldGenerationWorkKey(item.Chunk, item.Kind);
                if (key.Equals(failingKey)) throw new InvalidOperationException("synthetic failure");
                return new WorldGenerationExecutionResult(key, WorldRealizationBatch.Empty);
            }
        }

        private sealed class FailOnceReceiptStore : IWorldGenerationExecutionReceiptStore
        {
            private readonly InMemoryWorldGenerationExecutionReceiptStore inner = new InMemoryWorldGenerationExecutionReceiptStore();
            private bool fail = true;

            public int Count => inner.Count;

            public bool TryGet(WorldGenerationWorkKey workKey, out WorldGenerationExecutionReceipt receipt) => inner.TryGet(workKey, out receipt);

            public bool TryRecord(WorldGenerationExecutionReceipt receipt)
            {
                if (fail)
                {
                    fail = false;
                    return false;
                }
                return inner.TryRecord(receipt);
            }
        }
    }
}
