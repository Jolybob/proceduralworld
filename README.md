# Procedural World for Unity

A modular, deterministic 2D procedural-world framework designed to be installed as a Unity Package Manager (UPM) package and extended by any 2D game.

## Architecture — 0.1.6

The package is intentionally split by responsibility so generation logic stays reusable and presentation stays optional.

```text
Core
  WorldPosition / ChunkCoord / GeneratedCell / GeneratedChunk
      |
      +--> Generation
             |
             +--> Fields (continuous deterministic values)
             |
             +--> Regions (environment -> biome identity)
             |
             +--> Terrain (biome -> base terrain)
             |
             +--> future: Caves -> Resources -> Structures -> PostProcess
      |
      +--> Streaming (chunk lifetime)
      +--> Persistence (world modifications)
      +--> Adapters (Tilemap / Mesh / custom presentation)
```

### Architectural rules

1. **Core owns data, not rendering.**
2. **Fields produce values; they do not choose biomes or tiles.**
3. **Region passes choose environmental identity.**
4. **Terrain passes convert identity into generated terrain.**
5. **Later passes add caves, resources and structures without rewriting earlier stages.**
6. **Streaming controls which chunks are active, not how they are generated.**
7. **Persistence stores modifications independently of presentation.**
8. **Adapters translate package data into a specific game/rendering technology.**
9. **The same seed, settings and chunk coordinate must produce the same generated data.**

## Current generation pipeline

The default generator currently executes:

```text
Seed + Settings + ChunkCoord
          |
          v
 Temperature Field ----+
                       |
 Moisture Field -------+--> RegionBiomePass
                              |
                              v
                         Biome / Region ID
                              |
                              v
                          TerrainPass
                              |
                              v
                         GeneratedChunk
```

This is the foundation for richer biome definitions without coupling biome logic to Unity Tilemap.

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
2. Install this package using the Git URL above.
3. Create an empty GameObject named `ProceduralWorld`.
4. Add **Procedural World > Procedural World Tilemap**.
5. Press Play.

The adapter creates a Tilemap when required and renders a 5x5 chunk preview. Runtime-generated colors mean no external art assets are required for the prototype.

## Extending the generator

Custom stages can be inserted through `WorldGenerationPipeline`:

```csharp
var pipeline = new WorldGenerationPipeline()
    .Add(new MyBiomePass())
    .Add(new MyTerrainPass())
    .Add(new MyCavePass());

var generator = new ProceduralWorldGenerator(12345, settings, pipeline);
```

Every pass implements `IWorldGenerationPass`, receives `WorldGenerationContext`, and declares an `Order`. Lower order values execute first.

## Folder guide

- `Runtime/Core` — fundamental world data types.
- `Runtime/Generation` — generation orchestration and passes.
- `Runtime/Generation/Fields` — deterministic scalar fields/noise.
- `Runtime/Generation/Regions` — biome/region identity.
- `Runtime/Streaming` — future chunk loading/unloading.
- `Runtime/Persistence` — future save/change layers.
- `Runtime/Adapters` — engine/presentation integrations.
- `Runtime/Tilemap` — current Unity Tilemap adapter.

Each major folder contains a README describing its responsibility and dependency direction.

## Roadmap

Next architectural layers:

1. named biome definitions and configurable biome resolver
2. richer environmental fields
3. cave generation as an independent pass
4. terrain/material layers
5. deterministic resource placement
6. structure placement
7. chunk streaming
8. persistence/world modifications
9. editor preview and diagnostics
10. optional Jobs/Burst implementations
