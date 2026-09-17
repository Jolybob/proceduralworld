# World Generation Dependencies

The generation scheduler has an explicit dependency graph and a persistent execution lifecycle. Scheduling determines what is legal and desirable to execute; execution state records what is actually in flight, completed, or failed.

## Architecture

```text
World truth / requested work
          |
          v
WorldGenerationWorkGraph
          |
          +--> WorkKey: (chunk, phase)
          +--> explicit prerequisites
          +--> Pending -> Running -> Completed
          |                    |
          |                    +-> Failed -> Retry -> Pending
          +--> deterministic topological scheduling
          +--> cycle / failed-dependency diagnostics
          +--> bounded claim budgets
          v
executor
          |
          v
plan -> realization -> materialization
```

A chunk is no longer the identity of work. A **work key** is `(ChunkCoord, WorldGenerationWorkKind)`, so one chunk can carry independent plan, realization, and materialization stages.

## Persistent execution state

`Dequeue(maxItems)` now **claims** ready work instead of deleting it. Claimed work becomes `Running` and remains represented in the graph until the executor reports completion or failure.

- `Complete(key)` marks successful execution complete.
- `Fail(key)` records execution failure without losing the work item or its dependency edges.
- `Retry(key)` returns failed work to `Pending`.
- `GetStatus(key)` and status counters expose execution state without coupling the graph to Unity.

This is important for frame budgets and asynchronous workers: consuming a budget must not erase prerequisites that later stages still need to observe.

## Dependency semantics

A prerequisite is satisfied only when its status is `Completed`. `Running` work blocks dependents without being treated as a cycle. `Failed` work blocks dependents with a structured `FailedDependency` diagnostic until it is retried and completed.

Dependencies may cross chunk boundaries. A materialization request can therefore wait on a realization owned by another chunk without making chunk residency the authority for world state.

## Deterministic execution

`BuildSchedule()` orders currently-ready work by priority descending, phase, then chunk X/Y. Request arrival order does not determine the order of independent work. A dependency always wins over priority: a high-priority dependent cannot execute before its prerequisites complete.

Cycle diagnostics inspect only pending-to-pending dependency edges, so an ordinary in-flight prerequisite is correctly reported as blocked rather than cyclic.

## Failure and retry

`WorldGenerationDependencySchedulerRunner` executes claimed work through `IWorldGenerationWorkExecutor`. Successful calls are marked `Completed`; exceptions mark the corresponding work `Failed` before the exception is propagated. A caller can then retry the failed key after correcting the underlying condition.

This keeps generation state recoverable across worker failures and makes retries explicit rather than silently regenerating or dropping work.

## Streaming relationship

```text
semantic plan
    -> world reservations
    -> world realization
    -> dependency graph
    -> claim ready work
    -> execute
       |-> complete
       |-> fail / retry
    -> chunk execution
    -> streaming / persistence / rendering
```

World coordinates remain authoritative. Chunk residency, frame budgets, worker assignment, and execution order can change without changing deterministic world intent.

## Compatibility

`WorldGenerationScheduler` remains available as the simple chunk-level queue introduced in 0.1.102. `WorldGenerationWorkGraph` remains the dependency-aware API introduced in 0.1.103, now with persistent execution state suitable for multi-frame and asynchronous execution.
