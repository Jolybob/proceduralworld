# Changelog

## [0.1.108] - 2026-09-18

- Added `WorldPlanRuntimeBuilder` as the authoritative orchestration boundary for semantic plan expansion, compilation, world-space layout, optional feature lowering, optional realization, and chunk indexing.
- Added `WorldPlanRuntimeSettings` so feature resolution, placement feasibility, realization semantics, and deterministic layout/lowering policies remain explicit runtime dependencies.
- Added an explicit world-plan `ChunkSize` contract; lowering settings must match it, and generators reject plan runtimes configured for a different chunk size.
- Added `WorldPlanRuntime` with a world-space realization index and chunk-intersection queries without creating chunk-local planning graphs.
- Added `IWorldPlanChunkGenerator` and `WorldPlanChunkGeneration` as the optional plan-aware capability boundary for chunk generators.
- Passed the immutable world-plan runtime through `WorldGenerationContext` so custom generation passes can consume global semantic intent during chunk materialization.
- Integrated optional `WorldPlanGraphAsset` references into `ProceduralWorldDefinitionAsset`, compiling and laying out the plan once per generator instance using the world definition seed and chunk size.
- Added regression coverage for plan/layout runtime creation, feature lowering and chunk-indexed realization, chunk-size invariants, context propagation, and capability detection.
- Restored the complete world-generation checkpoint regression test suite after the previous test-file regression and retained the required `System` import.
- Added world-plan runtime integration documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.108.

## [0.1.107] - 2026-09-17

- Added `WorldGenerationExecutionCheckpoint` for resumable generation phase state across bounded execution budgets.
- Added lease-bound checkpoint storage with stale-worker protection, monotonic progress validation, and deterministic active-work ordering.
- Added checkpoint rebinding so expired work resumes from its last committed progress under a new execution attempt.
- Added `IWorldGenerationResumableWorkExecutor` and `WorldGenerationResumableExecutionRunner` for one-step-at-a-time generation execution across frames or worker budgets.
- Preserved failed checkpoints across explicit retries while removing execution checkpoints after successful completion.
- Added regression coverage for budgeted resume, lease reclaim, stale checkpoint rejection, deterministic active-work resumption, and retry state.
- Added world-generation execution checkpoint architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.107.

## [0.1.106] - 2026-09-17

- Added `WorldGenerationExecutionLease` as a deterministic ownership token for running generation work.
- Added `Claim`, `Renew`, `RecoverExpired`, and lease-authorized `Complete` / `Fail` lifecycle operations to the dependency work graph.
- Added deterministic attempt numbers and lease IDs so reclaimed work is distinguishable from stale worker executions.
- Added stale-worker protection: an expired or superseded lease cannot complete or fail a later execution attempt.
- Added `WorldGenerationExecutionLeaseCoordinator`, `IWorldGenerationLeasedWorkExecutor`, and `WorldGenerationExecutionLeaseRunner` for dependency-safe worker execution.
- Added regression coverage for lease creation, renewal, expiry/reclaim, deterministic claim ordering, stale-worker rejection, and runner success/failure.
- Added world-generation execution lease architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.106.

## [0.1.105] - 2026-09-17

- Added `WorldGenerationExecutionResult` with stable fingerprints for deterministic world-generation outputs.
- Added `WorldGenerationExecutionReceipt` and `IWorldGenerationExecutionReceiptStore` as a persistence-agnostic commit-evidence boundary.
- Added `InMemoryWorldGenerationExecutionReceiptStore` for runtime composition and regression testing.
- Added `WorldRealizationBatchCommitter` for atomic, idempotent world-space realization commits and exact replay handling.
- Added `IWorldGenerationTransactionalWorkExecutor` and `WorldGenerationTransactionalRunner` to complete generation work only after world commit and receipt persistence succeed.
- Added deterministic, process-independent execution fingerprints using an explicit stable hash.
- Added regression coverage for idempotent replay, atomic conflict handling, fingerprint determinism, transactional completion, failure isolation, and retry after receipt persistence failure.
- Added world-generation transaction architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.105.

## [0.1.104] - 2026-09-17

- Added persistent `WorldGenerationWorkStatus` state for pending, running, completed, and failed generation work.
- Changed the dependency-aware graph so dequeued work remains in the graph as `Running` until explicitly completed or failed; dependency edges therefore survive execution budgets.
- Added `Complete`, `Fail`, `Retry`, status counters, and status queries for deterministic execution lifecycle management.
- Updated the dependency scheduler runner to mark successful executor calls completed and failed calls failed before propagating the exception.
- Added failed-dependency diagnostics and deterministic retry behavior.
- Added regression coverage for multi-budget phase execution, running prerequisites, failed work, retry, and persistent dependency state.
- Hardened cycle diagnostics so running prerequisites are reported as blocked work rather than falsely classified as dependency cycles.
- Bumped the package version to 0.1.104.

## [0.1.103] - 2026-09-17

- Added `WorldGenerationWorkKey` so generation work is identified by `(chunk, phase)` rather than chunk alone.
- Added `WorldGenerationDependency` and `WorldGenerationWorkGraph` for explicit same-chunk and cross-chunk prerequisites.
- Added deterministic topological scheduling with stable ready-work ordering.
- Added bounded dequeue that executes only dependency-ready work and preserves pending prerequisites.
- Added structured dependency-cycle diagnostics and safe dequeue behavior when the graph is cyclic.
- Added `WorldGenerationDependencySchedulerRunner` for executor injection without coupling scheduling to Unity rendering, persistence, or chunk implementation details.
- Added regression coverage for request-order determinism, multi-phase same-chunk execution, cross-chunk dependencies, cycle detection, and bounded dequeue.
- Added world-generation dependency architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.103.

## [0.1.102] - 2026-09-17

- Added `WorldGenerationWorkItem`, `WorldGenerationSchedule`, and `WorldGenerationScheduler` as a deterministic bounded execution queue for streaming-driven generation work.
- Added priority-based request coalescing so a chunk has at most one pending work item and higher-priority work replaces lower-priority work.
- Added deterministic scheduling independent of request arrival order for distinct chunks.
- Added explicit dequeue budgets and cancellation for frame, worker, and server-tick execution.
- Added `IWorldGenerationWorkExecutor` and `WorldGenerationSchedulerRunner` to keep scheduling independent from Unity rendering, persistence, and chunk implementation details.
- Added regression coverage for request-order determinism, priority coalescing, bounded dequeue, and cancellation.
- Added world-generation scheduler architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.102.

## [0.1.101] - 2026-09-17

- Added `WorldRealizationEdit`, `WorldRealizationBatch`, and `WorldRealizationMap` as the deterministic world-space boundary between semantic intent and chunk materialization.
- Added source traceability, operation kinds, priorities, stable canonical ordering, conflict detection, removal, point/chunk/rectangle queries, and negative-coordinate indexing.
- Added `IWorldPlanRealizationSource` and `WorldPlanRealizer` to convert lowered world-plan feature placements into deterministic world edits.
- Added regression coverage for deterministic batches, negative coordinates, same-kind conflicts, different-layer coexistence, and deduplicated rectangle queries.
- Added world-realization architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.101.

## [0.1.100] - 2026-09-17

- Added `WorldReservation` and `WorldReservationMap` as a deterministic world-space coordination layer for claimed, protected, occupied, or otherwise reserved areas.
- Added chunk-indexed reservation queries with cross-chunk de-duplication while keeping world coordinates authoritative.
- Added atomic reservation conflict handling, removal, lookup, containment queries, and mathematical floor-division for negative world coordinates.
- Added `WorldPlanPlacementReservationPolicy` to integrate reservations with feature-lowering feasibility and prevent deterministic plan-node footprint overlap.
- Added `WorldPlanCorridorReservationTraversal` to route corridors around reserved world cells without coupling corridor planning to chunk residency.
- Added `WorldPlanCorridorReservationWriter` with atomic rollback when a realized corridor conflicts with existing reservations.
- Added regression coverage for cross-chunk indexing, overlap conflicts, negative coordinates, and corridor rollback.
- Added world-reservation architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.100.

## [0.1.99] - 2026-09-17

- Added `WorldPlanCorridor`, `WorldPlanCorridorPlanner`, and deterministic world-space corridor realization for compiled plan connections.
- Added `IWorldPlanCorridorTraversal` and `WorldPlanCorridorContext` so terrain, water, cave, reservation, and protected-area rules can control corridor traversal and costs without coupling the planner to a specific world representation.
- Added deterministic 4-neighbour A* with bounded search, stable tie-breaking, positive traversal-cost handling, and Manhattan fallback when no traversal policy is supplied.
- Added connection/node traceability and structured diagnostics for missing anchors and unreachable corridors.
- Added regression coverage for connection-order determinism and traversal-policy detours.
- Added world-plan corridor architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.99.
