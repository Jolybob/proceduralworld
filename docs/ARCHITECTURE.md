# Procedural World — Target Architecture

This document defines the intended long-term architecture for `com.jolybob.proceduralworld`.

The package is being built as a **deterministic world simulation and generation framework**, not as a Tilemap generator. Unity presentation, streaming policy, persistence storage, and gameplay systems sit around a canonical world-data layer.

The design target is a large, persistent 2D world with layered geography, caves, resources, structures, special points of interest, deterministic generation, chunk streaming, and safe player modification.

## Architectural principle

The core rule is:

> **World coordinates define the truth; chunks define the execution and storage boundary.**

A second rule now follows from that boundary:

> **Semantic world intent is planned before world geometry is materialized.**

A world feature must not become a different feature merely because its cells happen to cross a chunk boundary. Large features are therefore represented as world-space placements first and materialized into chunks second. Likewise, relationships such as `town -> gate -> dungeon` should exist as world-plan intent before they are reduced to geometric proximity or tiles.

The reusable placement kernel introduced in `0.1.90` is the intended foundation for structures, deposits, rivers, roads, chasms, landmarks, and points of interest. A future world-plan layer composes those primitives without making the placement system responsible for semantic hierarchy.

## Target system graph

```text
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
   temperature              region graph        feature definitions
   moisture                 macro regions        placement rules
   elevation                biome zones           structures
   density                  terrain families      deposits
   domain masks             local transitions     landmarks / POIs
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

The shared runtime kernel is now:

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

World features should remain the **geometry primitive**. Semantic composition belongs in the world-plan layer below.

### 6. World planning

The world plan is a deterministic intermediate representation between authored rules and concrete world-space placements.

Its purpose is to represent **intent, hierarchy, constraints, and required relationships** before those concepts are lowered into geometry.

The target flow is:

```text
world-plan definition / rules
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

### 8. World-plan layout and constraint solving

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

### 9. World-plan validation and structural completeness

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

### 10. World feature and path lowering

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

### 11. World feature queries

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

### 12. World connectivity graph

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

### 13. Generation pipeline

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

The important distinction is that **planning may inspect neighboring world coordinates without writing neighboring chunks**. The final chunk pass owns only its own materialization.

`WorldGenerationPipeline` should remain composable so games can replace one subsystem without replacing the generator itself.

### 14. Chunk boundary policy

A chunk is an implementation boundary, never a gameplay boundary.

Features may:

- begin outside the requested chunk and enter it;
- end outside the requested chunk;
- cross multiple chunks;
- be discovered from neighboring owner chunks;
- be reconstructed deterministically when a chunk is loaded later;
- be produced by a world plan whose nodes and connections span many chunks.

Plan solving, feature ownership, connectivity, and path planning must therefore remain world-space operations. Chunk-local coordinates appear only when the final materializer writes canonical cells.

Negative chunk coordinates must follow mathematical floor division semantics rather than language truncation semantics.

### 15. Streaming

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

### 16. Persistence

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

### 17. Gameplay world access

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

### 18. Change tracking and events

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

### 19. Presentation

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
same world definition
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
   +-- connectivity rules
```

A grammar-like representation may be useful for plan authoring because hierarchical rules are concise and reusable, but the runtime representation should remain typed world-plan data rather than a text parser or renderer-facing hierarchy.

At runtime, a definition becomes a generator/configuration graph. Presentation references should remain outside the generation assemblies whenever possible.

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
    world-plan / grammar-like authoring data
    editor-facing configuration data

Jolybob.ProceduralWorld.Tilemap
    Unity Tilemap presentation adapter

Jolybob.ProceduralWorld.Editor
    inspectors
    validation tools
    world-generation previews
    plan / layout diagnostics
```

The goal is to keep the runtime generation core usable by projects that do not use Tilemap, while still shipping a convenient Tilemap adapter.

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
  +-- deterministic plan layout / validation
  +-- world-space paths / corridors
  +-- points of interest / landmarks built from plans
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
  |
TOOLING
  |
  +-- world-definition editor
  +-- region/terrain preview
  +-- feature preview
  +-- plan graph preview
  +-- plan/layout diagnostics
  +-- connectivity diagnostics
  +-- deterministic seed inspector
  +-- generation diagnostics
```

## Architectural synthesis: field-driven worlds plus semantic planning

The long-term architecture intentionally combines two complementary procedural techniques.

### Field-driven generation

World fields are good at producing continuous, locally queryable properties such as elevation, temperature, moisture, cave density, and macro-region membership. They scale naturally to unbounded world coordinates and chunk streaming.

### Plan-driven generation

World plans are good at expressing intentional structure: hierarchy, required relationships, semantic entrances, guaranteed landmarks, settlements, dungeon layouts, and other content whose correctness depends on several features being generated together.

The two systems should meet at the world-space boundary:

```text
continuous world fields
        |
        +----------------------+
        |                      |
        v                      v
 region / terrain       plan eligibility / constraints
        |                      |
        +----------+-----------+
                   |
                   v
            deterministic plan
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

This lets continuous simulation-like world generation and intentionally designed procedural structures coexist without forcing either system to own the responsibilities of the other.

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
- MonoBehaviour orchestration for a particular project.

Those systems consume the package through stable interfaces.

The core should also not require one particular authoring syntax. Text grammars, ScriptableObjects, code-generated definitions, or custom editor tooling are representations of authoring intent; the runtime plan contract is the stable boundary.

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
13. preview and inspect world generation from authoring tools without requiring gameplay systems.

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

The world-plan, semantic-port, deterministic layout, and world-corridor layers described above are the next architectural expansion rather than current runtime commitments. They should reuse the existing feature placement, query, connectivity, and generation boundaries instead of introducing parallel chunk-local systems.
