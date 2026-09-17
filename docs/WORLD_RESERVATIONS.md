# World Reservations

World reservations provide a persistent, world-space coordination layer between semantic planning and chunk materialization.

The architectural rule is:

> World coordinates define the truth; chunks define the execution and storage boundary.

A reservation therefore belongs to world space even when its footprint crosses multiple chunks. The chunk index is only an acceleration structure for queries.

## Pipeline

```text
WorldPlan / world feature planning
        |
        v
WorldReservationMap  <---- terrain/protected/gameplay policies
        |
        +---- placement feasibility
        |
        +---- corridor traversal
        |
        +---- corridor reservation
        v
chunk materialization / streaming
```

## Core API

- `WorldReservation` describes an inclusive world-space rectangle, owner, kind, and priority.
- `WorldReservationMap` stores reservations and indexes them by the chunks they overlap.
- `TryReserve` is atomic: a conflicting reservation is rejected and the map remains unchanged.
- `CollectContaining` and `CollectIntersecting` provide deterministic world-space queries and de-duplicate cross-chunk results.
- Negative coordinates use mathematical floor division so chunk ownership remains stable across the origin.
- Same-owner reservations can coexist, allowing one planning domain to query its own occupied footprint without self-conflict.

## Planning integration

`WorldPlanPlacementReservationPolicy` adapts the map to `IWorldPlanPlacementFeasibility`. Accepted feature candidates become reservations, so later deterministic nodes cannot overlap earlier accepted nodes.

`WorldPlanCorridorReservationTraversal` adapts reservations to `IWorldPlanCorridorTraversal`. A corridor can therefore route around protected areas, already-realized features, or earlier infrastructure without knowing how those systems store their data.

`WorldPlanCorridorReservationWriter` commits a realized corridor as one-cell reservations. It rolls back the corridor's partial reservations if a conflict is encountered, keeping realization atomic.

## Determinism and streaming

Reservation queries are sorted by stable reservation identity. The store does not depend on Unity objects, loaded chunks, scene hierarchy, or iteration order of dictionaries. This makes it suitable for deterministic generation and save/load systems.

The current map is an in-memory coordination primitive. Persistence can serialize `WorldReservation` records, while future implementations can replace or augment the chunk index without changing planner contracts.

## Why this layer exists

Feature lowering and corridor planning both need to answer the same world-level question: **is this space already claimed by something that must be respected?** Keeping that state in a dedicated world-space layer avoids coupling those planners to terrain tiles, chunk lifetime, Tilemaps, or specific gameplay systems.
