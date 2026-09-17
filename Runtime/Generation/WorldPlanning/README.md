# World Planning Runtime

This module contains the typed semantic world-plan graph, deterministic compiler, reusable hierarchical subgraph expansion, and world-space layout layer.

The plan layer sits between authored semantic intent and world-space feature/materialization systems.

Current runtime types:

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

## Determinism

Expansion and layout both canonicalize their inputs by stable semantic identifiers. Reordering serialized graph lists does not change scoped IDs, node footprints, or port anchors.

Canvas positions and Unity authoring objects are not part of the runtime plan or runtime layout representation.

## Runtime boundary

The runtime planning module has no dependency on `UnityEditor`, GraphView, Tilemap, GameObjects, or scene hierarchies. Authoring assets compile into plain runtime definitions, then into a `WorldPlan`, then into a world-space `WorldPlanLayout`.

The next realization stages can consume the layout without creating a chunk-local planning layer:

```text
WorldPlanLayout
    |
    +----> terrain-aware feasibility
    +----> feature-placement lowering
    +----> world-space corridor/path planning
    |
    v
chunk-local materialization
```
