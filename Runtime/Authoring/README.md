# Unity world definition authoring

`ProceduralWorldDefinitionAsset` and `WorldPlanGraphAsset` form the optional Unity authoring layer for the procedural world framework.

The authoring layer defines the **world recipe and semantic plan**. It should describe data, rules, graph topology, and constraints rather than runtime presentation objects.

```text
World Definition Asset
        |
        +-- seed
        +-- generation settings
        +-- macro geography / region layout
        +-- region profiles
        +-- terrain profiles
        +-- feature rules
        |
        +-- World Plan Graph Asset
               |
               +-- custom node types
               +-- semantic ports
               +-- node properties
               +-- semantic connections
               +-- editor canvas positions
               |
               v
        WorldPlanCompiler
               |
               v
        deterministic WorldPlan
               |
               v
        ProceduralWorldGenerator / future plan integration
```

The full architectural direction is documented in [`docs/ARCHITECTURE.md`](../../docs/ARCHITECTURE.md), with the graph contract in [`docs/WORLD_PLAN_GRAPH.md`](../../docs/WORLD_PLAN_GRAPH.md).

## Current world definition asset

Create a definition with:

**Assets > Create > Procedural World > World Definition**

The current asset can author:

- deterministic world seed;
- existing `WorldGenerationSettings` values;
- region profiles (`RegionId`, name, description, default terrain);
- terrain profiles (`TerrainId`, name, tile, description);
- threshold or radial-sector macro-region layouts.

Resource, structure, topology, and broader feature authoring remain extensions of this same world-definition boundary.

## World plan graph asset

Create a plan graph with:

**Assets > Create > Procedural World > World Plan Graph**

A `WorldPlanGraphAsset` stores four separate concepts:

1. **Node type definitions** — reusable schemas describing footprint/clearance metadata, category, ports, and allowed properties.
2. **Node instances** — stable node IDs, labels, and properties.
3. **Semantic connections** — stable connection IDs linking typed source and target ports as required, optional, or derived relationships.
4. **Editor canvas positions** — visual layout only; these positions are not compiled into the runtime plan.

This makes the graph useful as a customizable authoring representation without turning editor UI state into generation input.

## Custom node types

A node type can define:

- stable type ID and display name;
- authoring category;
- minimum footprint and clearance;
- input, output, or bidirectional ports;
- semantic port types;
- required ports;
- single- or multi-connection policy;
- custom property keys.

This is intended to support domain-specific schemas such as:

```text
Settlement
TownCenter
District
Building
Dungeon
DungeonRoom
Entrance
RiverSource
EncounterArea
Landmark
```

The runtime does not require those specific names. They are examples of custom node types that a game can define through the authoring asset or future custom editor tooling.

## Node graph editor

Open the graph with:

**Window > Procedural World > World Plan Graph**

or select a `WorldPlanGraphAsset` and press **Open Node Graph** in its inspector.

The current editor supports:

- moving graph nodes;
- adding nodes from registered/custom node types;
- drawing a canvas of typed node ports;
- creating compatible connections by dragging between ports;
- semantic-type connection filtering;
- deleting nodes and connections;
- validating the graph;
- framing the graph view;
- Unity Undo support;
- saving graph edits back to the asset.

Connection types and node properties can also be edited through the asset inspector.

The editor currently uses Unity's GraphView-based editor API. The graph window remains an Editor-only feature and has no runtime dependency.

## Compilation boundary

The graph editor does not directly generate tiles or instantiate prefabs.

```text
Editor graph
    |
    v
WorldPlanGraphAsset
    |
    v
WorldPlanGraphDefinition
    |
    v
WorldPlanCompiler
    |
    v
WorldPlan
```

Compilation performs canonical ordering and structural validation of node types, nodes, ports, properties, and connections.

Validation includes missing node types, duplicate identities, missing nodes/ports, invalid directions, incompatible semantic types, duplicate endpoint relationships, required unconnected ports, and invalid connection multiplicity.

## Determinism boundary

The graph's stable semantic IDs are generation-relevant. The editor canvas is not.

```text
stable graph definition
+ deterministic seed
+ stable node / port / connection identities
= deterministic runtime plan
```

Moving a node from `(100, 100)` to `(900, 40)` in the graph editor must not change the compiled runtime node ordering, connection relationships, or later deterministic generation inputs.

The compiler deliberately receives no GraphView objects and no editor canvas state.

## Runtime integration status

The 0.1.94 increment establishes the graph foundation:

```text
implemented
  WorldPlan graph data
  custom node/port schemas
  compiler
  validation
  Unity graph editor

next
  hierarchical template/subgraph expansion
  deterministic plan selection
  world-space layout / constraint solving
  lowering to WorldFeaturePlacement
  semantic port world positions
  world-space path / corridor planning
  integration into ProceduralWorldGenerator
```

Until those later stages are implemented, the graph is a validated deterministic planning artifact and does not automatically alter chunk generation.

## Separation from presentation

The authoring assembly references only the runtime generation assembly. The Editor assembly references the authoring/runtime APIs so the graph UI can edit assets and invoke validation/compilation.

The intended dependency direction remains:

```text
Editor UI
    -> Authoring data
    -> Runtime compiler/model

Authoring data
    -> Runtime generation

Runtime generation
    -> no Editor dependency
    -> no Tilemap requirement
```

This keeps world definitions and plans reusable with Tilemap, SpriteRenderer, ECS, custom rendering, or headless generation.

## Runtime usage

The existing world definition still produces a generator through `CreateGenerator()`:

```csharp
using Jolybob.ProceduralWorld;
using Jolybob.ProceduralWorld.Authoring;
using UnityEngine;

public sealed class WorldBootstrap : MonoBehaviour
{
    [SerializeField] private ProceduralWorldDefinitionAsset definition;

    private ProceduralWorldGenerator generator;

    private void Awake()
    {
        generator = definition.CreateGenerator();
    }
}
```

The generated world remains data-only. The graph foundation can be compiled independently and will be connected to future planning/materialization stages without introducing scene-object ownership into generation.
