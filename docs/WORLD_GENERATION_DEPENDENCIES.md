# World Generation Dependencies

The generation scheduler now has an explicit dependency graph in addition to deterministic priority ordering.

## Architecture

```text
World truth / requested work
          |
          v
WorldGenerationWorkGraph
          |
          +--> WorkKey: (chunk, phase)
          +--> explicit prerequisites
          +--> deterministic topological scheduling
          +--> cycle diagnostics
          +--> bounded dequeue
          v
WorldGenerationDependencySchedulerRunner
          |
          v
plan -> realization -> materialization
```

The important distinction is that a chunk is no longer the identity of work. A **work key** is `(ChunkCoord, WorldGenerationWorkKind)`. This allows one chunk to carry independent plan, realization, and materialization stages while dependencies explicitly define their required order.

## Deterministic execution

`WorldGenerationWorkGraph.BuildSchedule()` performs a deterministic topological sort. Among currently-ready work it orders by phase, then chunk X/Y. Request arrival order therefore does not determine execution order for independent work.

Dependencies may cross chunk boundaries. This is intentional: a materialization request can depend on a plan or realization result owned by another chunk without forcing that chunk to be loaded as the source of truth.

## Budgets

`BuildSchedule(maxItems)` returns the first deterministic ready prefix. `Dequeue(maxItems)` removes only that prefix after a successful dependency validation. A dependency cycle prevents dequeue and returns a structured `DependencyCycle` issue instead of silently executing invalid work.

## Streaming relationship

The scheduler is an execution mechanism, not the authority for world state:

```text
semantic plan
    -> world reservations
    -> world realization
    -> dependency graph
    -> chunk execution
    -> streaming / persistence / rendering
```

World coordinates remain authoritative. Chunk residency and execution order can change without changing deterministic world intent.

## Compatibility

`WorldGenerationScheduler` remains available as the simple chunk-level queue introduced in 0.1.102. `WorldGenerationWorkGraph` is the dependency-aware layer for systems that need explicit phase and cross-chunk prerequisites.
