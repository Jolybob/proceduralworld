# World Generation Execution Leases

Version 0.1.106 adds deterministic ownership around `Running` generation work.

## Why leases exist

Version 0.1.105 made generation execution transactional: deterministic work can produce a world-space realization batch, commit it, record a receipt, and only then complete the dependency-graph item.

That still left a lifecycle gap for long-running or distributed execution. A `Running` item had no owner and no expiry. A stalled worker could therefore hold work indefinitely, while a recovered worker had no safe way to distinguish its current attempt from an old worker's late completion.

Execution leases close that gap without moving authority away from the world-space model.

## Lifecycle

```text
Pending
   |
   | Claim(owner, tick, duration)
   v
Running + ExecutionLease
   |                  |
   | Complete         | Expire / Recover
   v                  v
Completed          Pending
                      |
                      | Claim again
                      v
                 Running + Attempt N+1
```

A lease identifies:

- the generation work key `(chunk, phase)`;
- the worker owner ID;
- a deterministic attempt number;
- a deterministic lease ID;
- acquisition and expiry ticks.

A lease is an ownership token, not world state. World coordinates, realization edits, and persistence remain authoritative elsewhere.

## Core API

`WorldGenerationExecutionLease` is the immutable ownership token.

`WorldGenerationWorkGraph.Claim(...)` claims only dependency-ready `Pending` work and marks it `Running` with a lease.

`WorldGenerationWorkGraph.Renew(...)` extends the active lease without changing its identity or attempt.

`WorldGenerationWorkGraph.RecoverExpired(...)` deterministically returns expired leased work to `Pending`.

`WorldGenerationWorkGraph.Complete(lease)` and `Fail(lease)` require the exact current lease. A stale worker therefore cannot complete or fail a later reclaimed attempt.

`WorldGenerationExecutionLeaseCoordinator` exposes the lease lifecycle without exposing the graph implementation to callers.

`WorldGenerationExecutionLeaseRunner` injects the lease into an executor and commits lifecycle state through the lease token.

## Determinism

Lease identity is derived from the worker owner, work key, and monotonically increasing attempt number. Request order does not affect the order in which dependency-ready work is claimed because the existing deterministic graph ordering remains authoritative.

Expired leases are recovered in canonical chunk/kind/owner/attempt order so recovery itself does not depend on dictionary iteration order.

Attempt history remains allocated for a work key even if the graph entry is removed and later re-added. This prevents an old lease ID from being reused accidentally.

## Stale-worker protection

A reclaimed work item receives a new attempt and new lease ID. Completion and failure operations compare the complete immutable lease against the graph's current active lease.

```text
worker A: claim attempt 1
worker A: stalls
lease expires
worker B: claim attempt 2
worker A: late Complete(attempt 1)  -> rejected
worker B: Complete(attempt 2)       -> accepted
```

This protects execution state from late worker messages. Executors should keep world mutation behind the transactional realization boundary so stale computation cannot directly mutate authoritative world state.

## Relationship to transactions and streaming

The intended execution stack is now:

```text
World truth / world plan
        |
        v
WorldGenerationWorkGraph
        |
        v
Execution Lease
        |
        v
Deterministic worker computation
        |
        v
WorldGenerationExecutionResult
        |
        v
WorldRealizationBatchCommitter
        |
        v
Execution Receipt Store
        |
        v
Lease-authorized Complete
        |
        v
Streaming / materialization
```

This preserves the package rule that streaming controls *when* work executes, while world coordinates and committed world-space edits control *what the world is*.

Core Keeper's documented world generation progressively materializes terrain and scenery as players reach new areas, with generated world state stored in the world save. The lease boundary provides the package-level execution ownership needed to make that style of progressive generation robust across bounded worker/frame budgets without tying correctness to chunk residency.

## Compatibility

The existing non-leased scheduler and `WorldGenerationDependencySchedulerRunner` remain available. Existing key-based `Complete` and `Fail` APIs continue to support legacy non-leased execution, while leased work must use the lease token variants.

The new layer is intentionally runtime-only and persistence-agnostic. A project can persist lease state externally if worker recovery spans process restarts, while the graph remains the authority for live execution state.
