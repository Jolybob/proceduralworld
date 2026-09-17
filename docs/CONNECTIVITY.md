# World Connectivity

The connectivity layer turns deterministic world-space feature placements into a graph that can be consumed by gameplay, roads, landmarks, points of interest, and later navigation systems.

## Architectural boundary

Connectivity is a world-data concern, not a chunk-residency or rendering concern.

```text
world feature placements
          |
          v
  WorldConnectivityGraphBuilder
          |
          v
   WorldConnectivityGraph
       /            \
    nodes            edges
      |                |
 feature placement   world-space relation
```

A graph may be built from an explicit placement set or from `WorldFeaturePlacementIndex` over an inclusive world-space rectangle. The resulting graph contains no `GeneratedChunk` references and is valid regardless of which chunks are loaded.

## Determinism

Input placements are sorted by world-space anchor, feature identity, footprint, and owner chunk before node IDs are assigned. Candidate edges are ordered by squared distance and canonical endpoint IDs.

Therefore, the same placement set and connectivity settings produce the same node ordering and edge ordering regardless of source enumeration order.

No mutable random stream is consumed by graph construction. Connectivity is derived entirely from stable world-space inputs.

## Graph construction

`WorldConnectivityGraphBuilder` uses a uniform world-space bucket grid to discover candidate pairs. Each candidate must satisfy the configured maximum connection distance.

The builder performs two deterministic passes:

1. Build a sparse forest where candidate edges join previously disconnected components and both endpoint degree budgets allow the edge.
2. Add additional short links while the maximum degree remains available.

This produces a sparse graph with explicit connected-component information rather than an unordered collection of pairwise proximity tests.

The graph exposes:

- immutable node records containing the source `WorldFeaturePlacement`;
- canonical undirected edges with squared world-space distance;
- neighbor queries;
- connected-component counting;
- mutable-in-session construction followed by read-only list exposure.

## Spatial semantics

All positions remain in world coordinates. Negative coordinates use mathematical floor division when mapped into graph spatial buckets. Bucket-neighbor arithmetic is range-checked so positions near `int.MinValue` and `int.MaxValue` do not wrap into unrelated buckets.

Distance calculations use `long` arithmetic and checked squaring to avoid 32-bit overflow.

## Query integration

The graph builder accepts `WorldFeaturePlacementIndex` directly:

```csharp
var graph = new WorldConnectivityGraphBuilder().Build(
    featureIndex,
    new WorldPosition(-256, -256),
    new WorldPosition(256, 256),
    new WorldConnectivitySettings(96, 3));
```

This makes the intended runtime flow:

```text
world query
    -> cached feature discovery
    -> world-space placements
    -> deterministic connectivity graph
    -> gameplay / POI / road / landmark system
```

The query range is explicit, which keeps an unbounded procedural world from being accidentally treated as one materialized graph.

## Intended future uses

The graph is deliberately content-agnostic. Future systems can add feature-specific meaning without changing the graph kernel:

- connect towns, shrines, entrances, or landmarks;
- generate deterministic road corridors between points of interest;
- classify disconnected regions for world diagnostics;
- seed navigation or pathfinding topology;
- build encounter or biome transition networks;
- drive authored landmark chains.

The graph does not itself carve terrain, spawn prefabs, or run pathfinding. Those systems consume the stable world-space relations it produces.
