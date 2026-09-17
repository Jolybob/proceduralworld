# World Generation Execution Checkpoints

## Purpose

`WorldGenerationExecutionLease` makes a generation worker's ownership explicit. The next boundary is resumable execution: a worker must be able to yield after a bounded amount of work and continue later without rebuilding the same generation phase from the beginning.

This layer keeps the existing authority model intact:

```text
world truth / demand
        |
        v
WorldGenerationWorkGraph
        |
        v
dependency-ready work
        |
        v
WorldGenerationExecutionLease
        |
        v
WorldGenerationExecutionCheckpoint
        |
        +---- yield after a bounded step
        +---- renew lease across frames
        +---- reclaim expired lease
        |         |
        |         v
        |    rebind checkpoint to new attempt
        v
world realization / materialization
```

The architecture is intentionally aligned with incremental procedural generation: Core Keeper's documented generation work can be spread over multiple frames to reduce stuttering, while chunk generation continues to be triggered by exploration and can cause additional structure-generation work. The package therefore treats a generation phase as resumable work rather than assuming one scheduler dequeue equals one completed chunk.

## Checkpoint model

`WorldGenerationExecutionCheckpoint` records:

- the generation work key;
- the current lease ID and attempt;
- a semantic phase name;
- a phase-local step number;
- a monotonic progress ordinal;
- completed work units;
- optional total work units.

The checkpoint is execution state, not world truth. A checkpoint never becomes a substitute for deterministic world-space data.

## Lease binding

A checkpoint is always bound to the current execution lease. When a lease expires and another worker claims the same work item:

1. the attempt number increases;
2. the lease ID changes;
3. the existing checkpoint progress is preserved;
4. the checkpoint is rebound to the new lease;
5. the previous worker can no longer write progress.

This prevents a stale worker from corrupting a resumed generation phase.

## Resumable executor

`IWorldGenerationResumableWorkExecutor` receives:

- the work item;
- the current execution lease;
- the latest checkpoint.

It returns `WorldGenerationExecutionStep`:

- `Continue` saves progress and leaves the work `Running`;
- `Completed` commits the final checkpoint and completes the lease;
- `Failed` records the checkpoint and fails the work item.

`WorldGenerationResumableExecutionRunner` processes active work for its owner before claiming new work. This is important: a frame budget should resume partially completed generation before starting unrelated new generation.

## Determinism

Checkpoint ordering and active-work selection are deterministic by work key and lease identity. Progress is monotonic within an execution attempt. Reclaimed work preserves progress but receives a new attempt and lease ID.

The checkpoint layer does not make generation itself nondeterministic or stateful in world space. A resumable generator should derive each step from deterministic inputs and use the checkpoint only to select how much of that deterministic process has already been committed.

## Failure and retry

Failed work retains its latest checkpoint. Calling `WorldGenerationWorkGraph.Retry` returns it to `Pending`; the next claim rebinds the saved progress to a new lease attempt. This makes retries resumable instead of forcing a complete restart.

Completed work removes its execution checkpoint because no further execution state is required once the world commit has succeeded.

## Streaming relationship

Chunk boundaries remain an execution and storage boundary, not a source of truth. A single resumable work item can still represent cross-chunk planning, realization, or materialization. This matters for structures and corridors that span chunk boundaries.

The intended runtime flow is therefore:

```text
world-space truth
    |
    +--> plan / reservation / realization
    |
    v
chunk demand
    |
    v
dependency work graph
    |
    v
lease ownership
    |
    v
resumable execution checkpoint
    |
    +--> frame budget / worker budget
    |
    v
transactional world commit
    |
    v
persistence / presentation
```

## API

Primary types:

- `WorldGenerationExecutionCheckpoint`
- `WorldGenerationExecutionStep`
- `WorldGenerationExecutionCheckpointStore`
- `WorldGenerationResumableExecutionCoordinator`
- `IWorldGenerationResumableWorkExecutor`
- `WorldGenerationResumableExecutionRunner`

This layer deliberately sits above the dependency graph and execution leases rather than replacing them.
