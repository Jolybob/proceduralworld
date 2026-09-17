# World Planning Runtime

This module contains the typed semantic world-plan graph, deterministic compiler, reusable hierarchical subgraph expansion, world-space layout, runtime lowering, and chunk-facing realization boundary.

The plan layer sits between authored semantic intent and world-space feature/materialization systems.

Current runtime types include:

- `WorldPlanGraphDefinition`
- `WorldPlanNodeTypeDefinition`
- `WorldPlanNodeDefinition`
- `WorldPlanPortDefinition`
- `WorldPlanConnectionDefinition`
- `WorldPlanProperty`
- `WorldPlanSubgraphPortDefinition`
- `WorldPlanSubgraphTemplateDefinition`
- `WorldPlanCompiler`
- `WorldPlanSubgraphCompiler`
- `WorldPlanValidationResult`
- `WorldPlanSubgraphExpansionResult`
- `WorldPlanLayoutSettings`
- `WorldPlanNodeLayout`
- `WorldPlanLayoutPort`
- `WorldPlanLayout`
- `WorldPlanLayoutSolver`
- `WorldPlanFeatureLoweringSettings`
- `WorldPlanFeatureLowerer`
- `WorldPlanFeaturePlacement`
- `WorldPlanRealizer`
- `WorldPlanRuntimeSettings`
- `WorldPlanRuntimeBuilder`
- `WorldPlanRuntime`
- `IWorldPlanChunkGenerator`
- `WorldPlanChunkGeneration`
- `WorldPlan`

## Hierarchical plans

A `WorldPlanNodeDefinition` can reference a `WorldPlanSubgraphTemplateDefinition` through `TemplateId`.

During expansion, template instances are removed and replaced by concrete nodes and connections using deterministic scoped IDs:

```text
Template instance: dungeon
        |
        +-- room_a      -> dungeon/room_a
        +-- room_b      -> dungeon/room_b
        +-- room_a -> room_b
                    -> dungeon/room_a -> dungeon/room_b
```

A template exposes only the internal ports that may cross its boundary through `WorldPlanSubgraphPortDefinition`.

Nested templates are recursively lowered, so a reusable room network can itself contain reusable room clusters or larger dungeon sections.

The expansion layer validates missing templates, duplicate identifiers, invalid exposed ports, and recursive template cycles before delegating the resulting flat graph to the existing semantic compiler.

## World-space layout

`WorldPlanLayoutSolver` converts the compiled flat `WorldPlan` into world-space node footprints and semantic port anchors.

```text
WorldPlan
   |
   v
WorldPlanLayoutSolver
   |
   +-- connected components
   +-- deterministic graph-distance layers
   +-- footprint + clearance packing
   +-- semantic port anchors
   |
   v
WorldPlanLayout
```

The layout is deterministic from stable plan identity and `WorldPlanLayoutSettings`. The solver does not read Unity editor canvas coordinates, chunk residency, or mutable random state.

Each connected component uses its lowest node ID as its root. Breadth-first graph distance determines layers and stable node IDs determine order inside each layer. Disconnected components are packed horizontally with a deterministic component gap.

Node minimum width, height, and clearance are treated as occupied layout constraints. The result therefore provides a stable non-overlapping world-space footprint for later room/structure materialization.

Port anchors are placed on node perimeters using semantic connection direction and stable port ordering. Connected sources use the right side, connected targets use the left side, and unconnected bidirectional ports use the bottom side.

## Runtime plan program

`WorldPlanRuntimeBuilder` is now the authoritative orchestration boundary for plan-driven generation:

```text
WorldPlanGraphDefinition
        |
        v
WorldPlanSubgraphCompiler
        |
        v
WorldPlanCompiler
        |
        v
WorldPlanLayoutSolver
        |
        +----> WorldPlanFeatureLowerer (optional)
        |             |
        |             v
        |        WorldPlanRealizer (optional)
        |             |
        |             v
        +------> WorldRealizationMap
                         |
                         v
                  chunk intersection query
```

The builder executes these stages once per world runtime, rather than recreating semantic planning inside each chunk. Feature lowering remains optional because feature catalogs and realization semantics are application-owned extension points.

`WorldPlanRuntime` indexes realization edits by chunk only as an acceleration structure. World coordinates remain the authoritative identity and no chunk-local copy of the semantic plan is created.

## Chunk-generation integration

`ProceduralWorldGenerator` implements `IWorldPlanChunkGenerator` when supplied with a `WorldPlanRuntime`. The same runtime is also exposed on `WorldGenerationContext` so custom generation passes can inspect the global plan while they materialize a chunk.

The chunk boundary is intentionally explicit:

```text
WorldPlanRuntime
      |
      +-- global plan/layout/realization truth
      |
      v
WorldGenerationContext.WorldPlan
      |
      +-- custom materialization pass
      |
      v
GeneratedChunk
```

Consumers that only have `IWorldChunkGenerator` can use `WorldPlanChunkGeneration.TryCollectRealizationEdits(...)` to detect and query the optional plan capability without coupling to `ProceduralWorldGenerator`.

## Determinism

Expansion, compilation, layout, lowering, realization ordering, and chunk indexing all canonicalize their inputs by stable semantic identifiers and world coordinates. Reordering serialized graph lists does not change scoped IDs, node footprints, port anchors, or realization lookup results.

Canvas positions and Unity authoring objects are not part of the deterministic runtime plan or runtime layout representation.

## Authoring boundary

`ProceduralWorldDefinitionAsset` can now reference a `WorldPlanGraphAsset`. The graph is compiled and laid out with the world definition's seed when the generator is created. Feature resolver/materializer policies remain explicit runtime dependencies rather than hidden Unity presentation state.

## Runtime boundary

The runtime planning module has no dependency on `UnityEditor`, GraphView, Tilemap, GameObjects, or scene hierarchies. Authoring assets compile into plain runtime definitions, then into a `WorldPlan`, then into world-space plan data shared by chunk generation.

The remaining materialization stages can consume the same world-space truth without creating a chunk-local planning layer:

```text
WorldPlanRuntime
    |
    +----> terrain-aware feasibility
    +----> feature placement
    +----> world-space corridor/path planning
    +----> chunk-local materialization
```
