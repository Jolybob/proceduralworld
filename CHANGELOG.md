# Changelog

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

## [0.1.98] - 2026-09-17

- Added `IWorldPlanFeatureResolver` and `WorldPlanFeatureLowerer` to lower semantic world-plan nodes into the existing world-space feature placement kernel.
- Added `IWorldPlanPlacementFeasibility` and `WorldPlanPlacementContext` so terrain, cave, water, reservation, and protected-area rules can reject or accept candidate footprints without coupling the core planner to a specific world representation.
- Added deterministic bounded Manhattan-ring relocation when the preferred layout anchor is not feasible.
- Added mathematical floor-division owner-chunk assignment for negative world coordinates.
- Added source-node traceability through `WorldPlanFeaturePlacement` and structured lowering diagnostics.
- Added regression coverage for lowering, deterministic feasibility relocation, and negative-coordinate owner chunks.
- Added world-plan feature-lowering architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.98.

## [0.1.97] - 2026-09-17

- Added `WorldPlanLayoutSettings`, `WorldPlanNodeLayout`, `WorldPlanLayoutPort`, and `WorldPlanLayout` as a Unity-independent world-space layout representation.
- Added `WorldPlanLayoutSolver` to deterministically arrange compiled world-plan nodes by connected-component graph distance and canonical node ID ordering.
- Added minimum-footprint and minimum-clearance aware spacing so generated plan layouts do not overlap occupied node regions.
- Added deterministic semantic port anchors with stable left/right/bottom side assignment and ordinal port ordering.
- Added regression coverage for input-order independence, clearance-aware spacing, deterministic layer advancement, and connection-driven port anchoring.
- Added world-plan layout architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.97.
