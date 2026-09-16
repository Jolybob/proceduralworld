# Procedural World for Unity

A modular, deterministic 2D procedural-world framework designed to be installed as a Unity Package Manager (UPM) package and extended by any 2D game.

## Current architecture — 0.1.19

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
      +----> structures
      +----> post-process
      +----> streaming
      +----> persistence
      +----> presentation adapters
```

`GeneratedCell.Region`, `GeneratedCell.Terrain`, and `GeneratedCell.Resource` are the canonical generated-data identifiers. The older `Biome` and `Tile` fields remain compatibility mirrors for existing integrations.

Fields produce reusable deterministic values. Regions convert environment data into stable region identities. Terrain catalogs convert region definitions into terrain definitions. Caves and resources are independent generation passes that modify generated cell state without coupling generation to rendering.

Generation systems that need randomness should use `WorldRandomService` and request a stream for their subsystem, chunk, and optional item salt. This keeps resource types and future structure systems independently deterministic.

## Main extension points

- `INoiseField` — deterministic scalar fields
- `IEnvironmentFieldProvider` — reusable environmental sampling
- `ICaveFieldProvider` — reusable cave-density sampling
- `IRegionResolver` — converts environmental samples into stable region IDs
- `RegionCatalog` / `RegionDefinition` — stable region data definitions
- `TerrainCatalog` / `TerrainDefinition` — stable terrain data definitions
- `ResourceCatalog` / `ResourceDefinition` — stable resource data definitions
- `IWorldRandom` / `WorldRandomService` — deterministic subsystem random streams
- `IWorldGenerationPass` — ordered generation stages
- `WorldGenerationPipeline` — composes generation passes
- `ProceduralWorldGenerator` — orchestrates deterministic chunk generation

The existing `ProceduralWorldGenerator(seed, settings)` API remains available. Advanced users can provide custom pipelines, field providers, catalogs, cave fields, and resource catalogs.

## Resource layer

Resources are generated as data, not rendered objects. `ResourcePass` selects eligible cells by canonical region and terrain IDs, uses a dedicated deterministic Resources stream per resource type, respects per-resource spawn probability and per-chunk limits, and marks occupied cells with `GeneratedCellFlags.HasResource`.

Resources are disabled by default. Enable `WorldGenerationSettings.resourcesEnabled` when a project wants procedural resource placement.

## Cave layer

Caves are implemented as an independent post-terrain modifier. They are disabled by default so existing worlds retain their previous generated output.

Enable them through `WorldGenerationSettings.cavesEnabled` and configure `caveScale`, `caveThreshold`, `caveMinimumDistance`, and `caveSeedOffset`.

`GeneratedCellFlags.Carved` records that a cell was modified by cave generation, while the rendering adapter only consumes the resulting cell state.

## Deterministic random streams

A generation pass can request an isolated stream:

```csharp
IWorldRandom random = context.Random.Create(
    context.ChunkCoordinate,
    WorldRandomDomain.Resources,
    resource.Id.Value);

if (random.Chance(0.15f))
{
    // deterministic resource placement
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

The component creates a Tilemap if one is not already present and generates a 5x5 chunk preview around the world origin. The preview seed is currently `24680` for this architecture revision. The colors are generated at runtime, so no sprites or Tile assets need to be imported.

## Custom fields and catalogs

Projects can replace environmental and cave fields, region/terrain catalogs, the resource catalog, or the complete pipeline without changing the core chunk data model.

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
- structure placement and WFC
- world modification layers
- chunk streaming
- persistence interfaces
- editor world preview
- Jobs/Burst implementations
- additional render adapters
