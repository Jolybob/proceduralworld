# Procedural World for Unity

A modular, deterministic 2D procedural-world framework designed to be installed as a Unity Package Manager (UPM) package and extended by any 2D game.

## Current architecture — 0.1.32

The generation stack is intentionally separated by responsibility:

```text
seed + settings
      |
      v
  field layer
      |
      +----> environment fields -> region resolver -> RegionId
      |
      +----> cave fields -------> cave modifier -> cell flags / terrain changes
      |
      v
 canonical generated cell data
      |
      +----> TerrainId
      +----> ResourceId
      +----> StructureId
      +----> post-process pipeline
      +----> chunk streaming planner/controller
      +----> persistence
      +----> world access / editing
      +----> change tracking / history
      +----> change notifications
      +----> presentation adapters
```

`GeneratedCell.Region`, `GeneratedCell.Terrain`, `GeneratedCell.Resource`, and `GeneratedCell.Structure` are the canonical generated-data identifiers. The older `Biome` and `Tile` fields remain compatibility mirrors for existing integrations.

Fields produce reusable deterministic values. Regions convert environment data into stable region identities. Terrain catalogs convert region definitions into terrain definitions. Caves, resources, and structures are independent generation passes that modify generated cell state without coupling generation to rendering. The post-process layer provides a final composable data-only modification stage. Streaming decides which chunk coordinates are active without changing how chunks are generated. Persistence stores player/world modifications separately from deterministic generation. The world-access layer exposes only currently loaded state to gameplay, while the change journal records successful mutations as before/after state transitions. World edit history groups those transitions into named undo/redo entries. The notification layer fans recorded changes out to reactive consumers without replacing the journal as the history source of truth.

## Main extension points

- `INoiseField` — deterministic scalar fields
- `IEnvironmentFieldProvider` — reusable environmental sampling
- `ICaveFieldProvider` — reusable cave-density sampling
- `IRegionResolver` — converts environmental samples into stable region IDs
- `RegionCatalog` / `RegionDefinition` — stable region data definitions
- `TerrainCatalog` / `TerrainDefinition` — stable terrain data definitions
- `ResourceCatalog` / `ResourceDefinition` — stable resource data definitions
- `StructureCatalog` / `StructureDefinition` — stable structure data definitions
- `IWorldRandom` / `WorldRandomService` — deterministic subsystem random streams
- `IWorldGenerationPass` — ordered generation stages
- `WorldGenerationPipeline` — composes generation passes
- `IWorldPostProcessStep` — ordered final world-data modifications
- `WorldPostProcessPipeline` — composes post-process steps with isolated random streams
- `WorldPostProcessContext` — exposes chunk data and step-scoped deterministic services
- `WorldPostProcessPass` — inserts the post-process pipeline into the main generation pipeline
- `IWorldChunkSink` — receives streaming load/unload operations
- `ChunkStreamingPlanner` — computes deterministic active-chunk deltas
- `ChunkStreamingDelta` — describes loads and unloads for one update
- `WorldChunkStreamingController` — connects chunk planning to deterministic generation
- `WorldPersistentChunkStreamingController` — connects streaming to persistence-aware load/save lifecycle and implements active-world access
- `IWorldChunkAccess` — narrow read/write boundary for currently loaded chunks
- `WorldChunkCoordinates` — deterministic world-to-chunk/local coordinate conversion
- `WorldEditService` — gameplay-facing controlled world mutation API
- `WorldEditOperationKind` — classifies mutation types
- `WorldCellChange` — captures complete before/after cell state for one edit
- `IWorldChangeJournal` — backend-neutral change-history boundary
- `InMemoryWorldChangeJournal` — test/prototype change journal
- `WorldEditHistoryEntry` — named group of changes used for undo/redo
- `WorldEditHistory` — grouped undo/redo history over journal changes
- `IWorldChangeListener` — reactive consumer boundary for successful world changes
- `WorldChangeObserverJournal` — observable journal decorator with disposable subscriptions
- `WorldCellModification` — one persisted cell override
- `WorldChunkSaveData` — sparse chunk save representation
- `IWorldChunkStore` — backend-neutral persistence contract
- `WorldChunkPersistenceService` — regenerates, compares, saves, and restores chunks
- `InMemoryWorldChunkStore` — test/prototype persistence backend
- `ProceduralWorldGenerator` — orchestrates deterministic chunk generation

The existing `ProceduralWorldGenerator(seed, settings)` API remains available. Advanced users can provide custom pipelines, field providers, catalogs, cave fields, resource catalogs, structure catalogs, and a post-process pipeline.

## World editing, change tracking, history, and notifications

Gameplay should mutate loaded cells through `WorldEditService` rather than reaching into streaming internals. The service exposes named operations for tiles, resources, structures, and complete cell replacement. A mutation is recorded only after the underlying world access accepts it, and no-op edits are not journaled.

`IWorldChangeJournal` receives `WorldCellChange` records containing the exact world position, complete `Before` and `After` `GeneratedCell` state, and the operation kind. `WorldEditHistory` consumes those records without coupling history to persistence or rendering. Multiple journal records can be committed under one name and then undone or redone as a single history entry. Undo applies grouped changes in reverse order; redo reapplies them in forward order. A new committed edit after undo invalidates the redo branch.

`WorldChangeObserverJournal` decorates any `IWorldChangeJournal` and publishes each successfully recorded change to subscribed `IWorldChangeListener` instances. The wrapped journal remains the durable source of truth for history, while listeners can drive UI, rendering invalidation, multiplayer transport, analytics, audio, or other reactive systems. Subscriptions are disposable, and observers are called in registration order from a stable snapshot so a callback can safely subscribe or unsubscribe without mutating the active notification iteration.

Example:

```csharp
var sourceJournal = new InMemoryWorldChangeJournal();
var journal = new WorldChangeObserverJournal(sourceJournal);
using (journal.Subscribe(change => { }))
{
    // Implement IWorldChangeListener in production code.
}

var edits = new WorldEditService(worldAccess, settings.chunkSize, journal);
var history = new WorldEditHistory(worldAccess, journal);

edits.TrySetTile(new WorldPosition(10, 10), WorldTile.Core);
edits.TrySetResource(new WorldPosition(11, 10), new ResourceId(12));
history.Commit("Build chamber");
history.Undo();
history.Redo();
```

Implement `IWorldChangeListener` when a system should react to the same canonical change record without owning world state or persistence.

## Resource layer

Resources are generated as data, not rendered objects. `ResourcePass` selects eligible cells by canonical region and terrain IDs, uses a dedicated deterministic Resources stream per resource type, respects per-resource spawn probability and per-chunk limits, and marks occupied cells with `GeneratedCellFlags.HasResource`.

Resources are disabled by default. Enable `WorldGenerationSettings.resourcesEnabled` when a project wants procedural resource placement.

## Structure layer

Structures are generated as deterministic multi-cell footprints. `StructurePass` selects anchors using the Structures random domain plus the structure ID as a salt, validates the entire footprint before placement, prevents overlap with caves, resources, or other structures, and records occupancy through `GeneratedCell.Structure` and `GeneratedCellFlags.HasStructure`.

Structures are disabled by default. Enable `WorldGenerationSettings.structuresEnabled` when a project wants procedural structure placement.

## Post-process layer

Post-process steps run after caves, resources, and structures and are intentionally independent from rendering. `WorldPostProcessPipeline` sorts steps by `Order` and executes them through `WorldPostProcessPass` at order `900` in the default generation pipeline.

Each `IWorldPostProcessStep` supplies a stable `Salt`. `WorldPostProcessContext.Random` creates a deterministic stream using the world seed, chunk coordinate, the `PostProcess` random domain, and that salt. This lets one modification step change without perturbing unrelated post-process randomness.

The default generator includes an empty post-process stage, so projects can inject world modifications without replacing the rest of the generation pipeline.

## Chunk streaming layer

Streaming is deliberately separate from generation and rendering. `ChunkStreamingPlanner` tracks the currently active chunk coordinates and computes the load/unload delta around a center chunk. `loadRadius` defines the required active square, while an optional larger `unloadRadius` adds hysteresis so nearby movement does not immediately unload edge chunks.

`WorldChunkStreamingController` connects the planner to `ProceduralWorldGenerator` and an `IWorldChunkSink`. Newly requested coordinates are generated exactly through the normal deterministic generator; unload operations only notify the sink.

`WorldPersistentChunkStreamingController` adds the world lifecycle boundary on top of this: loads use `WorldChunkPersistenceService.LoadChunk`, unloaded chunks are saved before the sink is notified, and `Reset()` saves and unloads all currently loaded chunks. It also implements `IWorldChunkAccess`, allowing gameplay services to read and mutate only active chunks without depending on streaming implementation details.

A radius of `2` activates 25 chunks. Streaming coordinates are emitted in stable Y-then-X order, making scheduling and tests deterministic.

Example:

```csharp
var generator = new ProceduralWorldGenerator(116503, settings);
var store = new InMemoryWorldChunkStore();
var persistence = new WorldChunkPersistenceService(generator, store);
var planner = new ChunkStreamingPlanner(loadRadius: 2, unloadRadius: 3);
var streaming = new WorldPersistentChunkStreamingController(persistence, planner, sink);
var journal = new WorldChangeObserverJournal(new InMemoryWorldChangeJournal());
var edits = new WorldEditService(streaming, settings.chunkSize, journal);
var history = new WorldEditHistory(streaming, journal);

streaming.Update(new ChunkCoord(10, -4));
edits.TrySetTile(new WorldPosition(641, -255), WorldTile.Core);
history.Commit("Player edit");
```

See `Runtime/Generation/Streaming/README.md` for the complete streaming contract.

## Persistence layer

Persistence does not replace procedural generation. `WorldChunkPersistenceService` always regenerates the requested chunk from the normal deterministic generator, then applies any stored cell overrides. When saving, it regenerates the same base chunk and stores only cells whose current state differs.

This makes untouched procedural terrain reproducible while player edits remain persistent. `IWorldChunkStore` is intentionally backend-neutral; a project can implement disk files, databases, cloud storage, or platform-specific persistence without changing generation code.

`WorldChunkSaveData.FormatVersion` provides an explicit migration point for future save-format changes.

Example:

```csharp
var generator = new ProceduralWorldGenerator(116503, settings);
var store = new InMemoryWorldChunkStore();
var persistence = new WorldChunkPersistenceService(generator, store);

GeneratedChunk chunk = persistence.LoadChunk(new ChunkCoord(10, -4));
GeneratedCell cell = chunk.GetCell(5, 5);
cell.SetResource(new ResourceId(12));
chunk.SetCell(5, 5, cell);

persistence.SaveChunk(chunk);
```

The in-memory store is intended for tests and prototypes. Production projects should provide their own `IWorldChunkStore` implementation.

## Running package tests

This package contains an EditMode test assembly under `Tests/Runtime`. Its assembly definition is configured as a Unity test assembly, but Git-installed package tests must also be enabled by the consuming Unity project.

Open your project's `Packages/manifest.json` and add the package name to the top-level `testables` array:

```json
{
  "dependencies": {
    "com.jolybob.proceduralworld": "https://github.com/Jolybob/proceduralworld.git"
  },
  "testables": [
    "com.jolybob.proceduralworld"
  ]
}
```

Keep your project's existing dependencies and add only the `testables` entry; do not replace the whole manifest with the example above.

Then let Unity re-import the package, reopen **Window > General > Test Runner**, select **EditMode**, and use **Run All**.

For a locally embedded package, tests are considered testable automatically.

The seed-difference regression test compares the seeded environment field across multiple world positions rather than requiring two different seeds to cross a coarse region/terrain threshold in one specific chunk. This keeps the test aligned with deterministic field behavior without making an unsupported assumption about region boundaries.

## Cave layer

Caves are implemented as an independent post-terrain modifier. They are disabled by default so existing worlds retain their previous generated output.

Enable them through `WorldGenerationSettings.cavesEnabled` and configure `caveScale`, `caveThreshold`, `caveMinimumDistance`, and `caveSeedOffset`.

`GeneratedCellFlags.Carved` records that a cell was modified by cave generation, while the rendering adapter only consumes the resulting cell state.

## Deterministic random streams

A generation pass can request an isolated stream:

```csharp
IWorldRandom random = context.Random.Create(
    context.ChunkCoordinate,
    WorldRandomDomain.Structures,
    structure.Id.Value);

if (random.Chance(0.01f))
{
    // deterministic structure placement
}
```

The same world seed, chunk coordinate, domain, and salt produce the same sequence. Different domains and salts are intentionally independent.

## Install from Git

In a Unity 6 project:

1. Open **Window > Package Manager**.
2. Click **+**.
3. Choose **Install package from git URL...**.
4. Enter:

```text
https://github.com/Jolybob/proceduralworld.git
```

## First test in the Universal 2D template

1. Create a new Unity 6 project with the **Universal 2D** template.
2. Install this package using the URL above.
3. In the Hierarchy create an empty GameObject named `ProceduralWorld`.
4. Add the component:
   `Procedural World > Procedural World Tilemap`.
5. Press Play.

The component creates a Tilemap if one is not already present and generates a 5x5 chunk preview around the world origin. The preview seed is currently `116503` for this architecture revision. The colors are generated at runtime, so no sprites or Tile assets need to be imported.

## Custom fields and catalogs

Projects can replace environmental and cave fields, region/terrain catalogs, the resource catalog, the structure catalog, the complete generation pipeline, or the post-process pipeline without changing the core chunk data model. Streaming consumers are also replaceable through `IWorldChunkSink`, persistence backends through `IWorldChunkStore`, gameplay access through `IWorldChunkAccess`, mutation history through `IWorldChangeJournal`, undo/redo through `WorldEditHistory`, and reactive consumers through `IWorldChangeListener`.

## Roadmap

The architecture is intended to grow in this order:

```text
fields
  -> regions / biomes
  -> terrain layers
  -> caves
  -> resources
  -> structures
  -> post-process
  -> chunk streaming
  -> persistence
  -> persistence-aware streaming lifecycle
  -> world access / editing
  -> change tracking
  -> grouped undo / redo history
  -> change notifications
  -> rendering adapters
```

Planned extension points include:

- richer biome and region definitions
- Voronoi and domain-warped fields
- cellular-automata cave refinement
- terrain layers and material selection
- richer resource distribution and clustering
- richer structure placement and WFC
- editor-facing world modification tools
- history transaction merging and bounded history memory
- change-event batching for high-volume systems
- streaming prioritization and asynchronous generation hooks
- durable storage implementations built on `IWorldChunkStore`
- save migration tooling
- editor world preview
- Jobs/Burst implementations
- additional render adapters
