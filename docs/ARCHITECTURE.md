# Procedural World — Target Architecture

This document defines the intended long-term architecture for `com.jolybob.proceduralworld`.

The package is being built as a **deterministic world simulation and generation framework**, not as a Tilemap generator. Unity presentation, streaming policy, persistence storage, gameplay systems, and authoring UI sit around a canonical world-data layer.

The design target is a large, persistent 2D world with layered geography, caves, resources, structures, semantic world plans, special points of interest, deterministic generation, chunk streaming, safe player modification, and a customizable visual authoring workflow.

## Architectural principles

The core rules are:

> **World coordinates define the truth; chunks define the execution and storage boundary.**

> **Semantic world intent is planned before world geometry is materialized.**

> **The node graph is an authoring representation, not the runtime world model.**

A world feature must not become a different feature merely because its cells happen to cross a chunk boundary. Large features are therefore represented as world-space placements first and materialized into chunks second.

Likewise, relationships such as `town -> gate -> dungeon` should exist as world-plan intent before they are reduced to geometric proximity or tiles.

The node graph UI is a customization and authoring layer over that plan model. It should be possible to build a world definition visually, create custom node types, define custom ports and constraints, attach custom properties, validate the graph, and preview deterministic results without coupling the runtime generator to the editor UI.

The reusable placement kernel introduced in `0.1.90` is the intended foundation for structures, deposits, rivers, roads, chasms, landmarks, and points of interest. A future world-plan layer composes those primitives without making the placement system responsible for semantic hierarchy.

## Target system graph

```text
                    AUTHORING / CUSTOMIZATION
                              |
                 node graph + inspectors + templates
                              |
                              v
                         WORLD DEFINITION
                              |
                    seed + generation version
                              |
                              v
                    +-----------------------+
                    |   WORLD COORDINATE   |
                    |     / PARAMETERS     |
                    +-----------+-----------+
                                |
          +---------------------+----------------------+
          |                     |                      |
          v                     v                      v
   LARGE-SCALE FIELDS     REGIONS / BIOMES       WORLD FEATURES
          |                     |                      |
   temperature              macro regions        feature definitions
   moisture                 biome zones           placement rules
   elevation                terrain families      structures
   density                  transitions           deposits
   domain masks             plan eligibility       landmarks / POIs
          |                     |                      |
          +---------------------+----------------------+
                                |
                                v
                    DETERMINISTIC WORLD PLAN
                                |
              semantic nodes / ports / constraints
                                |
                                v
                     WORLD LAYOUT / SOLVER
                                |
               placements + semantic connections
                                |
            +-------------------+-------------------+
            |                                       |
            v                                       v
     FEATURE / PATH DATA                    WORLD GRAPH / QUERIES
            |                                       |
            v                              placements / connectivity
     CHUNK MATERIALIZER                            |
            |                                      |
       GeneratedChunk                              |
            |                                      |
        +---+------------------+--------------------+----------------+
        |                      |                    |               |
        v                      v                    v               v
     RENDERING             PERSISTENCE            GAMEPLAY       SIMULATION
     adapters            save overrides          world access   navigation
     Tilemap              chunk storage           edits          encounters
     sprites              world metadata          queries        systems
     ECS/custom           migrations              plans          world logic
        |                      |                    |               |
        +----------------------+--------------------+---------------+
                               |
                               v
                      CHANGE / EVENT LAYER
                               |
                    cell changes + logical batches
                    history + observers + transactions
```

The graph editor belongs above the runtime plan boundary. Runtime generation must never depend on graph-editor view state, selection state, node positions in the editor window, or editor-only object identity.

## Layers

### 1. World definition

The world definition is the authored, versioned description of a world recipe.

It should eventually contain:

- world seed;
- generation version;
- chunk size;
- field configuration;
- macro-region layout;
- region definitions;
- terrain definitions;
- resource definitions;
- structure definitions;
- topology rules;
- feature density and placement rules;
- world-plan rules and optional plan templates;
- semantic node and port definitions;
- layout and corridor rules;
- optional world bounds or special zones;
- references to content catalogs.

The existing `ProceduralWorldDefinitionAsset` is the beginning of this layer. It already authors seed/settings, regions, terrains, and macro layouts. Resource, structure, topology, and broader feature authoring should be added without moving those responsibilities into presentation code.

A world definition should describe **what may exist and what must be true**, while runtime planning chooses one deterministic concrete realization from those rules.

### 2. Deterministic world fields

Fields are pure world-space functions. They should answer questions about a coordinate without depending on which chunk requested the sample.

Examples:

- elevation;
- temperature;
- moisture;
- cave density;
- heat;
- humidity;
- distance to special centers;
- warped domain masks.

The existing `INoiseField`, `IEnvironmentFieldProvider`, and `ICaveFieldProvider` are the correct abstraction direction.

A field should be stable for a fixed `(seed, generation version, world coordinate)` and should not consume mutable shared random state.

Fields describe the physical domain. They should not become the only source of higher-level semantic structure.

### 3. Regions and macro geography

Regions describe **where a world is**. They should not also be responsible for choosing every object that appears there.

The target hierarchy is:

```text
World
  -> macro geography
      -> large zones / radial sectors / authored regions
          -> biome / region identity
              -> terrain families
                  -> local feature rules
                      -> world-plan eligibility
```

`IRegionLayout` separates world-space geography from region content. `RegionLayoutResolver` adapts that layout into the existing resolver contract, allowing older field-based resolvers to continue working.

The current radial-sector system is a prototype for macro geography, not the final representation of all worlds.

### 4. Terrain materialization

Terrain converts region/environment decisions into canonical cell state.

The target responsibility is:

```text
world coordinate
    -> region
    -> terrain rule
    -> terrain identity
    -> base cell topology
```

Terrain selection should remain data-driven and independent of Tilemap assets.

The canonical identifiers are:

- `RegionId`;
- `TerrainId`;
- `ResourceId`;
- `StructureId`.

`GeneratedCell.Biome` and `GeneratedCell.Tile` remain compatibility mirrors for existing users.

Terrain remains a materialization concern. World-plan nodes may constrain which terrain or region they require, but the plan should not directly own tile values.

### 5. World feature planning

Large or multi-cell features are represented as **world-space placements** before any chunk is written.

The shared runtime kernel is:

```text
feature placement definition
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
       +----> chunk A intersection
       +----> chunk B intersection
       +----> chunk C intersection
```

The reusable contracts are:

- `IWorldFeaturePlacementDefinition` — common placement rules such as footprint, spawn chance, owner-chunk density, and origin constraints;
- `WorldFeaturePlacement` — immutable world-space identity and rectangular footprint;
- `WorldFeaturePlacementSet` — deterministic unique placement collection;
- `WorldFeaturePlacementPlanner` — deterministic owner-chunk anchor generation;
- `IWorldFeaturePlacementSource` — chunk queries without moving the placement identity into chunk-local coordinates.

Structures are the first feature type adapted to this kernel. `StructureDefinition` keeps structure-specific content requirements, while `StructurePlacement` is the compatibility/domain adapter and `StructurePlacementPass` remains responsible for local materialization.

This architecture is intentionally generic so deposits, rivers, roads, chasms, landmark complexes, and points of interest can reuse the same ownership and cross-chunk mechanics rather than each inventing a separate chunk-boundary solution.

World features should remain the **geometry primitive**. Semantic composition belongs in the world-plan layer.

### 6. World planning

The world plan is a deterministic intermediate representation between authored rules and concrete world-space placements.

Its purpose is to represent **intent, hierarchy, constraints, customization, and required relationships** before those concepts are lowered into geometry.

The target flow is:

```text
world-plan definition / graph authoring
            |
            v
DeterministicWorldPlanBuilder
            |
            v
       WorldPlan
      /         \
 PlanNodes     PlanConnections
    |                |
 PlanPorts      semantic requirements
    |                |
    +-------+--------+
            |
            v
   WorldPlanLayoutSolver
            |
            +----> WorldFeaturePlacement
            +----> semantic world connections
            +----> world corridors / paths
```

A plan node represents one semantic thing that the world must or may contain. A node may represent a region-scale landmark, dungeon, settlement, room group, entrance, encounter area, or another logical feature without prescribing its final rendering.

Plan nodes may be hierarchical:

```text
WorldPlan
  -> Settlement
      -> TownCenter
      -> District
          -> Building
              -> Room
      -> Gate
  -> Dungeon
      -> Entrance
      -> Wing
          -> Room
```

This hierarchy is intended to support reusable composition at different scales. A larger node may contain child nodes whose concrete geometry is solved only after their own requirements are expanded.

Plan nodes should be deterministic data rather than mutable scene objects. The plan is a generated world-data artifact, not a Unity hierarchy.

### 7. Semantic ports and connections

World-plan relationships should not rely exclusively on spatial proximity.

A semantic connection describes an intended relationship such as:

```text
TownCenter.east_gate -> Road.west_entry
DungeonEntrance.outside -> DungeonCorridor.entry
RoomA.east_door -> RoomB.west_door
```

The target abstraction is a world-space `Port` owned by a plan node, with enough information to express:

- stable port identity;
- direction or orientation;
- semantic type;
- required vs optional status;
- compatibility constraints;
- cardinality where needed;
- eventual world-space position.

Connections should be able to express both required and optional relationships.

```text
Required
--------
Must resolve during plan validation.

Optional
--------
May resolve depending on deterministic selection.

Derived
-------
May be inferred later from world-space proximity or graph analysis.
```

The important distinction is:

```text
WORLD PLAN
semantic intent
      |
      v
GEOMETRIC PLACEMENT
world-space realization
      |
      v
CONNECTIVITY GRAPH
spatially derived relations
```

The existing `WorldConnectivityGraph` remains useful for derived graph relationships. It should not become the sole representation of semantic intent.

### 8. Customization model

The authoring architecture should be extensible without requiring the core runtime to know every game-specific node type.

The target customization model is:

```text
Base plan node contract
        |
        +---- custom node type definition
        |          |
        |          +---- display metadata
        |          +---- ports
        |          +---- properties
        |          +---- constraints
        |          +---- child rules
        |          +---- planner behavior / adapter
        |
        +---- custom connection type
        |          |
        |          +---- port compatibility
        |          +---- routing rules
        |
        +---- custom validators
                   |
                   +---- semantic validation
                   +---- layout validation
                   +---- project-specific rules
```

Customization should be data-driven where possible. A game should be able to introduce concepts such as `Settlement`, `BossRoom`, `MineShaft`, `Shrine`, `QuestLocation`, `RiverCrossing`, or `TeleportGate` without forking the generic world-plan infrastructure.

A custom node should be able to define:

- stable node type identity;
- user-facing label and category;
- default and configurable properties;
- input and output port definitions;
- port compatibility rules;
- child-node constraints;
- footprint or placement requirements;
- deterministic planner selection rules;
- optional validation rules;
- lowering behavior into world-space feature or path data.

The customization boundary should not allow editor-only state to influence deterministic generation. The runtime must consume a stable, serialized, versioned representation of the authored node definition.

### 9. Node graph UI

The node graph UI is the primary visual authoring surface for world plans.

Its purpose is to let authors construct and inspect semantic world intent without manually editing runtime objects or encoding the entire design as free-form code.

The target editor is:

```text
                 NODE GRAPH EDITOR
                         |
       +-----------------+------------------+
       |                 |                  |
   node palette      graph canvas       inspectors
       |                 |                  |
 custom types       nodes + edges       properties
 templates          ports + groups      constraints
       |                 |                  |
       +-----------------+------------------+
                         |
                         v
                 SERIALIZED PLAN ASSET
                         |
                graph validation / compile
                         |
                         v
                    WORLD PLAN DATA
                         |
                         v
              deterministic world planner
```

The UI should provide, at minimum:

- a searchable node palette;
- user-configurable node categories;
- drag/drop creation of custom node types;
- visual input/output ports;
- typed port compatibility and connection feedback;
- multi-selection and group organization;
- node comments or labels for authoring clarity;
- property editing through an inspector;
- graph-level settings and scope controls;
- validation messages connected to graph nodes and ports;
- deterministic preview controls for seed and generation version;
- plan compilation or validation before generation;
- graph navigation independent from world coordinates.

The editor should distinguish **graph layout** from **world layout**.

```text
Editor graph position
    !=
world-space position
```

The location of a node on the canvas is presentation data for the authoring workflow. The actual world-space position is produced later by deterministic planning and layout solving.

The UI should also support visualizing generated results without making the preview itself the source of truth:

```text
serialized plan
      |
      v
validation
      |
      v
runtime planner
      |
      v
world-space preview
```

### 10. Graph validation and compilation

The node graph should be validated before it is used as a runtime world-plan definition.

Validation should detect:

- missing node definitions;
- invalid or incompatible port connections;
- required ports without connections;
- invalid cardinality;
- cycles where a node type forbids recursion;
- invalid child composition;
- missing required properties;
- impossible layout constraints;
- unsupported connection types;
- references to deleted or unavailable custom node types.

The graph should then compile to a stable runtime representation.

```text
Editor graph
      |
      v
Graph validator
      |
      v
Plan compiler
      |
      v
Serialized/runtime WorldPlanDefinition
      |
      v
DeterministicWorldPlanBuilder
```

Compilation should normalize editor-only conveniences such as canvas position, selection state, comments, collapsed groups, and visual styling into a compact runtime-safe representation.

### 11. World-plan layout and constraint solving

Once a concrete plan is selected, child nodes and features must be arranged in world coordinates before chunk materialization.

The intended solver responsibilities include:

- satisfy required port alignments;
- respect node footprint and clearance constraints;
- keep forbidden overlaps apart;
- preserve region or terrain eligibility constraints;
- prefer stable deterministic arrangements when multiple solutions exist;
- allow unconstrained nodes to be placed within authored world-space bounds or radial domains;
- produce a final world-space placement for each resolved feature;
- detect unsatisfied requirements before chunk materialization.

A conceptual process is:

```text
required relationships
        |
        v
initial constrained placement
        |
        v
local adjustments / alignment
        |
        v
overlap and clearance resolution
        |
        v
boundary / world-domain normalization
        |
        v
validated world plan
```

A solver may use iterative separation or other deterministic techniques, but it must not rely on global mutable random state. Any tie-breaking randomness must come from an isolated deterministic random domain derived from stable plan identities.

The solver operates in world coordinates. It must not solve separately per chunk.

### 12. World-plan validation and structural completeness

A selected plan should be validated as a semantic object before it becomes authoritative world geometry.

Validation should be able to detect:

- missing required plan nodes;
- unresolved required ports;
- incompatible port types;
- impossible footprint constraints;
- illegal overlaps;
- invalid region or terrain requirements;
- disconnected required components;
- connections that cannot be routed under the current rules;
- recursion or expansion cycles that exceed configured limits.

This creates a useful guarantee:

```text
selected plan
    -> validate intent
    -> solve geometry
    -> validate geometry
    -> materialize
```

The goal is that once a plan is accepted, its required semantic content is not silently lost during chunk generation.

Optional content may still be rejected by deterministic rules; required content should produce an explicit planning failure rather than quietly disappearing.

### 13. World feature and path lowering

World-plan nodes should lower into the existing world-space feature system rather than creating a second chunk-placement mechanism.

The intended lowering is:

```text
PlanNode
   |
   +----> WorldFeaturePlacement
   |
   +----> WorldFeaturePort positions
   |
   +----> WorldCorridor / WorldPath
```

This keeps the responsibilities separate:

- **plan** — what the world intends to contain;
- **placement** — where a concrete feature exists;
- **path/corridor** — how compatible plan ports are spatially connected;
- **chunk materializer** — how those results become local canonical cells.

This also allows roads, tunnels, rivers, and dungeon corridors to share a future world-space path abstraction without coupling them to the Tilemap or a specific chunk.

A future `IWorldCorridorPlanner` should consume semantic or graph connections and return deterministic world-space paths. Path planning should query world traversability and remain independent from chunk residency.

### 14. World feature queries

Feature discovery is a world-level query boundary, separate from chunk generation and renderer residency.

The runtime query model is:

```text
IWorldFeaturePlacementQuerySource
              |
              v
WorldFeaturePlacementIndex
       /          |          \
    chunk       point       area
    query       query       query
```

`WorldFeaturePlacementIndex` caches deterministic placement sets by chunk coordinate and exposes point containment and inclusive world-rectangle intersection queries. Querying does not require constructing the corresponding `GeneratedChunk`.

The index is intentionally session-scoped: generation rules remain the source of truth, while cached query results can be invalidated explicitly when a caller changes the planning environment or replaces the source.

This creates a stable bridge between deterministic generation and future gameplay systems such as landmark lookup, proximity checks, POI discovery, and world simulation.

World-plan queries should eventually follow the same rule: a caller should be able to inspect semantic nodes, ports, or planned relationships without requiring every underlying chunk to be resident.

### 15. World connectivity graph

Connectivity is a world-data layer built over feature placements rather than over resident chunks.

```text
world-space placements
        |
        v
WorldConnectivityGraphBuilder
        |
        v
WorldConnectivityGraph
   /             \
 nodes           edges
```

`WorldConnectivityNode` retains its source `WorldFeaturePlacement`. `WorldConnectivityEdge` represents an undirected world-space relation with deterministic endpoint ordering and squared distance. `WorldConnectivityGraph` exposes neighbor queries and connected-component counting.

`WorldConnectivityGraphBuilder` first orders placements canonically, discovers nearby candidates through a uniform world-space bucket grid, builds a deterministic sparse forest where degree budgets allow, then adds short redundant links. Graph construction consumes no mutable random stream.

The builder can consume an explicit placement list or `WorldFeaturePlacementIndex` over an inclusive world rectangle. This keeps an unbounded procedural world from becoming one implicit graph and lets gameplay ask for connectivity only in the region it currently cares about.

The graph does not itself carve terrain, spawn prefabs, or run pathfinding. It provides stable world-space relations for later points of interest, landmarks, roads, navigation topology, and simulation systems.

The graph is therefore a **derived relationship layer**, while semantic plan connections are **authored/generated intent**. A future reconciliation stage may use both:

```text
required plan connections
          +
spatial connectivity graph
          +
path feasibility
          |
          v
   world-plan reconciliation
```

See [`CONNECTIVITY.md`](CONNECTIVITY.md) for the detailed connectivity contract.

### 16. Generation pipeline

The generation pipeline should remain an ordered transformation of canonical data.

Target order:

```text
1. base world-space fields
2. macro geography / regions
3. terrain
4. plan selection / deterministic world planning
5. plan layout / semantic connection resolution
6. topology / caves
7. large world-feature planning
8. world paths / corridor planning
9. resource/deposit materialization
10. structure / landmark materialization
11. connectivity / post-process reconciliation
12. final canonical chunk
```

The exact ordering of individual passes may vary by game, but the architectural distinction is important:

- **plan selection** decides semantic content;
- **layout** decides world-space geometry;
- **feature planning** decides deterministic feature identities/footprints;
- **materialization** writes only the requested chunk.

Planning may inspect neighboring world coordinates without writing neighboring chunks. The final chunk pass owns only its own materialization.

`WorldGenerationPipeline` should remain composable so games can replace one subsystem without replacing the generator itself.

### 17. Chunk boundary policy

A chunk is an implementation boundary, never a gameplay boundary.

Features may:

- begin outside the requested chunk and enter it;
- end outside the requested chunk;
- cross multiple chunks;
- be discovered from neighboring owner chunks;
- be reconstructed deterministically when a chunk is loaded later;
- be produced by a world plan whose nodes and connections span many chunks.

Plan solving, feature ownership, connectivity, and path planning must remain world-space operations. Chunk-local coordinates appear only when the final materializer writes canonical cells.

Negative chunk coordinates must follow mathematical floor division semantics rather than language truncation semantics.

### 18. Streaming

Streaming decides **which chunks are resident**, not how the world exists.

Target separation:

```text
player / interest sources
          |
          v
     streaming planner
          |
   active chunk set / delta
          |
          v
  persistence-aware loader
          |
          +----> deterministic generation
          +----> saved overrides
          |
          v
     loaded world state
```

The current `ChunkStreamingPlanner`, `WorldChunkStreamingController`, and `WorldPersistentChunkStreamingController` already implement this separation.

Future streaming work should add scheduling, priorities, generation budgets, cancellation, and background-safe work without moving gameplay rules into the streaming controller.

A loaded chunk should never determine which world plan exists. Streaming order must not change plan selection, feature ownership, or semantic connections.

### 19. Persistence

Persistence stores **player-authored divergence from deterministic generation**.

Target model:

```text
 deterministic base world
          +
 sparse persistent overrides
          +
 world metadata / generation version
          =
 persistent world state
```

The existing `WorldChunkPersistenceService` already regenerates a base chunk and compares it with the saved state.

The next architectural requirement is versioned generation metadata. A save should be tied to the generation recipe that produced it, so changing a world algorithm cannot silently reinterpret old saves.

For planned worlds, persistence should continue to store authoritative player changes and world metadata rather than treating the transient plan object as a save-file implementation detail.

### 20. Gameplay world access

Gameplay must interact with an explicit world-access boundary rather than directly modifying chunk storage.

Target responsibilities:

- read loaded cells;
- query world-space state;
- query feature placements and connectivity without coupling to renderer residency;
- inspect world-plan semantics where available;
- mutate loaded state through controlled operations;
- reject operations outside the active-world boundary;
- produce journaled changes.

The current `IWorldChunkAccess` and `WorldEditService` are the intended foundation.

Gameplay should consume stable world-data queries rather than infer semantic world structure from rendered tiles.

### 21. Change tracking and events

World changes are data events, not rendering callbacks.

Target flow:

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

The existing journal, transaction, history, observer, and batch interfaces are aligned with this goal.

Planned semantic state may also need logical change notifications in the future, but those should remain separate from renderer callbacks and should preserve stable world identities.

### 22. Presentation

Presentation is an adapter over canonical world state.

The core package should never require a Tilemap to understand generation.

Target adapters include:

- Unity Tilemap;
- SpriteRenderer grids;
- ECS / Entities;
- custom meshes;
- debug/analysis views;
- network-replica views.

`WorldTilemapRenderer` is one adapter, not the world model.

A renderer should not become the source of truth for world-plan hierarchy, feature identity, or semantic connections.

## Determinism contract

The package should preserve the following contract:

```text
same world definition
+ same seed
+ same generation version
+ same world coordinate
= same generated result
```

For world planning, the contract extends to semantic identity:

```text
same serialized world-plan definition
+ same seed
+ same generation version
+ same plan scope / stable node identity
= same selected plan content
```

Random streams should be isolated by subsystem and stable inputs:

```text
seed + scope + domain + stable identity
```

The generic feature planner keeps placement randomness in the owner's coordinate domain. A structure adapter uses `WorldRandomDomain.Structures`; future feature types should use their own stable random domains so unrelated systems do not perturb one another.

World-plan selection must not consume a shared random stream whose state depends on generation order. Plan nodes, template choices, optional branches, and solver tie-breakers should derive isolated random streams from stable identities.

Editor graph position, selection order, viewport state, comments, and node visual styling must never influence deterministic runtime output.

Connectivity graph construction is deterministic from sorted world-space placements and does not consume mutable random state.

Changing unrelated optional plan content should not silently perturb independently keyed deterministic systems.

## Coordinate contract

World-space is authoritative.

For a chunk size `N`:

```text
worldX = chunkX * N + localX
worldY = chunkY * N + localY
```

For negative coordinates, conversion from world coordinate to chunk coordinate must use floor division:

```text
chunkX = floor(worldX / N)
chunkY = floor(worldY / N)
```

The generic placement source and connectivity spatial index explicitly protect this boundary because world features and graph queries must behave identically across positive and negative coordinate space.

World-plan layout and path planning must obey the same world-space convention. A plan must not acquire different geometry because its nodes happen to be materialized through different chunk requests.

## Authoring contract

Authoring assets describe **data and rules**, not runtime world objects.

The preferred authoring stack is:

```text
World Definition Asset
        |
        +---- world settings / fields
        +---- region / terrain catalogs
        +---- feature definitions
        +---- world-plan graph asset
        |          |
        |          +---- custom node types
        |          +---- semantic ports
        |          +---- custom connections
        |          +---- constraints
        |          +---- templates / subgraphs
        |          +---- validation rules
        |
        +---- connectivity / path rules
```

An authoring asset may select:

```text
world definition
   |
   +-- seed
   +-- generation version
   +-- fields
   +-- region layout
   +-- regions
   +-- terrains
   +-- resources
   +-- structures
   +-- topology
   +-- feature rules
   +-- plan templates / world-plan rules
   +-- semantic port rules
   +-- layout / corridor rules
   +-- connectivity rules
```

A grammar-like representation may be useful for plan authoring because hierarchical rules are concise and reusable, but the runtime representation should remain typed world-plan data rather than a text parser or renderer-facing hierarchy.

The node graph UI should be treated as an authoring adapter over the same serialized plan contract. Other authoring front ends remain valid: direct serialized data, code-generated plans, external generators, or custom tooling can all compile into the same runtime representation.

## Node graph UI architecture

The editor should be divided into four concerns:

```text
GRAPH MODEL
  semantic node definitions
  ports / connections
  properties / constraints
        |
        v
GRAPH VIEW
  canvas
  node widgets
  port widgets
  selection / navigation
        |
        v
INSPECTOR / PALETTE
  custom node creation
  property editing
  templates
  validation feedback
        |
        v
PLAN COMPILER
  normalization
  stable serialization
  runtime validation
```

The graph model and the visual graph should not be the same object graph.

The graph model should own:

- stable node identity;
- node type identity;
- serialized property values;
- stable port identity;
- connection identity and endpoints;
- child hierarchy or subgraph references;
- deterministic configuration;
- authoring metadata that is safe to preserve.

The graph view may own:

- canvas position;
- zoom and pan state;
- selection state;
- fold/collapse state;
- visual theme metadata;
- temporary interaction state.

The graph view must be disposable without changing the generated plan.

### Custom node registration

Custom node types should be discoverable by metadata rather than hard-coded into one editor window.

A target registration model is conceptually:

```text
Node Type Registration
    |
    +-- stable type id
    +-- category / palette path
    +-- display name
    +-- icon / presentation metadata
    +-- port schema
    +-- property schema
    +-- default values
    +-- validation hooks
    +-- compiler / lowering hooks
```

This permits a project to add domain-specific node types without changing generic graph canvas code.

### Templates and subgraphs

The node graph should support reusable templates or subgraphs:

```text
Settlement Template
    |
    +-- TownCenter
    +-- Market
    +-- Residential District
    +-- Gate
    +-- Required Connections
```

A template should have a stable identity and version. Expanding a template must be deterministic and must preserve stable identities for its resulting nodes and ports.

Templates may be nested within templates, but recursion depth and cycle behavior must be explicitly validated.

### Custom properties and inspectors

Custom nodes should expose structured properties rather than making users edit opaque serialized blobs.

Properties may include:

- numeric constraints;
- booleans and enums;
- references to catalogs;
- region/terrain requirements;
- spawn/density controls;
- minimum/maximum distances;
- footprint dimensions;
- connection policy;
- optional child-node counts;
- custom game-specific data.

The inspector should render these schemas without moving their semantics into the UI layer.

### Graph validation UX

Validation should be actionable in the graph UI.

The editor should be able to:

- highlight invalid nodes;
- highlight invalid ports or connections;
- explain missing required connections;
- report unresolved custom node types;
- distinguish warnings from blocking errors;
- preview the deterministic expansion of a template;
- validate a graph before saving or generation.

Validation results should refer to stable graph identities rather than ephemeral editor object references.

### Preview architecture

The authoring UI should support a deterministic preview path:

```text
World Definition Asset
        |
        v
Serialized World Plan
        |
        v
Runtime validation
        |
        v
Deterministic planner
        |
        v
World-space preview data
        |
        +----> plan visualization
        +----> feature visualization
        +----> terrain / region preview
        +----> connectivity preview
```

The preview should be able to show both the semantic graph and its resolved world-space result.

## Package layering target

The package should trend toward these assemblies/modules:

```text
Jolybob.ProceduralWorld.Runtime
    Core data
    Fields
    Regions
    Terrain
    Topology
    Feature planning and queries
    World-plan data and deterministic planning
    Path / corridor planning contracts
    Connectivity / graph data
    Generation
    Streaming contracts
    Persistence contracts

Jolybob.ProceduralWorld.Authoring
    ScriptableObject world definitions
    world-plan / template authoring data
    custom node type metadata
    editor-facing configuration data

Jolybob.ProceduralWorld.Editor
    node graph UI
    node palette
    node inspectors
    graph validation UI
    plan compiler / serialization tooling
    world-generation previews
    plan/layout diagnostics
    deterministic seed inspector

Jolybob.ProceduralWorld.Tilemap
    Unity Tilemap presentation adapter
```

The runtime generation core must not reference the editor graph UI.

The authoring/editor layer may reference runtime plan contracts, catalogs, validators, and preview services, but the dependency direction must not reverse.

## Target roadmap

The architectural sequence is:

```text
FOUNDATION
  |
  +-- canonical world data
  +-- deterministic fields
  +-- regions / terrain
  |
FEATURE SYSTEM
  |
  +-- caves / topology
  +-- resource deposits
  +-- generic world feature placement  <-- implemented in 0.1.90
  +-- world-space structure placement  <-- implemented in 0.1.87 / 0.1.90 adapter
  |
WORLD SCALE
  |
  +-- deterministic cross-chunk feature queries  <-- implemented in 0.1.91
  +-- connectivity / graph passes  <-- implemented in 0.1.92
  +-- world-plan data model
  +-- hierarchical plan expansion
  +-- semantic ports / required connections
  +-- custom node definitions
  +-- reusable templates / subgraphs
  +-- deterministic plan layout / validation
  +-- world-space paths / corridors
  +-- points of interest / landmarks built from plans
  |
AUTHORING
  |
  +-- world-definition inspector
  +-- customizable node palette
  +-- node graph UI
  +-- custom node registration
  +-- custom port / connection schemas
  +-- property inspectors
  +-- template / subgraph authoring
  +-- graph validation / compilation
  +-- plan/world-space preview
  |
RUNTIME
  |
  +-- budgeted generation
  +-- asynchronous/background-safe generation
  +-- prioritized streaming
  |
PERSISTENCE
  |
  +-- generation versioning
  +-- migration strategy
  +-- persistent world metadata
  |
PRESENTATION
  |
  +-- Tilemap
  +-- ECS/custom renderers
  +-- batched invalidation
```

## Architectural synthesis: field-driven worlds plus semantic planning

The long-term architecture intentionally combines two complementary procedural techniques.

### Field-driven generation

World fields are good at producing continuous, locally queryable properties such as elevation, temperature, moisture, cave density, and macro-region membership. They scale naturally to unbounded world coordinates and chunk streaming.

### Plan-driven generation

World plans are good at expressing intentional structure: hierarchy, required relationships, semantic entrances, guaranteed landmarks, settlements, dungeon layouts, and other content whose correctness depends on several features being generated together.

### Graph-driven authoring

The visual node graph is the authoring surface that makes plan-driven generation customizable. The graph should describe **semantic intent**, not final tile geometry.

The three layers should meet like this:

```text
FIELD DOMAIN
continuous world properties
        |
        +----------------------+
        |                      |
        v                      v
region / terrain       plan eligibility / constraints
        |                      |
        +----------+-----------+
                   |
                   v
            GRAPH-AUTHORED PLAN
                   |
            graph compilation
                   |
                   v
        deterministic world plan
                   |
             layout / solver
                   |
                   v
        world-space placements
                   |
        +----------+----------+
        |                     |
        v                     v
  path / corridor         connectivity
        |                     |
        +----------+----------+
                   |
                   v
          chunk materialization
```

This separation makes the editor highly customizable while keeping runtime generation deterministic, streamable, and independent from Unity editor state.

## What is intentionally out of scope for the core

The core runtime should not own:

- player input;
- combat;
- inventory;
- quests;
- UI;
- prefab instantiation policy;
- sprite selection policy;
- network transport;
- save-file format details for a specific game;
- MonoBehaviour orchestration for a particular project;
- editor canvas state or graph-window state;
- a single mandatory authoring syntax.

Those systems consume the package through stable interfaces.

The core should also not require one particular authoring frontend. Text grammars, ScriptableObjects, code-generated definitions, node-graph assets, or external tools are representations of authoring intent; the runtime plan contract is the stable boundary.

## Architectural success criteria

The target architecture is considered healthy when a project can:

1. generate the same world from the same definition and seed regardless of load order;
2. stream chunks in any order without changing their contents;
3. generate a feature that crosses chunk boundaries without special-case chunk logic;
4. replace Tilemap presentation without rewriting generation;
5. change one generation subsystem without perturbing unrelated deterministic random streams;
6. persist player edits without storing the entire procedural world;
7. load an old world using the generation version it was authored against;
8. query and connect world features without requiring those chunks to be resident;
9. represent intentional multi-feature world structure as a deterministic plan before materialization;
10. express required semantic connections independently from derived proximity edges;
11. validate a selected plan before it becomes authoritative world geometry;
12. solve large features and corridors in world space rather than one chunk at a time;
13. define custom semantic node types without modifying generic graph-editor or runtime infrastructure;
14. define custom ports, connections, properties, and validators through stable registration contracts;
15. author reusable plan templates or subgraphs with deterministic expansion and versioning;
16. edit graph layout and visual styling without changing runtime generation results;
17. preview semantic graph structure and resolved world-space geometry from the same serialized plan;
18. preview and inspect world generation from authoring tools without requiring gameplay systems.

## Current implementation status

Implemented foundations include:

- canonical generated-cell state with region/terrain/resource/structure identifiers;
- deterministic scalar, environment, and cave field providers;
- region catalogs and world-space macro-region layouts;
- terrain catalogs and topology passes;
- clustered deterministic resource deposits;
- generic world-space feature placement;
- cross-chunk structure placement using world-space ownership;
- cached point and rectangle feature queries;
- deterministic world connectivity graphs;
- chunk streaming and persistence-aware streaming;
- world access, controlled editing, transactions, journals, history, and change observers;
- Unity Tilemap presentation adapters;
- optional Unity authoring through `ProceduralWorldDefinitionAsset`.

The world-plan, semantic-port, deterministic layout, world-corridor, customization, and node-graph UI layers described above are the next architectural expansion rather than current runtime commitments. They should reuse the existing feature placement, query, connectivity, and generation boundaries instead of introducing parallel chunk-local systems.
