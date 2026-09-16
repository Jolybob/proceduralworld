# Procedural World for Unity

A modular, deterministic 2D procedural-world framework designed to be installed as a Unity Package Manager (UPM) package and extended by any 2D game.

## Current architecture — 0.1.16

The generation stack is intentionally separated by responsibility:

```text
seed + settings
      |
      v
  field layer
      |
      +----> environment fields -> region resolver -> region identity
      |
      +----> cave fields -------> cave modifier -> cell flags / terrain changes
      |
      v
  terrain catalog / terrain pass
      |
      v
 generated chunk data
      |
      +----> resources
      +----> structures
      +----> post-process
      +----> streaming
      +----> persistence
      +----> presentation adapters
```

Fields produce reusable deterministic values. Regions convert environment data into stable region identities. Terrain catalogs convert region definitions into terrain definitions. Modifier passes such as caves can then alter generated cell state without coupling generation to rendering.

## Main extension points

- `INoiseField` — deterministic scalar fields
- `IEnvironmentFieldProvider` — reusable environmental sampling
- `ICaveFieldProvider` — reusable cave-density sampling
- `IRegionResolver` — converts environmental samples into stable region IDs
- `RegionCatalog` / `TerrainCatalog` — stable data definitions
- `IWorldGenerationPass` — ordered generation stages
- `WorldGenerationPipeline` — composes generation passes
- `ProceduralWorldGenerator` — orchestrates deterministic chunk generation

The existing `ProceduralWorldGenerator(seed, settings)` API remains available. Advanced users can provide custom pipelines, field providers, catalogs, and cave fields.

## Cave layer

Caves are implemented as an independent post-terrain modifier. They are disabled by default so existing worlds retain their previous generated output.

Enable them through `WorldGenerationSettings.cavesEnabled` and configure `caveScale`, `caveThreshold`, `caveMinimumDistance`, and `caveSeedOffset`.

`GeneratedCellFlags.Carved` records that a cell was modified by cave generation, while the rendering adapter only consumes the resulting cell state.

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

The component creates a Tilemap if one is not already present and generates a 5x5 chunk preview around the world origin. The colors are generated at runtime, so no sprites or Tile assets need to be imported.

## Custom fields and catalogs

Projects can replace environmental and cave fields, region/terrain catalogs, or the complete pipeline without changing the core chunk data model.

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
- resource distribution
- structure placement and WFC
- world modification layers
- chunk streaming
- persistence interfaces
- editor world preview
- Jobs/Burst implementations
- additional render adapters
