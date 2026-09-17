# World Features

The feature layer provides the reusable world-space placement kernel for large or multi-cell generated features.

## Responsibilities

- define deterministic placement rules independent of feature-specific content
- create immutable world-space feature identities
- collect unique placements without chunk-local identity drift
- discover placements from deterministic owner chunks
- support footprints that cross chunk boundaries
- keep feature planning independent from rendering and prefabs

## Core contracts

```text
IWorldFeaturePlacementDefinition
              |
              v
WorldFeaturePlacementPlanner
              |
              v
WorldFeaturePlacement
              |
              v
WorldFeaturePlacementSet
              |
              v
IWorldFeaturePlacementSource
```

Feature-specific systems should add only their own content and materialization rules. Structures are the first adapter and continue to expose their existing `StructurePlacement` and `IStructurePlacementSource` APIs.

The owner chunk is the deterministic authority for creating a placement. Any chunk whose world-space rectangle intersects that placement may materialize its local portion.
