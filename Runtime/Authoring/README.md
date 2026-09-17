# Unity world definition authoring

`ProceduralWorldDefinitionAsset` is the optional Unity authoring layer for the procedural world framework.

## Role in the target architecture

The authoring layer defines the **world recipe**. It should describe data and generation rules, not runtime presentation objects.

```text
World Definition Asset
        |
        +-- seed
        +-- generation settings
        +-- generation version (target)
        +-- macro geography / region layout
        +-- region profiles
        +-- terrain profiles
        +-- resource rules (target)
        +-- structure rules (target)
        +-- topology rules (target)
        +-- feature rules (target)
                |
                v
        ProceduralWorldGenerator
                |
                v
        deterministic world data
```

The full architectural direction is documented in [`docs/ARCHITECTURE.md`](../../docs/ARCHITECTURE.md).

## Current asset

Create a definition with:

**Assets > Create > Procedural World > World Definition**

The current asset can author:

- deterministic world seed;
- existing `WorldGenerationSettings` values;
- region profiles (`RegionId`, name, description, default terrain);
- terrain profiles (`TerrainId`, name, tile, description);
- threshold or radial-sector macro-region layouts.

Resource, structure, topology, and broader feature authoring are target extensions of the same world-definition boundary and should not be moved into the rendering layer.

## Runtime usage

At runtime, call `CreateGenerator()` and pass the returned `ProceduralWorldGenerator` into the existing streaming/persistence stack.

```csharp
using Jolybob.ProceduralWorld;
using Jolybob.ProceduralWorld.Authoring;
using UnityEngine;

public sealed class WorldBootstrap : MonoBehaviour
{
    [SerializeField] private ProceduralWorldDefinitionAsset definition;

    private ProceduralWorldGenerator generator;

    private void Awake()
    {
        generator = definition.CreateGenerator();
    }
}
```

The generated world remains data-only. The generator can then feed streaming, persistence, gameplay access, change tracking, and any presentation adapter.

## Separation from presentation

The authoring assembly references only `Jolybob.ProceduralWorld.Runtime`; it does not depend on the Tilemap adapter.

That boundary is intentional:

```text
Authoring
    -> Runtime generation
    -> Streaming / persistence
    -> Presentation adapter
```

not:

```text
Authoring
    -> Tilemap-specific generation
```

This keeps world definitions reusable with Tilemap, ECS, SpriteRenderer, custom rendering, or headless generation.

## World-space geography

For Core Keeper-style radial geography, select **Radial Sectors**, define one or more radial/angle rules, and keep region IDs stable once a world definition has shipped.

The underlying `RadialSectorRegionResolver` is deterministic and supports seed rotation plus warped boundaries. The target architecture treats this as one macro-geography strategy alongside future authored zones and other world-space layouts.

## Authoring roadmap

The authoring layer should eventually expose the same concepts as the runtime target architecture:

```text
World Definition
  -> world identity / generation version
  -> fields
  -> macro geography
  -> regions / terrain
  -> topology
  -> resource deposits
  -> world-space structures
  -> points of interest / landmarks
  -> streaming defaults
  -> persistence metadata
```

Editor tooling should preview these rules without making the world definition responsible for spawning GameObjects or rendering tiles.
