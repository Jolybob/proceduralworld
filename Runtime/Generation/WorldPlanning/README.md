# World Planning Runtime

This module contains the typed semantic world-plan graph, deterministic compiler, and reusable hierarchical subgraph expansion layer.

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

## Determinism

Expansion sorts types, nodes, connections, and exposed ports by stable ordinal identifiers. Reordering serialized lists does not change scoped identities or the resulting runtime plan.

Canvas positions and Unity authoring objects are not part of the runtime plan representation.

## Runtime boundary

The runtime planning module has no dependency on `UnityEditor`, GraphView, Tilemap, GameObjects, or scene hierarchies. Authoring assets compile into plain runtime definitions, then into `WorldPlan`.
