# Procedural World for Unity

A modular, deterministic 2D procedural-world framework for Unity 6.

`com.jolybob.proceduralworld` is designed around one architectural rule:

> **World coordinates define the truth; chunks define the execution and storage boundary.**

The package is intended for large, persistent 2D worlds where terrain, caves, resources, structures, topology, streaming, persistence, and gameplay access remain separate systems.

## Architecture

```text
                         WORLD DEFINITION
                                |
                    seed + generation version
                                |
                                v
                    deterministic world fields
                                |
          +---------------------+----------------------+
          |                     |                      |
          v                     v                      v
      geography              terrain              features
      / regions              / topology            / landmarks
          |                     |                      |
          +---------------------+----------------------+
                                |
                                v
                    WORLD GENERATION PLAN
                                |
                 world-space feature identities
                                |
                                v
                       CHUNK MATERIALIZER
                                |
                         GeneratedChunk
                    /            |             \
                   /             |              \
                  v              v               v
             STREAMING       PERSISTENCE       GAMEPLAY
                  |               |               |
                  +---------------+---------------+
                                  |
                                  v
                         CHANGE / EVENT LAYER
                                  |
                     history / observers / batches
                                  |
                                  v
                         PRESENTATION ADAPTERS
                         Tilemap / ECS / custom
```

The detailed target architecture, layering rules, determinism contract, coordinate rules, package boundaries, and roadmap are documented in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Current implementation — 0.1.87

The current runtime already provides:

- deterministic scalar, environment, and cave fields;
- region catalogs and world-space region layouts;
- terrain catalogs and canonical generated-cell state;
- caves, liquids, chasms, and topology passes;
- clustered deterministic resource deposits;
- world-space structure placement that can cross chunk boundaries;
- ordered post-process generation;
- deterministic chunk streaming and persistence-aware streaming;
- world access and controlled edit services;
- transactions, change journals, grouped undo/redo history, and change observers;
- a Unity Tilemap presentation adapter;
- an optional Unity authoring assembly with `ProceduralWorldDefinitionAsset`.

The core generated state is data-only. Presentation assets and runtime GameObjects are adapters around that state.

## Core data model

`GeneratedCell` is the canonical per-cell result. The primary identifiers are:

- `RegionId`
- `TerrainId`
- `ResourceId`
- `StructureId`

The older `Biome` and `Tile` fields are retained as compatibility mirrors.

A `GeneratedChunk` owns only the cells for one chunk coordinate. World-space systems can inspect or plan features beyond that boundary, but materialization writes only the requested chunk.

## Generation pipeline

The default generation pipeline is intentionally composable:

```text
RegionBiomePass
      -> TerrainPass
      -> CavePass
      -> TopologyPass
      -> ResourcePass
      -> StructurePlacementPass
      -> WorldPostProcessPass
```

Each stage is an `IWorldGenerationPass`, so a project can replace one subsystem without replacing the whole generator.

Large features follow a separate planning/materialization model:

```text
feature definition
      -> deterministic planner
      -> world-space placement
      -> relevant chunk intersection
      -> local materialization
```

The structure system is the first implementation of this model. A `StructurePlacement` is independent of which chunks happen to be loaded, while `StructurePlacementPass` writes only its local footprint.

## Determinism

For a fixed world definition, seed, generation version, and world coordinate, generation should produce the same result regardless of chunk load order.

Random streams are isolated by domain and stable salt so unrelated systems do not accidentally perturb one another.

```csharp
IWorldRandom random = context.Random.Create(
    context.ChunkCoordinate,
    WorldRandomDomain.Structures,
    structure.Id.Value);
```

World-space coordinate conversion must use floor-division semantics for negative coordinates.

## World definition and authoring

`ProceduralWorldDefinitionAsset` provides the beginning of the authored world-definition layer.

Create one with:

**Assets > Create > Procedural World > World Definition**

The current asset can define the seed, generation settings, regions, terrains, and threshold/radial macro-region layouts. The target architecture extends this same concept to resource rules, structure rules, topology, feature placement, and generation metadata.

At runtime:

```csharp
var generator = definition.CreateGenerator();
```

The authoring assembly depends only on the runtime generation assembly, keeping it independent from the Tilemap presentation adapter.

See [`Runtime/Authoring/README.md`](Runtime/Authoring/README.md) for the authoring example.

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

Persistence stores the sparse divergence between the deterministic base world and player-authored changes. The target architecture also requires generation-version metadata so an old world can be interpreted against the recipe that created it.

## Gameplay and change tracking

Gameplay should use `IWorldChunkAccess` and `WorldEditService` instead of reaching into chunk storage.

Successful mutations produce `WorldCellChange` records through `IWorldChangeJournal`. Transactions can publish one logical `WorldChangeBatch`, allowing rendering, networking, analytics, UI, or other observers to react at the appropriate granularity.

## Presentation

The generation core does not require a Tilemap.

`WorldTilemapRenderer` is one presentation adapter implementing the relevant streaming and change-rendering contracts. Projects can supply their own adapters for SpriteRenderers, ECS, custom meshes, debug views, or network replicas.

## Package layering target

The package is intended to converge on these conceptual boundaries:

```text
Runtime
  core data
  deterministic fields
  regions / terrain
  topology
  feature planning
  generation
  streaming contracts
  persistence contracts

Authoring
  ScriptableObject world definitions
  configuration and validation data

Tilemap
  Unity Tilemap presentation adapter

Editor
  inspectors
  world previews
  diagnostics
  authoring tooling
```

This prevents presentation dependencies from leaking into the deterministic world-generation core.

## Target roadmap

The next architectural stages are deliberately world-scale:

```text
canonical world data
  -> deterministic fields
  -> geography / regions / terrain
  -> caves / topology
  -> resource deposits
  -> world-space structure placement
  -> generic cross-chunk feature framework
  -> connectivity / graph generation
  -> points of interest / landmarks
  -> generation scheduling and budgets
  -> background-safe generation
  -> generation-versioned persistence
  -> authoring / preview / diagnostics tooling
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the full target model and success criteria.

## Extension points

The primary public boundaries include:

- `INoiseField`
- `IEnvironmentFieldProvider`
- `ICaveFieldProvider`
- `IRegionResolver` / `IRegionLayout`
- `RegionCatalog` / `RegionDefinition`
- `TerrainCatalog` / `TerrainDefinition`
- `ResourceCatalog` / `ResourceDefinition`
- `StructureCatalog` / `StructureDefinition`
- `IWorldGenerationPass` / `WorldGenerationPipeline`
- `IStructurePlacementSource` / `StructurePlacementPlanner`
- `IWorldChunkSink` / `ChunkStreamingPlanner`
- `IWorldChunkAccess` / `WorldEditService`
- `IWorldChangeJournal` / `WorldEditHistory`
- `IWorldChangeListener` / `IWorldChangeBatchListener`
- `IWorldChangeRenderer` / `WorldTilemapRenderer`
- `IWorldChunkStore` / `WorldChunkPersistenceService`
- `ProceduralWorldGenerator`

The existing `ProceduralWorldGenerator(seed, settings)` API remains available for straightforward integrations.

## Testing

The package contains an EditMode test assembly under `Tests/Runtime`.

For Git-installed packages, enable the package in the consuming project's `testables` list, then run the EditMode tests from Unity's Test Runner.

The repository's source-level regression suite covers deterministic generation contracts, world-coordinate behavior, resource deposits, structure placement, streaming, persistence, editing, history, notifications, and presentation boundaries.

## Install

In Unity 6, install from Git using:

```text
https://github.com/Jolybob/proceduralworld.git
```

The package manifest currently declares version `0.1.87`.

## Scope

The runtime package intentionally does not own game-specific systems such as combat, inventory, quests, UI, player input, prefab orchestration, or network transport. Those systems should consume the stable world-data and service interfaces exposed by the package.
