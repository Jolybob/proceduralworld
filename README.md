# Procedural World for Unity

A small, deterministic 2D procedural-world framework designed to be installed as a Unity Package Manager (UPM) package and extended by any 2D game.

## Current prototype

Version `0.1.0` contains:

- deterministic seeded generation
- chunk-based world data (`64x64` by default)
- radial/warped region generation
- multi-octave Perlin noise
- a Unity Tilemap adapter
- zero external art assets required for the first test

This is intentionally the first vertical slice, not the final architecture. The next layers can replace the prototype region/terrain rules with pluggable generation passes, custom region resolvers, noise providers, structures, persistence, and streaming.

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

Unity custom packages use a `package.json` at the package root plus Runtime/Editor assemblies; this repository follows that UPM layout.

## First test in the Universal 2D template

1. Create a new Unity 6 project with the **Universal 2D** template.
2. Install this package using the URL above.
3. In the Hierarchy create an empty GameObject named `ProceduralWorld`.
4. Add the component:
   `Procedural World > Procedural World Tilemap`.
5. Press Play.

The component creates a Tilemap if one is not already present and generates a 5x5 chunk preview around the world origin. The colors are generated at runtime, so no sprites or Tile assets need to be imported.

### Controls in the Inspector

- **Seed**: changing this changes the generated world.
- **Chunk Size**: size of each logical chunk.
- **Noise Scale**: large/small terrain variation.
- **Noise Strength**: amount of border distortion.
- **Core Radius / Inner Radius / Mid Radius**: prototype biome bands.
- **Border Warp**: irregularity of radial region boundaries.
- **Chunks Radius**: how many chunks to preview around `(0,0)`.
- **Generate On Start**: generate automatically in Play Mode.

Use the component's **Generate World** context-menu command to regenerate from the Inspector as well.

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
        -> generation pipeline
        -> chunk data
        -> optional presentation adapters
```

Planned extension points include:

- pluggable generation passes
- Voronoi/region fields
- cellular-automata caves
- structure placement and WFC
- world modification layers
- chunk streaming
- persistence interfaces
- editor world preview
- Jobs/Burst implementations
- additional render adapters
