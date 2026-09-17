# World Generation Transactions

Version 0.1.105 adds a commit boundary between dependency execution state and world-space realization.

## Architecture

```text
World plan / deterministic world truth
                |
                v
     WorldGenerationWorkGraph
                |
                v
          claim one item
                |
                v
 IWorldGenerationTransactionalWorkExecutor
                |
                v
 WorldGenerationExecutionResult
        (stable fingerprint)
                |
                v
 WorldRealizationBatchCommitter
                |
          +-----+-----+
          |           |
       conflict     commit
          |           |
       Failed     world state
                      |
                      v
       execution receipt store
                      |
                      v
              graph.Complete
```

The dependency graph is the authority for execution lifecycle. The realization map is the authority for committed world-space edits. A work item is not marked `Completed` until the generated realization batch has been committed and a matching execution receipt has been recorded.

## Idempotent world commits

`WorldRealizationBatchCommitter` preflights the complete batch before writing it. Exact replays of already committed edits are treated as no-ops. Conflicting edits fail without partially writing the new batch.

This matters for worker retries and process recovery. A worker can safely replay a deterministic result after an uncertain failure window without duplicating an existing realization edit.

## Execution receipts

`WorldGenerationExecutionReceipt` records:

- the generation work key `(chunk, phase)`;
- a stable fingerprint of the generated realization result;
- the number of realization edits in that result.

`IWorldGenerationExecutionReceiptStore` is deliberately persistence-agnostic. `InMemoryWorldGenerationExecutionReceiptStore` is provided for runtime composition and tests; a project can implement the same interface on top of its save system.

The receipt fingerprint uses an explicit process-independent hash rather than `System.HashCode`, so the value is stable across process boundaries for the same work key and canonical realization batch.

## Transactional runner

`WorldGenerationTransactionalRunner` processes at most one claimed item at a time within the requested budget. This avoids leaving unrelated work stuck in `Running` when one transaction fails.

For each item:

1. claim dependency-ready work;
2. execute it to a deterministic `WorldGenerationExecutionResult`;
3. atomically commit its `WorldRealizationBatch`;
4. record its execution receipt;
5. mark the graph item `Completed`.

Any exception or commit/receipt conflict marks only the current work item `Failed`. Remaining budget capacity is not claimed until the current transaction has completed.

## Failure recovery

A commit can succeed and receipt persistence can fail. In that case the graph records the work as `Failed`, but the already-committed realization remains valid. Retrying the same deterministic result is safe because the realization committer recognizes the exact edits as already present.

This separates three durable concerns:

```text
execution state   -> WorldGenerationWorkGraph
world state       -> WorldRealizationMap / persistence adapter
commit evidence   -> IWorldGenerationExecutionReceiptStore
```

The package does not assume a particular save format or filesystem layout.

## Relationship to streaming

Core Keeper's documented world generation progressively materializes procedural terrain and scenery as players reach new areas, while generated world state is persisted in the world save. The transaction layer applies the same architectural separation without making Unity chunk residency the source of truth: streaming decides when work is executed, while world coordinates and committed realization edits remain authoritative. citeturn148802search0turn148802search2

## Compatibility

The existing `WorldGenerationScheduler`, `WorldGenerationWorkGraph`, `IWorldGenerationWorkExecutor`, and `WorldGenerationDependencySchedulerRunner` APIs remain available. Transactional execution is additive through `IWorldGenerationTransactionalWorkExecutor` and `WorldGenerationTransactionalRunner`.
