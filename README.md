# Procedural World for Unity

A modular, deterministic 2D procedural-world framework designed to be installed as a Unity Package Manager (UPM) package and extended by any 2D game.

## Current architecture — 0.1.14

The generation stack is intentionally separated by responsibility:

```text
seed + settings
      |
      v
  field layer
      |
      |  IEnvironmentFieldProvider
      v
 EnvironmentSample
      |
      +----> region resolver ----> region identity
      |
      +----> terrain pass -------> terrain category
      |
      +----> future caves/resources/structures
      |
      v
 generated chunk data
      |
      +----> streaming
      +----> persistence
      +----> presentation adapters
```

The field layer owns reusable deterministic world values. Region selection consumes those values rather than creating its own noise. Terrain selection consumes region identity and remains separate from environmental sampling.

## Main extension points

- `INoiseField` — deterministic scalar fields
- `IEnvironmentFieldProvider` — reusable environmental sampling
- `IRegionResolver` — converts environmental samples into stable region IDs
- `IWorldGenerationPass` — ordered generation stages
- `WorldGenerationPipeline` — composes generation passes
- `ProceduralWorldGenerator` — orchestrates deterministic chunk generation

The existing `ProceduralWorldGenerator(seed, settings)` API remains available. Advanced users can provide a custom pipeline and/or a custom `IEnvironmentFieldProvider`.

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

## Custom environment fields

A custom field provider can change environmental inputs without changing region or terrain passes:

```csharp
using Jolybob.ProceduralWorld;

var settings = new WorldGenerationSettings();
var fields = new MyEnvironmentFieldProvider();
var generator = new ProceduralWorldGenerator(12345, settings, null, fields);
var chunk = generator.GenerateChunk(new ChunkCoord(0, 0));
```

This keeps world data generation independent from how the fields are produced.

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
- cellular-automata and noise-based caves
- terrain layers and material selection
- resource distribution
- structure placement and WFC
- world modification layers
- chunk streaming
- persistence interfaces
- editor world preview
- Jobs/Burst implementations
- additional render adapters
