# Procedural World for Unity

A modular, deterministic 2D procedural-world framework designed to be installed as a Unity Package Manager (UPM) package and extended by any 2D game.

## Current architecture — 0.1.5

The package now has the first reusable generation architecture rather than keeping generation rules inside one monolithic generator:

- deterministic seeded world generation
- chunk-based world data
- ordered, pluggable generation passes via `IWorldGenerationPass`
- reusable `WorldGenerationContext`
- pluggable `INoiseField` abstraction
- deterministic multi-octave `SeededPerlinNoiseField`
- prototype radial biome logic isolated in `RadialBiomePass`
- Unity Tilemap presentation adapter kept separate from core generation

The existing `ProceduralWorldGenerator(seed, settings)` API remains available. Advanced users can provide their own `WorldGenerationPipeline` and add custom passes.

## Install from Git

In a Unity 6 project:

1. Open **Window > Package Manager**.
2. Click **+**.
3. Choose **Install package from git URL...**.
4. Enter:

```text
https://github.com/Jolybob/proceduralworld.git
```

5. Let Unity import the package.

## First test in the Universal 2D template

1. Create a new Unity 6 project with the **Universal 2D** template.
2. Install this package using the URL above.
3. In the Hierarchy create an empty GameObject named `ProceduralWorld`.
4. Add the component:
   `Procedural World > Procedural World Tilemap`.
5. Press Play.

The component creates a Tilemap if one is not already present and generates a 5x5 chunk preview around the world origin. The colors are generated at runtime, so no sprites or Tile assets need to be imported.

## Extending the generator

A custom generation pipeline can be supplied without changing the package generator itself:

```csharp
using Jolybob.ProceduralWorld;

var settings = new WorldGenerationSettings();
var pipeline = new WorldGenerationPipeline()
    .Add(new MyTerrainPass())
    .Add(new MyCavePass());

var generator = new ProceduralWorldGenerator(12345, settings, pipeline);
var chunk = generator.GenerateChunk(new ChunkCoord(0, 0));
```

Each pass receives a `WorldGenerationContext`, giving it access to the seed, settings, current chunk, and deterministic noise provider. Passes are executed in ascending `Order`.

## API example

The core generator can also be used without the Tilemap adapter:

```csharp
using Jolybob.ProceduralWorld;

var settings = new WorldGenerationSettings();
var generator = new ProceduralWorldGenerator(12345, settings);
var chunk = generator.GenerateChunk(new ChunkCoord(0, 0));

GeneratedCell cell = chunk.GetCell(10, 10);
```

The generator works on plain data. The Tilemap component is only a presentation adapter.

## Roadmap

The intended architecture is:

```text
Core data / algorithms
        -> fields / noise
        -> generation pipeline
        -> biome / region resolution
        -> terrain / caves / structures
        -> chunk data
        -> streaming / persistence
        -> optional presentation adapters
```

Planned extension points include:

- richer biome and region resolvers
- Voronoi/region fields
- cellular-automata caves
- terrain layers and material selection
- structure placement and WFC
- world modification layers
- chunk streaming
- persistence interfaces
- editor world preview
- Jobs/Burst implementations
- additional render adapters
