# Procedural World for Unity

A modular, deterministic 2D procedural-world framework for Unity 6.

`com.jolybob.proceduralworld` is designed around one architectural rule:

> **World coordinates define the truth; chunks define the execution and storage boundary.**

A second rule follows from that boundary:

> **Semantic world intent is planned before world geometry is materialized.**

The package is intended for large, persistent 2D worlds where terrain, caves, resources, structures, topology, world plans, streaming, persistence, gameplay access, connectivity, and presentation remain separate systems.

## Architecture

```text
                 AUTHORING / CUSTOMIZATION
                           |
              node graph + inspectors + templates
                           |
                           v
                    WORLD DEFINITION
                           |
                           v
             DETERMINISTIC WORLD FIELDS
                           |
          +----------------+----------------+
          |                                 |
          v                                 v
   REGIONS / TERRAIN                  WORLD PLAN GRAPH
          |                          nodes / ports / edges
          |                                 |
          |                                 v
          |                        WORLD PLAN COMPILER
          |                                 |
          +----------------+----------------+
                           |
                           v
                  WORLD-SPACE REALIZATION
                           |
           +---------------+---------------+
           |                               |
           v                               v
   FEATURE PLACEMENTS                 WORLD PATHS
           |                         / corridors
           +---------------+---------------+
                           |
                           v
                    CHUNK MATERIALIZER
                           |
                           v
                    GENERATED CHUNKS
                           |
          +----------------+----------------+
          |                |                |
          v                v                v
      Rendering       Persistence       Gameplay
      adapters        + overrides       + queries
                           |
                           v
                   CHANGE / EVENT LAYER
```

The detailed target architecture, layering rules, determinism contract, coordinate rules, authoring model, node graph design, and roadmap are documented in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md). World connectivity details are in [`docs/CONNECTIVITY.md`](docs/CONNECTIVITY.md), and the graph implementation contract is in [`docs/WORLD_PLAN_GRAPH.md`](docs/WORLD_PLAN_GRAPH.md).

## Current implementation — 0.1.95

The runtime currently provides:

- deterministic scalar, environment, and cave fields;
- region catalogs and world-space region layouts;
- terrain catalogs and canonical generated-cell state;
- caves, liquids, chasms, and topology passes;
- clustered deterministic resource deposits;
- a generic world-space feature placement kernel;
- cached world-space feature queries independent of chunk materialization;
- a deterministic world connectivity graph over feature placements;
- cross-chunk structure placement built on the generic placement kernel;
- a typed world-plan graph model with node types, semantic ports, connection kinds, and properties;
- deterministic world-plan compilation with canonical ordering and structural validation;
- a Unity-authored `WorldPlanGraphAsset` that separates editor canvas state from runtime graph semantics;
- a Unity node graph editor with custom-port rendering, compatibility filtering, node movement, connection creation/removal, validation, framing, and asset-backed undo/save behavior;
- Unity 6-compatible GraphView editor tooling without dependence on the inaccessible runtime `Toolbar` type;
- deterministic chunk streaming and persistence-aware streaming;
- world access and controlled edit services;
- transactions, change journals, grouped undo/redo history, and change observers;
- a Unity Tilemap presentation adapter;
- an optional Unity authoring assembly with `ProceduralWorldDefinitionAsset`.

The graph foundation is intentionally one step ahead of the final world-plan pipeline: hierarchical expansion, deterministic layout/constraint solving, feature lowering, and world-space corridor generation remain the next runtime increments.

## World-plan graph

The world-plan graph is a typed semantic layer between authored rules and world-space geometry.

```text
node type definition
        |
        +-- semantic ports
        +-- property schema
        +-- footprint / clearance metadata
        |
        v
node instances
        |
        +-- stable node ID
        +-- properties
        +-- editor canvas position
        |
        v
semantic connections
        |
        v
WorldPlanCompiler
        |
        +-- canonical ordering
        +-- node / port resolution
        +-- structural validation
        |
        v
WorldPlan
```

Canvas positions are intentionally not part of the runtime `WorldPlan` definition. Designers can reorganize the editor graph without changing deterministic world semantics.

Ports can be input, output, or bidirectional. Connections are classified as `Required`, `Optional`, or `Derived`. Semantic port types are checked during validation and graph connection filtering.

## Feature and world planning

Large features are represented as immutable world-space placements before any chunk is written:

```text
feature definition
      -> owner-chunk planning
      -> WorldFeaturePlacement
      -> placement index / queries
      -> relevant chunk intersections
      -> local materialization
```

World plans build on that primitive rather than introducing a parallel chunk-local system:

```text
WorldPlan node
      -> feature placement
      -> semantic ports
      -> future world path / corridor
```

The existing connectivity graph remains a derived world-space relationship layer. Semantic plan connections express intent and are validated before geometry is materialized.

## Determinism

For a fixed world definition, seed, generation version, and world coordinate, generation should produce the same result regardless of chunk load order.

World-plan compilation additionally requires stable semantic identity:

```text
same graph definition
+ same generation seed
+ same stable node / port IDs
= same runtime plan ordering and relationships
```

The editor UI is not part of deterministic inputs. Moving a node on the canvas changes authoring metadata only.

Random streams are isolated by subsystem and stable salt. Connectivity graph construction is deterministic from sorted world-space placements and does not consume mutable random state.

## Authoring and customization

Create a world-plan graph asset with:

**Assets > Create > Procedural World > World Plan Graph**

Define node types in the asset inspector. A node type can specify:

- stable type ID and display metadata;
- category;
- minimum footprint and clearance;
- typed input/output/bidirectional ports;
- semantic port types;
- required ports;
- multi-connection policy;
- custom property keys.

Open **Window > Procedural World > World Plan Graph** or press **Open Node Graph** from the asset inspector.

The graph editor supports moving nodes, adding nodes from custom node types, creating compatible connections, deleting nodes/connections, validation, framing, and asset-backed undo/save behavior.

## Runtime boundary

The editor graph does not instantiate GameObjects and does not become a Unity scene hierarchy.

The intended boundary is:

```text
Editor UI
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
   |
   +----> layout / constraint solver
   +----> feature placement lowering
   +----> path / corridor planning
   |
   v
world-space generation
```

The current 0.1.95 implementation reaches the compiler/runtime-plan stage. Layout solving, feature lowering, and corridor planning remain the next runtime increments.

## Streaming and persistence

Streaming controls residency, not world identity.

```text
interest source
     -> streaming planner
     -> load / unload delta
     -> persistence-aware loader
     -> deterministic generator + saved overrides
     -> loaded world state
```

World plans and large feature relationships must remain stable regardless of streaming order.

Persistence stores sparse player-authored divergence from the deterministic base world plus generation metadata, rather than storing the editor graph as live scene state.

## Presentation

The generation core does not require a Tilemap.

`WorldTilemapRenderer` is one presentation adapter. Projects can supply adapters for SpriteRenderers, ECS, custom meshes, debug views, or network replicas.

## Target roadmap

```text
canonical world data
  -> deterministic fields
  -> geography / regions / terrain
  -> caves / topology
  -> resource deposits
  -> generic world feature placement
  -> cross-chunk structure placement
  -> feature queries / caching
  -> connectivity / graph generation
  -> typed world-plan graph foundation       <-- implemented 0.1.94
  -> customizable node/port authoring UI      <-- implemented 0.1.94
  -> Unity 6 GraphView compatibility           <-- implemented 0.1.95
  -> hierarchical plan templates / subgraphs
  -> deterministic plan expansion
  -> deterministic plan layout / constraints
  -> feature-placement lowering
  -> world-space paths / corridors
  -> points of interest / landmarks from plans
  -> generation scheduling and budgets
  -> background-safe generation
  -> generation-versioned persistence
  -> richer authoring / preview / diagnostics
```

## Scope

The runtime package intentionally does not own combat, inventory, quests, UI, player input, prefab orchestration, network transport, or a game-specific save-file format.

The node graph editor is package tooling; the core generation runtime remains independent from the editor and presentation layers.
