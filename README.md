# Procedural World for Unity

A modular, deterministic 2D procedural-world framework designed to be installed as a Unity Package Manager (UPM) package and extended by any 2D game.

## Current architecture — 0.1.22

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
      +----> presentation adapters
```

`GeneratedCell.Region`, `GeneratedCell.Terrain`, `GeneratedCell.Resource`, and `GeneratedCell.Structure` are the canonical generated-data identifiers. The older `Biome` and `Tile` fields remain compatibility mirrors for existing integrations.

Fields produce reusable deterministic values. Regions convert environment data into stable region identities. Terrain catalogs convert region definitions into terrain definitions. Caves, resources, and structures are independent generation passes that modify generated cell state without coupling generation to rendering. The post-process layer provides a final composable data-only modification stage. Streaming then decides which chunk coordinates are active without changing how chunks are generated.

Generation systems that need randomness should use `WorldRandomService` and request a stream for their subsystem, chunk, and optional item salt. This keeps resource types, structure types, and post-process steps independently deterministic.

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
- `ProceduralWorldGenerator` — orchestrates deterministic chunk generation

The existing `ProceduralWorldGenerator(seed, settings)` API remains available. Advanced users can provide custom pipelines, field providers, catalogs, cave fields, resource catalogs, structure catalogs, and a post-process pipeline.

## Resource layer

Resources are generated as data, not rendered objects. `ResourcePass` selects eligible cells by canonical region and terrain IDs, uses a dedicated deterministic Resources stream per resource type, respects per-resource spawn probability and per-chunk limits, and marks occupied cells with `GeneratedCellFlags.HasResource`.

Resources are disabled by default. Enable `WorldGenerationSettings.resourcesEnabled` when a project wants procedural resource placement.

## Structure layer

Structures are generated as deterministic multi-cell footprints. `StructurePass` selects anchors using the Structures random domain plus the structure ID as a salt, validates the entire footprint before placement, prevents overlap with caves, resources, and other structures, and records occupancy through `GeneratedCell.Structure` and `GeneratedCellFlags.HasStructure`.

Structures are disabled by default. Enable `WorldGenerationSettings.structuresEnabled` when a project wants procedural structure placement.

## Post-process layer

Post-process steps run after caves, resources, and structures and are intentionally independent from rendering. `WorldPostProcessPipeline` sorts steps by `Order` and executes them through `WorldPostProcessPass` at order `900` in the default generation pipeline.

Each `IWorldPostProcessStep` supplies a stable `Salt`. `WorldPostProcessContext.Random` creates a deterministic stream using the world seed, chunk coordinate, the `PostProcess` random domain, and that salt. This lets one modification step change without perturbing unrelated post-process randomness.

The default generator includes an empty post-process stage, so projects can inject world modifications without replacing the rest of the generation pipeline.

## Chunk streaming layer

Streaming is deliberately separate from generation and rendering. `ChunkStreamingPlanner` tracks the currently active chunk coordinates and computes the load/unload delta around a center chunk. `loadRadius` defines the required active square, while an optional larger `unloadRadius` adds hysteresis so nearby movement does not immediately unload edge chunks.

`WorldChunkStreamingController` connects the planner to `ProceduralWorldGenerator` and an `IWorldChunkSink`. Newly requested coordinates are generated exactly through the normal deterministic generator; unload operations only notify the sink.

A radius of `2` activates 25 chunks. Streaming coordinates are emitted in stable Y-then-X order, making scheduling and tests deterministic.

Example:

```csharp
var generator = new ProceduralWorldGenerator(75319, settings);
var planner = new ChunkStreamingPlanner(loadRadius: 2, unloadRadius: 3);
var streaming = new WorldChunkStreamingController(generator, planner, sink);

streaming.Update(new ChunkCoord(10, -4));
```

See `Runtime/Generation/Streaming/README.md` for the complete contract.

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

The component creates a Tilemap if one is not already present and generates a 5x5 chunk preview around the world origin. The preview seed is currently `75319` for this architecture revision. The colors are generated at runtime, so no sprites or Tile assets need to be imported.

## Custom fields and catalogs

Projects can replace environmental and cave fields, region/terrain catalogs, the resource catalog, the structure catalog, the complete generation pipeline, or the post-process pipeline without changing the core chunk data model. Streaming consumers are also replaceable through `IWorldChunkSink`.

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
  -> rendering adapters
```

Planned extension points include:

- richer biome and region definitions
- Voronoi and domain-warped fields
- cellular-automata cave refinement
- terrain layers and material selection
- richer resource distribution and clustering
- richer structure placement and WFC
- world modification layers built on post-process steps
- streaming prioritization and asynchronous generation hooks
- persistence interfaces
- editor world preview
- Jobs/Burst implementations
- additional render adapters
