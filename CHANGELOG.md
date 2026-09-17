# Changelog

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
- Added `IWorldPlanRealizationSource` and `WorldPlanRealizer` to convert lowered world-plan feature placements into renderer/gameplay-independent world edits.
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
