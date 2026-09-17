# World-space realization

The realization layer is the boundary between semantic world intent and chunk-local materialization.

```text
WorldPlan
   |
   v
Deterministic layout / feature lowering
   |
   v
WorldRealizationEdit
   |
   v
WorldRealizationBatch
   |
   v
WorldRealizationMap  <---- world edits / protected areas / gameplay systems
   |
   +---- query by world position
   +---- query by chunk
   +---- query by world rectangle
   |
   v
chunk materialization / streaming / rendering
```

## Design rules

- World coordinates remain authoritative. A chunk is an execution and indexing boundary, not the owner of semantic truth.
- A realization edit is deterministic and traceable through `Id` and `SourceId`.
- `Kind` separates independently materialized layers such as tiles, objects, decals, or collision.
- `Priority` is retained as deterministic ordering metadata; the core map intentionally rejects competing edits of the same kind at the same cell instead of silently resolving gameplay conflicts.
- `WorldRealizationBatch` canonicalizes input order so upstream systems can emit edits in any order without changing the resulting stream.
- `WorldRealizationMap` indexes each edit by its mathematical owner chunk, including negative coordinates, while queries continue to operate in world space.
- Rectangle queries de-duplicate edits even when future indexing strategies place an edit in more than one acceleration bucket.

## Planning-to-materialization flow

`WorldPlanRealizer` consumes the already-lowered `WorldPlanFeaturePlacement` stream and delegates the actual semantic-to-geometry mapping to `IWorldPlanRealizationSource`. This keeps feature definitions, plan topology, and Unity Tilemap/GameObject concerns outside the core planning layer.

A runtime adapter can then insert the resulting batch into `WorldRealizationMap`, query only the current chunk, and apply those edits to a renderer or gameplay representation. Streaming a chunk back in does not require recomputing the semantic plan merely to recover its world-space edits.

## Why this follows reservations

Reservations answer **where world intent may exist**. Realization answers **what deterministic world operation that intent becomes**. Keeping those stages separate allows terrain, cave, reservation, persistence, and gameplay systems to participate in planning without making chunk loading part of the decision process.
