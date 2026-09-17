# Procedural World — Target Architecture

This document defines the intended long-term architecture for `com.jolybob.proceduralworld`.

The package is a **deterministic world simulation and generation framework**, not a Tilemap generator. Unity presentation, streaming policy, persistence storage, gameplay systems, and authoring UI sit around a canonical world-data layer.

The design target is a large, persistent 2D world with layered geography, caves, resources, structures, semantic world plans, points of interest, deterministic generation, chunk streaming, safe player modification, and a customizable visual authoring workflow.

## Architectural principles

The core rules are:

> **World coordinates define the truth; chunks define the execution and storage boundary.**

> **Semantic world intent is planned before world geometry is materialized.**

> **The node graph is an authoring representation, not the runtime world model.**

Large features are represented as world-space placements first and materialized into chunks second. Relationships such as `town -> gate -> dungeon` exist as semantic plan intent before being reduced to geometry or tiles.

The node graph is a customization layer over the plan model. Its canvas arrangement is presentation metadata; stable node, port, and connection identities are semantic data.

## Target system graph

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
          +-------------------+-------------------+
          |                                       |
          v                                       v
   REGIONS / TERRAIN                         WORLD PLAN GRAPH
          |                               nodes / ports / edges
          |                                       |
          |                                       v
          |                              PLAN COMPILER / VALIDATOR
          |                                       |
          +-------------------+-------------------+
                              |
                              v
                     WORLD PLAN / INTENT
                              |
                              v
                 WORLD LAYOUT / CONSTRAINTS
                              |
             +----------------+----------------+
             |                                 |
             v                                 v
     FEATURE PLACEMENTS                    WORLD PATHS
             |                              / CORRIDORS
             +----------------+----------------+
                              |
                              v
                      CONNECTIVITY / QUERIES
                              |
                              v
                     CHUNK MATERIALIZER
                              |
                              v
                       GENERATED CHUNKS
                              |
            +-----------------+------------------+
            |                 |                  |
            v                 v                  v
        RENDERING         PERSISTENCE        GAMEPLAY
        adapters          + overrides        + simulation
                                               + queries
                              |
                              v
                      CHANGE / EVENT LAYER
```

The architecture combines:

- **field-driven generation** for continuous properties such as elevation, temperature, moisture, caves, and macro geography;
- **plan-driven generation** for intentional structure such as settlements, dungeons, landmarks, semantic entrances, and required relationships.

Both systems meet at the world-space boundary.

## 1. World definition

The world definition is the authored, versioned description of a world recipe.

It should eventually contain world identity, generation version, chunk size, field configuration, macro geography, region/terrain definitions, resources, structures, topology rules, feature placement rules, world-plan templates, custom node schemas, semantic port rules, connectivity rules, optional world bounds, and content catalog references.

`ProceduralWorldDefinitionAsset` is the existing Unity-authored world-definition foundation. `WorldPlanGraphAsset` is the semantic graph authoring boundary.

The authoring layer describes **what may exist and what must be true**. Runtime compilation and planning choose a deterministic realization from those rules.

## 2. Deterministic world fields

Fields are pure world-space functions. They answer questions about coordinates without depending on which chunk requested the sample.

Examples include elevation, temperature, moisture, cave density, heat, humidity, distance to special centers, and warped domain masks.

A field must be stable for a fixed `(seed, generation version, world coordinate)` and must not consume mutable shared random state.

## 3. Regions and macro geography

Regions describe where a world is without owning every object placed inside it.

```text
World
  -> macro geography
      -> large zones / radial sectors / authored regions
          -> biome / region identity
              -> terrain families
                  -> local feature rules
                      -> world-plan eligibility
```

`IRegionLayout` separates world-space geography from region content. The current radial-sector system is one macro-geography strategy, not the final representation of all worlds.

## 4. Terrain materialization

Terrain converts region/environment decisions into canonical cell state.

```text
world coordinate
    -> region
    -> terrain rule
    -> terrain identity
    -> base cell topology
```

Terrain remains data-driven and independent from Tilemap assets. The canonical identifiers are `RegionId`, `TerrainId`, `ResourceId`, and `StructureId`.

World-plan nodes may constrain region/terrain eligibility, but the graph does not own tile values.

## 5. World feature planning

Large or multi-cell features are represented as world-space placements before chunks are written.

```text
feature definition
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
       v
IWorldFeaturePlacementSource
       |
       +----> relevant chunk intersections
```

This generic kernel is the geometry primitive for structures and future deposits, rivers, roads, chasms, landmarks, and other large features.

World features remain geometry primitives. Semantic composition belongs to the world-plan layer.

## 6. World-plan graph runtime

The world plan is the deterministic intermediate representation between authored graph rules and world-space geometry.

The 0.1.94 runtime foundation provides typed graph data:

```text
WorldPlanGraphDefinition
   |
   +-- node type schemas
   |     +-- ports
   |     +-- property schema
   |     +-- footprint metadata
   |
   +-- node instances
   |     +-- stable IDs
   |     +-- properties
   |
   +-- semantic connections
         +-- source node/port
         +-- target node/port
         +-- required / optional / derived
```

The runtime model includes `WorldPlanProperty`, `WorldPlanPortDefinition`, `WorldPlanNodeTypeDefinition`, `WorldPlanNodeDefinition`, `WorldPlanConnectionDefinition`, `WorldPlanGraphDefinition`, `WorldPlanNode`, `WorldPlanConnection`, and `WorldPlan`.

The current runtime plan is flat. Hierarchical templates/subgraphs remain a later expansion stage.

## 7. Custom node types and semantic ports

Node types are reusable schemas rather than hardcoded gameplay classes.

A node type can define:

- stable type ID;
- display metadata/category;
- minimum width and height;
- minimum clearance;
- input, output, or bidirectional ports;
- semantic port types;
- required ports;
- single- or multi-connection policy;
- custom property keys.

This allows a game to define nodes such as `Settlement`, `District`, `Building`, `Dungeon`, `Room`, `Entrance`, `RiverSource`, `EncounterArea`, or `Landmark` without modifying the core runtime model.

## 8. Semantic connections

Plan connections express semantic intent independently of spatial proximity.

```text
Settlement.gate -> Road.entry
Dungeon.outside -> Corridor.entry
RoomA.eastDoor -> RoomB.westDoor
```

Connections are classified as:

```text
Required
--------
The selected plan is invalid without the relationship.

Optional
--------
The relationship may exist after deterministic plan selection.

Derived
-------
The relationship may be inferred later by spatial or graph analysis.
```

This preserves the distinction between authored intent and derived connectivity.

## 9. Plan compiler and validation

`WorldPlanCompiler` is the semantic correctness boundary before layout.

The compiler performs canonical ordering of node types, nodes, and connections so serialized collection order does not change the runtime plan.

The validator currently detects null records, duplicate identities, unknown node types, missing nodes/ports, invalid direction combinations, incompatible semantic types, duplicate endpoint relationships, required unconnected ports, connection multiplicity violations, and undeclared properties as warnings.

An important invariant is that malformed graphs produce validation issues rather than throwing due to missing referenced node types.

The intended flow is:

```text
authored graph
    -> validate
    -> deterministic compile
    -> plan expansion / layout
    -> world-space geometry
```

## 10. Unity node graph authoring UI

`WorldPlanGraphAsset` is the Unity serialization boundary for graph authoring.

The current editor is available under:

**Window > Procedural World > World Plan Graph**

or through **Open Node Graph** on a selected graph asset.

The current GraphView-based editor supports:

- creating nodes from custom node types;
- moving nodes;
- rendering typed input/output ports;
- compatible-port filtering by direction and semantic type;
- creating connections;
- deleting nodes and connections;
- graph validation;
- framing/navigation;
- Undo and asset persistence.

Canvas positions are stored only as visual authoring metadata. `WorldPlanGraphAsset.BuildDefinition()` intentionally excludes those positions from runtime graph compilation.

The Editor assembly references Authoring and Runtime. Runtime world-plan classes do not reference UnityEditor or GraphView.

## 11. Plan layout and constraint solving

The next runtime layer is deterministic world-space layout.

The intended `WorldPlanLayoutSolver` responsibilities are:

- satisfy required port alignment;
- respect footprint and clearance constraints;
- prevent forbidden overlaps;
- enforce region/terrain eligibility;
- position unconstrained nodes inside authored world domains;
- use deterministic tie-breaking;
- produce world-space node and port positions;
- report unsatisfied constraints explicitly.

The solver operates globally in world coordinates and must not solve independently per chunk.

## 12. Hierarchical templates and subgraphs

The target authoring workflow supports reusable graph templates/subgraphs:

```text
Settlement
  -> District*
      -> Building*
          -> Room*
```

A future deterministic expander should derive child identities from parent identity plus stable template identity, preserve semantic connection identities, enforce recursion limits, and flatten the result into the same runtime `WorldPlan` model.

## 13. World-space paths and corridors

Semantic connections eventually lower to world-space paths.

```text
PlanConnection
      |
      v
source/target port world positions
      |
      v
world traversability query
      |
      v
deterministic world path / corridor
      |
      v
chunk-local materialization
```

A future `IWorldCorridorPlanner` should support roads, dungeon corridors, tunnels, river segments, and similar systems without coupling them to Tilemap or a particular chunk.

## 14. Feature queries and connectivity

`WorldFeaturePlacementIndex` provides world-level point, area, and chunk feature queries without requiring resident `GeneratedChunk` data.

`WorldConnectivityGraph` provides deterministic derived relationships over world-space feature placements.

The intended reconciliation is:

```text
semantic plan requirements
          +
actual feature placements
          +
spatial connectivity
          +
path feasibility
          |
          v
world-plan reconciliation
```

A proximity edge does not automatically satisfy a required semantic connection.

## 15. Generation pipeline

Target order:

```text
1. base world-space fields
2. macro geography / regions
3. terrain
4. semantic plan selection / expansion
5. plan validation
6. world-plan layout / constraint solving
7. topology / caves
8. large world-feature planning
9. world path / corridor planning
10. resource/deposit materialization
11. structure / landmark materialization
12. connectivity / post-process reconciliation
13. final canonical chunk
```

The exact pass order can vary by game, but semantic selection, world-space planning, feature ownership, path planning, and final chunk materialization remain separate responsibilities.

## 16. Chunk boundary policy

A chunk is an implementation boundary, never a gameplay boundary.

Features and plans may cross any number of chunks and may be discovered from neighboring owner chunks. Chunk-local coordinates appear only during final materialization.

Negative world coordinates use mathematical floor division.

Plan selection, feature ownership, connectivity, and path planning must remain independent of chunk load order.

## 17. Streaming

Streaming decides which chunks are resident, not what the world is.

```text
interest sources
      -> streaming planner
      -> active chunk set
      -> persistence-aware loader
      -> deterministic generation + saved overrides
      -> loaded world state
```

Future work adds priorities, generation budgets, cancellation, and background-safe execution.

## 18. Persistence

Persistence stores player-authored divergence from deterministic generation.

```text
deterministic base world
        +
sparse persistent overrides
        +
world metadata / generation version
        =
persistent world state
```

Authoring graphs are definitions, not the save format. Stable runtime identities and generation-version metadata form the bridge to persistent worlds.

## 19. Gameplay world access

Gameplay uses explicit world-data services for reads, world-space queries, feature/connectivity queries, controlled mutation, and journaled changes.

Gameplay should not infer semantic world structure from rendered tiles.

## 20. Change tracking and events

World changes are data events, not rendering callbacks.

```text
world edit / simulation mutation
            |
            v
     canonical cell change
            |
            v
        change journal
            |
      +-----+------+
      |            |
      v            v
    history      observers
                    |
       +------------+------------+
       |            |            |
       v            v            v
    rendering    networking    gameplay systems
```

Future plan-level logical events should use stable semantic IDs and remain separate from renderer callbacks.

## 21. Presentation

Presentation is an adapter over canonical world state.

The core package never requires a Tilemap. Target adapters include Tilemap, SpriteRenderer grids, ECS/Entities, custom meshes, debug views, and network replicas.

## 22. Authoring and customization contract

The authoring layer is intentionally customizable:

```text
World Definition
   |
   +-- identity / generation version
   +-- fields
   +-- geography
   +-- region / terrain rules
   +-- topology
   +-- resources / structures
   +-- World Plan Graph
          |
          +-- custom node types
          +-- custom properties
          +-- custom semantic ports
          +-- required / optional connections
          +-- templates / subgraphs
   +-- connectivity rules
```

Text grammars, ScriptableObjects, code-generated definitions, and future external authoring tools are representations of the same semantic intent. The stable runtime boundary is the typed world-plan graph.

## 23. Package layering target

```text
Jolybob.ProceduralWorld.Runtime
    Core data
    Fields
    Regions
    Terrain
    Topology
    Feature planning and queries
    World-plan data / compiler / validation
    Path / corridor planning contracts
    Connectivity / graph data
    Generation
    Streaming contracts
    Persistence contracts

Jolybob.ProceduralWorld.Authoring
    ScriptableObject world definitions
    WorldPlanGraphAsset
    graph authoring data
    configuration data

Jolybob.ProceduralWorld.Tilemap
    Unity Tilemap presentation adapter

Jolybob.ProceduralWorld.Editor
    WorldPlanGraphWindow
    graph editing UI
    inspectors
    preview / diagnostics tooling
```

Runtime remains independent from Editor and Tilemap.

## 24. Determinism contract

Ordinary generation:

```text
same world definition
+ same seed
+ same generation version
+ same world coordinate
= same generated result
```

Plan compilation:

```text
same graph definition
+ same plan seed
+ same stable node / port / connection IDs
= same runtime plan ordering and relationships
```

Editor viewport, node positions, selection state, comments, zoom, and visual styling are not generation inputs.

Future plan expansion and layout tie-breaking must use isolated deterministic random domains derived from stable semantic identities.

## 25. Coordinate contract

World-space is authoritative.

For chunk size `N`:

```text
worldX = chunkX * N + localX
worldY = chunkY * N + localY
```

For negative coordinates:

```text
chunkX = floor(worldX / N)
chunkY = floor(worldY / N)
```

All feature, graph, plan, path, and chunk systems must preserve this convention.

## 26. Target roadmap

```text
FOUNDATION
  + canonical world data
  + deterministic fields
  + regions / terrain

FEATURE SYSTEM
  + caves / topology
  + resource deposits
  + generic world feature placement
  + cross-chunk structure placement

WORLD SCALE
  + feature queries / caching
  + connectivity / graph generation
  + typed world-plan graph foundation              <-- implemented 0.1.94
  + customizable node / port authoring UI          <-- implemented 0.1.94
  + hierarchical plan templates / subgraphs
  + deterministic plan expansion / selection
  + deterministic plan layout / constraints
  + feature-placement lowering
  + world-space paths / corridors
  + points of interest / landmarks from plans

RUNTIME
  + generation scheduling and budgets
  + asynchronous/background-safe generation
  + prioritized streaming

PERSISTENCE
  + generation versioning
  + migration strategy
  + persistent world metadata

PRESENTATION / TOOLING
  + richer graph-side inspectors and palettes
  + resolved-world preview
  + graph diagnostics
  + region / feature / connectivity previews
  + deterministic seed inspector
```

## 27. Current implementation status

Implemented in 0.1.94:

- typed runtime world-plan graph model;
- customizable node type schemas;
- semantic input/output/bidirectional ports;
- required/optional/derived connection kinds;
- node properties;
- deterministic compilation and canonical ordering;
- semantic validation;
- Unity `WorldPlanGraphAsset`;
- Unity GraphView editor with node creation, movement, compatible-port filtering, connections, deletion, validation, framing, Undo, and persistence;
- Editor-to-Authoring assembly dependency boundary.

Still planned:

- hierarchical template/subgraph expansion;
- deterministic alternative selection / grammar rules;
- world-space layout and constraint solving;
- semantic port world positions;
- lowering plans into `WorldFeaturePlacement` instances;
- world-space path/corridor planning;
- direct `ProceduralWorldGenerator` plan integration;
- richer graph-side property and connection inspectors;
- resolved world geometry previews;
- generation scheduling/background-safe execution;
- generation-versioned persistence.

## 28. Architectural success criteria

The target architecture is healthy when a project can:

1. generate the same world from the same definition and seed regardless of load order;
2. stream chunks in any order without changing their contents;
3. generate features crossing chunk boundaries without special-case chunk logic;
4. replace Tilemap presentation without rewriting generation;
5. change one generation subsystem without perturbing unrelated deterministic random streams;
6. persist player edits without storing the entire procedural world;
7. load an old world using the generation version it was authored against;
8. query and connect world features without requiring resident chunks;
9. represent intentional multi-feature structure as a deterministic semantic plan;
10. define custom node types and semantic ports without modifying core runtime code for each game;
11. edit graph layout without changing deterministic runtime semantics;
12. validate a selected plan before it becomes world geometry;
13. solve large structures and paths in world space rather than one chunk at a time;
14. preview semantic graphs and resolved worlds without requiring gameplay systems;
15. author reusable hierarchical templates for worlds, regions, settlements, dungeons, buildings, and rooms.

## What is intentionally out of scope for the core

The core runtime should not own player input, combat, inventory, quests, UI, prefab instantiation policy, sprite selection policy, network transport, game-specific save formats, or MonoBehaviour orchestration.

The Editor graph window is tooling and depends on Unity editor APIs. It does not become runtime world state.

## Design rationale from grammar-driven generators

Grammar-driven generators demonstrate the value of separating **how a system generates** from **what content a specific grammar requests**. This package adopts that idea through custom node schemas and semantic world-plan graphs.

The runtime representation stays typed and deterministic instead of binding correctness to a text parser, global random state, or editor scene hierarchy.

The goal is to preserve hierarchy, explicit relationships, intentional structure, and reusable templates while retaining the package's world-space, chunk-safe, persistent-world architecture.
