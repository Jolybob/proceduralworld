# World Features

The feature layer provides the reusable world-space placement kernel for large or multi-cell generated features.

## Responsibilities

- define deterministic placement rules independent of feature-specific content
- create immutable world-space feature identities
- collect unique placements without chunk-local identity drift
- discover placements from deterministic owner chunks
- support footprints that cross chunk boundaries
- expose placement queries without requiring chunk materialization
- cache queried chunks so repeated gameplay queries do not rerun feature planners
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
        +-----+----------------------+
        |                            |
        v                            v
IWorldFeaturePlacementSource   IWorldFeaturePlacementQuerySource
                                     |
                                     v
                         WorldFeaturePlacementIndex
```

`IWorldFeaturePlacementSource` is the generation-facing contract and may inspect the full `WorldGenerationContext`.

`IWorldFeaturePlacementQuerySource` is the lightweight world-query contract. It receives only seed, chunk size, and chunk coordinate, so deterministic feature discovery can happen without constructing or regenerating a `GeneratedChunk`.

`WorldFeaturePlacementIndex` caches the query result for each requested chunk. It supports:

- chunk intersection queries;
- point containment queries in world coordinates;
- inclusive world-space rectangle queries;
- deterministic de-duplication when one placement is discovered through multiple chunks;
- explicit per-chunk invalidation and full cache clearing.

For structures, a query index can be built from the generic deterministic source using the structure catalog:

```csharp
var source = new DeterministicWorldFeaturePlacementSource(
    structures.Definitions,
    WorldRandomDomain.Structures);
var index = new WorldFeaturePlacementIndex(
    seed,
    chunkSize,
    source);
```

Feature-specific systems should add only their own content and materialization rules. Structures are the first adapter and continue to expose their existing `StructurePlacement` and `IStructurePlacementSource` APIs.

The owner chunk is the deterministic authority for creating a placement. Any chunk whose world-space rectangle intersects that placement may materialize its local portion. Querying is read-only and does not imply that the target chunk is resident.
