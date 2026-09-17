# World Features

The feature layer provides the reusable world-space placement kernel for large or multi-cell generated features, scene/dungeon selection, lightweight spatial queries, and deterministic connectivity graphs.

## Responsibilities

- define deterministic placement rules independent of feature-specific content
- select reusable world scenes from biome/tag pools before placement
- support limited and unique scenes without chunk-order dependence
- create immutable world-space feature identities
- collect unique placements without chunk-local identity drift
- discover placements from deterministic owner chunks
- support footprints that cross chunk boundaries
- expose placement queries without requiring chunk materialization
- cache queried chunks so repeated gameplay queries do not rerun feature planners
- build deterministic connectivity relations between world features
- keep feature planning, queries, connectivity, and scene metadata independent from rendering and prefabs

## Scene catalog

`WorldSceneDefinition` is the runtime contract for a scene, dungeon room, landmark, or other reusable set-piece. It combines the generic placement rules with stable scene identity, biome/tag requirements, weighted selection, optional uniqueness, world-instance limits, and orientation policy.

```text
WorldSceneCatalog
       |
       v
WorldSceneSelector
       |
       +-- enabled / weight
       +-- required biome tags
       +-- consumed unique IDs
       +-- deterministic seed + salt
       |
       v
WorldSceneDefinition
       |
       v
WorldFeaturePlacementPlanner
       |
       v
WorldFeaturePlacement
```

Selection is a world-semantic decision. The selector canonicalizes catalog IDs and derives its weighted draw from the world seed and selection salt, so reordering serialized scene lists does not change the selected scene. Required tags are matched before weighting, allowing the same catalog to serve forest, desert, snow, dungeon, or other biome pools. Unique scenes can be removed from subsequent selection through a persistent consumed-ID set without introducing process-local randomness.

`WorldSceneOrientationMode` records whether the eventual materializer may use fixed, 90-degree rotation, mirroring, or both. The feature kernel remains presentation-free; orientation/stamping is a later materialization concern.

`WorldSceneDefinitionAsset` and `WorldSceneCatalogAsset` expose this metadata through Unity authoring without introducing Tilemap, prefab, or scene-hierarchy dependencies into the runtime catalog.

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

## Spatial semantics

All positions remain in world coordinates. Negative coordinates use mathematical floor division. Spatial bucket arithmetic and squared-distance calculations are protected against integer wraparound at the supported world limits.

## Structure adapter

Structures are the first feature type adapted to the generic kernel. `StructureDefinition` implements the shared placement-definition contract while retaining structure-specific region/terrain requirements. The existing `StructurePlacement`, `IStructurePlacementSource`, and `StructurePlacementPass` APIs remain available for compatibility and materialization.

Scenes now share the same world-space kernel rather than creating a second chunk-local placement system. The owner chunk remains the deterministic authority for creating a placement, while any intersecting chunk may materialize its local portion. Scene selection and uniqueness therefore happen before chunk geometry and renderer residency.
