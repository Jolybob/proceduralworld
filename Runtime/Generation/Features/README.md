# World Features

The feature layer provides the reusable world-space placement kernel for large or multi-cell generated features, lightweight spatial queries, and deterministic connectivity graphs.

## Responsibilities

- define deterministic placement rules independent of feature-specific content
- create immutable world-space feature identities
- collect unique placements without chunk-local identity drift
- discover placements from deterministic owner chunks
- support footprints that cross chunk boundaries
- expose placement queries without requiring chunk materialization
- cache queried chunks so repeated gameplay queries do not rerun feature planners
- build deterministic connectivity relations between world features
- keep feature planning, queries, and connectivity independent from rendering and prefabs

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
        +-----+----------------------+-------------------+
        |                            |                   |
        v                            v                   v
IWorldFeaturePlacementSource   IWorldFeaturePlacementQuerySource   WorldConnectivityGraphBuilder
                                     |                   |
                                     v                   v
                         WorldFeaturePlacementIndex    WorldConnectivityGraph
```

`IWorldFeaturePlacementSource` is the generation-facing contract and may inspect the full `WorldGenerationContext`.

`IWorldFeaturePlacementQuerySource` is the lightweight world-query contract. It receives only seed, chunk size, and chunk coordinate, so deterministic feature discovery can happen without constructing or regenerating a `GeneratedChunk`.

`WorldFeaturePlacementIndex` caches the query result for each requested chunk. It supports chunk intersection queries, point containment queries, inclusive world-space rectangle queries, deterministic de-duplication, and explicit invalidation.

## Connectivity

`WorldConnectivityGraphBuilder` turns feature placements into a sparse, deterministic world-space graph. Input order does not affect node IDs or edge ordering.

The builder uses:

- uniform world-space spatial buckets for bounded candidate discovery;
- deterministic distance ordering;
- a sparse forest pass for component backbones;
- a second pass for redundant short links;
- a maximum connection distance;
- a maximum connections-per-node budget.

`WorldConnectivityGraph` exposes nodes containing the original `WorldFeaturePlacement`, canonical undirected edges, neighbor queries, and connected-component counting.

The graph can also be built directly from `WorldFeaturePlacementIndex` over an explicit world-space rectangle. This makes connectivity independent from renderer residency and keeps an unbounded procedural world from being accidentally treated as one graph.

```csharp
var graph = new WorldConnectivityGraphBuilder().Build(
    featureIndex,
    new WorldPosition(-256, -256),
    new WorldPosition(256, 256),
    new WorldConnectivitySettings(96, 3));
```

The graph layer does not carve terrain, instantiate prefabs, or perform pathfinding. It supplies stable world-space relations for later POI, landmark, road, navigation, and gameplay systems.

## Spatial semantics

All positions remain in world coordinates. Negative coordinates use mathematical floor division. Spatial bucket arithmetic and squared-distance calculations are protected against integer wraparound at the supported world limits.

## Structure adapter

Structures are the first feature type adapted to the generic kernel. `StructureDefinition` implements the shared placement-definition contract while retaining structure-specific region/terrain requirements. The existing `StructurePlacement`, `IStructurePlacementSource`, and `StructurePlacementPass` APIs remain available for compatibility and materialization.

The owner chunk is the deterministic authority for creating a placement. Any chunk whose world-space rectangle intersects that placement may materialize its local portion. Querying and connectivity remain read-only and do not imply that the target chunk is resident.
