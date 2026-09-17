# World Plan Subgraphs

`WorldPlanSubgraphTemplateDefinition` adds a hierarchical planning layer above the existing flat world-plan graph.

## Design goal

Large world features are naturally reusable compositions: a dungeon wing contains rooms, rooms contain entrances and connectors, and multiple world locations may instantiate the same semantic pattern. The template layer lets authoring stay hierarchical while runtime execution remains a single deterministic graph.

The architectural rule remains:

> World coordinates define the truth; chunks define the execution and storage boundary.

Subgraphs therefore describe semantic intent only. They do not introduce chunk-local generation, Unity scene objects, or editor-only runtime state.

## Template boundary

A template contains a normal `WorldPlanGraphDefinition` plus an explicit list of exposed ports.

```text
Template: DungeonWing

  Entrance ---> RoomA ---> RoomB ---> Exit

  exposed:
    entrance -> Entrance.in
    exit     -> Exit.out
```

`WorldPlanSubgraphPortDefinition` maps an exposed ID to a concrete internal node/port. Only exposed ports may participate in connections owned by the parent graph.

## Instances

A `WorldPlanNodeDefinition` becomes a template instance by setting `TemplateId`.

```text
Host graph

  Start ---> Wing01 ---> Boss
              |
              +-- expands to DungeonWing

  Start ---> Wing02 ---> Exit
              |
              +-- expands to DungeonWing
```

The instance itself does not survive into the compiled runtime graph. Its contents are copied into a deterministic scope derived from the instance ID.

```text
Wing01/Entrance
Wing01/RoomA
Wing01/RoomB
Wing01/Exit

Wing02/Entrance
Wing02/RoomA
Wing02/RoomB
Wing02/Exit
```

The same template can therefore be instantiated repeatedly without identifier collisions.

## Nested templates

Templates can contain instances of other templates. Expansion is recursive until only ordinary node definitions remain.

```text
World
  -> Dungeon
      -> Wing
          -> RoomCluster
              -> Room
```

Nested scopes follow the instance path:

```text
Dungeon/Wing01/ClusterA/Room03
```

Exposed ports on a parent template can point at a nested template instance's exposed port. The compiler resolves the chain all the way to a concrete internal node/port before returning the flattened graph.

## Determinism contract

Expansion is canonicalized by ordinal stable identifiers:

```text
node types   -> ID
nodes        -> ID
connections  -> source/port/target/port/kind/ID
exposed      -> ID
```

The input order of serialized lists is therefore not semantic. The same graph, seed, stable IDs, and template contents produce the same expanded identities and relationship ordering.

Canvas positions are never consulted by runtime expansion.

## Compilation pipeline

```text
WorldPlanGraphDefinition
          |
          v
WorldPlanSubgraphCompiler.Expand
          |
          +-- resolve template instances
          +-- detect missing templates
          +-- detect duplicate template IDs
          +-- detect template recursion
          +-- recursively expand nested instances
          +-- resolve exposed ports
          +-- scope IDs
          +-- rewrite parent connections
          |
          v
flat WorldPlanGraphDefinition
          |
          v
WorldPlanCompiler
          |
          +-- semantic validation
          +-- canonical ordering
          +-- runtime node/connection creation
          |
          v
WorldPlan
```

This keeps the hierarchical authoring model decoupled from the runtime execution model.

## Validation

Subgraph expansion reports structural errors before the flat compiler runs. The current layer covers:

- null templates;
- duplicate template IDs within a graph;
- references to missing templates;
- duplicate node IDs;
- template recursion;
- missing exposed ports;
- exposed ports that target missing nodes;
- nested exposed-port resolution failures;
- missing connection endpoints.

The final expanded graph is then passed through the existing semantic validation for port direction, semantic type compatibility, required connections, duplicate relationships, and connection multiplicity.

## Unity authoring

`WorldPlanGraphAsset` adds reusable template authoring fields:

- reusable-template metadata;
- stable `TemplateId` and display name;
- exposed port records;
- referenced template assets;
- template-backed graph nodes.

The Unity GraphView editor exposes referenced templates under **Add Subgraph** and renders their exposed ports using the underlying port's semantic direction, type, and connection capacity.

Template reference-cycle exceptions are caught by the editor inspector/window rather than interrupting authoring.

## Runtime usage

For code-authored graphs:

```csharp
WorldPlanSubgraphCompiler compiler = new WorldPlanSubgraphCompiler();
WorldPlanSubgraphExpansionResult expansion = compiler.Expand(definition);

if (expansion.Succeeded)
{
    WorldPlanGraphDefinition flat = expansion.ExpandedDefinition;
}

WorldPlanCompilationResult result = compiler.Compile(seed, definition);
```

For Unity-authored assets, `WorldPlanGraphAsset.Validate()` and `WorldPlanGraphAsset.Compile()` use the hierarchical compiler automatically.

## Relationship to world-space realization

Subgraph expansion is intentionally before layout and placement:

```text
hierarchical semantic plan
          |
          v
flat semantic plan
          |
          v
world-space layout / constraints
          |
          v
feature placement / paths
          |
          v
chunk-local materialization
```

A subgraph therefore has no privileged relationship to chunks. An expanded template may produce a structure spanning multiple chunks, exactly as any other world-space feature does.
