# Procedural World — Target Architecture

This document defines the intended long-term architecture for `com.jolybob.proceduralworld`.

The package is being built as a **deterministic world simulation and generation framework**, not as a Tilemap generator. Unity presentation, streaming policy, persistence storage, and gameplay systems sit around a canonical world-data layer.

The design target is a large, persistent 2D world with layered geography, caves, resources, structures, special points of interest, deterministic generation, chunk streaming, and safe player modification.

## Architectural principle

The core rule is:

> **World coordinates define the truth; chunks define the execution and storage boundary.**

A world feature must not become a different feature merely because its cells happen to cross a chunk boundary. This is why structure placement is now planned in world space and materialized per chunk.

The same principle should eventually apply to other large-scale systems such as resource deposits, rivers, chasms, landmarks, roads, biome borders, and points of interest.

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
   temperature              region graph        feature planners
   moisture                 macro regions        structures
   elevation                biome zones           deposits
   density                  terrain families      chasms / rivers
   domain masks             local transitions     points of interest
          |                     |                      |
          +---------------------+----------------------+
                                |
                                v
                     WORLD GENERATION PLAN
                                |
                 deterministic feature identities
                                |
                                v
                       CHUNK MATERIALIZER
                                |
                         GeneratedChunk
                                |
        +-----------------------+-----------------------+
        |                       |                       |
        v                       v                       v
     RENDERING              PERSISTENCE              GAMEPLAY
     adapters               save overrides           world access
     Tilemap                chunk storage            edits
     sprites                world metadata           simulation
     ECS/custom             migrations               queries
        |                       |                       |
        +-----------------------+-----------------------+
                                v
                       CHANGE / EVENT LAYER
                                |
                    cell changes + logical batches
                                |
                    +-----------+-----------+
                    |                       |
                    v                       v
                 history               observers
                 undo/redo              networking
                 transactions           analytics
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
- optional world bounds or special zones;
- references to content catalogs.

The existing `ProceduralWorldDefinitionAsset` is the beginning of this layer. It already authors seed/settings, regions, terrains, and macro layouts. Resource, structure, topology, and feature authoring should be added without moving those responsibilities into presentation code.

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

### 5. World feature planning

Large or multi-cell features should be represented as **world-space placements** before any chunk is written.

The target abstraction is:

```text
Feature definition
       |
       v
 deterministic planner
       |
       v
 world-space placement(s)
       |
       +----> chunk A intersection
       +----> chunk B intersection
       +----> chunk C intersection
```

This is now the structure architecture:

- `StructurePlacement` is the stable world-space identity;
- `StructurePlacementPlanner` creates deterministic placements;
- `IStructurePlacementSource` discovers placements relevant to a chunk;
- `StructurePlacementPass` materializes only the local intersection;
- `StructurePass` remains as a compatibility wrapper.

Future planners should follow the same model for deposits, rivers, roads, landmark complexes, and other features that can span chunks.

### 6. Generation pipeline

The generation pipeline should remain an ordered transformation of canonical data.

Target order:

```text
1. base world-space fields
2. macro geography / regions
3. terrain
4. topology / caves
5. large world-feature planning
6. resource/deposit materialization
7. structure / landmark materialization
8. connectivity / post-process reconciliation
9. final canonical chunk
```

The important distinction is that **planning may inspect neighboring world coordinates without writing neighboring chunks**. The final chunk pass owns only its own materialization.

`WorldGenerationPipeline` should remain composable so games can replace one subsystem without replacing the generator itself.

### 7. Chunk boundary policy

A chunk is an implementation boundary, never a gameplay boundary.

Features may:

- begin outside the requested chunk and enter it;
- end outside the requested chunk;
- cross multiple chunks;
- be discovered from neighboring owner chunks;
- be reconstructed deterministically when a chunk is loaded later.

The materializer must therefore reason in world coordinates and convert to local chunk coordinates only at the final write step.

Negative chunk coordinates must follow mathematical floor division semantics rather than language truncation semantics.

### 8. Streaming

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

### 9. Persistence

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

### 10. Gameplay world access

Gameplay must interact with an explicit world-access boundary rather than directly modifying chunk storage.

Target responsibilities:

- read loaded cells;
- query world-space state;
- mutate loaded state through controlled operations;
- reject operations outside the active-world boundary;
- produce journaled changes.

The current `IWorldChunkAccess` and `WorldEditService` are the intended foundation.

### 11. Change tracking and events

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

### 12. Presentation

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

## Determinism contract

The package should preserve the following contract:

```text
same world definition
+ same seed
+ same generation version
+ same world coordinate
= same generated result
```

Random streams should be isolated by subsystem and stable inputs:

```text
seed + world coordinate/chunk owner + domain + stable salt
```

Changing structure randomness should not silently change resource randomness. Changing a resource rule should not reorder unrelated post-process randomness.

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

The current structure placement implementation explicitly protects this boundary because world features must behave identically across positive and negative coordinate space.

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
```

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
    Feature planning
    Generation
    Streaming contracts
    Persistence contracts

Jolybob.ProceduralWorld.Authoring
    ScriptableObject world definitions
    editor-facing configuration data

Jolybob.ProceduralWorld.Tilemap
    Unity Tilemap presentation adapter

Jolybob.ProceduralWorld.Editor
    inspectors
    validation tools
    world-generation previews
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
  +-- world-space structure placement
  +-- generic feature placement framework
  |
WORLD SCALE
  |
  +-- cross-chunk feature ownership
  +-- deterministic feature queries
  +-- connectivity / graph passes
  +-- points of interest
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
  +-- deterministic seed inspector
  +-- generation diagnostics
```

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

## Architectural success criteria

The target architecture is considered healthy when a project can:

1. generate the same world from the same definition and seed regardless of load order;
2. stream chunks in any order without changing their contents;
3. generate a feature that crosses chunk boundaries without special-case chunk logic;
4. replace Tilemap presentation without rewriting generation;
5. change one generation subsystem without perturbing unrelated deterministic random streams;
6. persist player edits without storing the entire procedural world;
7. load an old world using the generation version it was authored against;
8. preview and inspect world generation from authoring tools without requiring gameplay systems.

## Current implementation status

Implemented foundations include deterministic fields, region layouts and resolvers, terrain catalogs, caves/topology, clustered resource deposits, world-space cross-chunk structure placement, post-process pipelines, chunk streaming, persistence, world access/editing, change journals/history/transactions, and a Tilemap presentation adapter.

The items in this document under **Target roadmap** are architectural direction unless they are explicitly represented by the current runtime APIs. The goal is to keep the package moving toward a world-scale procedural system without coupling future capabilities to today's chunk-local implementation details.
