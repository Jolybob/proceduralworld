# Unity world definition authoring

`ProceduralWorldDefinitionAsset` is the optional Unity authoring layer for the procedural world framework.

Create one with:

**Assets > Create > Procedural World > World Definition**

The asset can author:

- deterministic world seed;
- the existing `WorldGenerationSettings` values;
- region profiles (`RegionId`, name, description, default terrain);
- terrain profiles (`TerrainId`, name, tile, description);
- threshold or radial-sector macro-region layouts.

At runtime, call `CreateGenerator()` and pass the returned `ProceduralWorldGenerator` into the existing streaming/persistence stack.

Example:

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

The authoring assembly references only `Jolybob.ProceduralWorld.Runtime`; it does not depend on the Tilemap adapter. This keeps world definitions reusable with custom presentation systems.

For Core Keeper-style geography, select **Radial Sectors**, define one or more radial/angle rules, and keep region IDs stable once a world has shipped. The underlying `RadialSectorRegionResolver` remains deterministic and supports seed rotation plus warped boundaries.
