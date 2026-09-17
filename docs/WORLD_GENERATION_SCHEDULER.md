# World Generation Scheduler

The generation scheduler is the execution boundary between deterministic world truth and streaming work.

```text
World truth / requested chunks
          |
          v
WorldGenerationScheduler
          |
          +--> deterministic priority ordering
          +--> duplicate chunk coalescing
          +--> bounded dequeue budget
          +--> cancellation
          v
WorldGenerationSchedulerRunner
          |
          v
chunk generation / realization / materialization
```

## Design rules

- **World coordinates remain authoritative.** Scheduling never changes generated world truth.
- **Streaming order is not generation order.** Requests are canonicalized by priority, work kind, then chunk coordinates.
- **Duplicate requests coalesce.** A chunk has at most one pending work item; a higher-priority request replaces lower-priority work.
- **Budgets are explicit.** `Dequeue(maxItems)` lets a frame, worker, or server tick consume bounded work without changing the resulting canonical order.
- **Execution is injected.** `IWorldGenerationWorkExecutor` keeps the scheduler independent from Unity, Tilemaps, rendering, and persistence.

## Work kinds

`Plan`, `Realization`, and `Materialization` provide a small execution vocabulary. The scheduler orders them deterministically within equal priority. The package can add richer work phases later without moving scheduling into chunk objects.

## Determinism

`WorldGenerationSchedule` sorts by priority, work kind, X, Y, and sequence. The scheduler's canonical schedule ignores request arrival order for distinct chunks. `Sequence` only remains as a final identity field on a queued item; canonical scheduling does not use it as a tie-breaker.

This means asynchronous streaming can request chunks in any order while the same pending set produces the same execution sequence.

## Next layer

The scheduler intentionally does not generate content itself. The next integration point is a materialization executor that consumes `WorldGenerationWorkItem`, reads world-space realization state, and applies only the work relevant to the requested chunk. This preserves the architecture:

```text
semantic intent -> world realization -> deterministic schedule -> chunk execution
```
