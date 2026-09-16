# Resources

The resource layer places gameplay-relevant generated content without knowing how it will be rendered.

## Responsibilities

- stable `ResourceId` definitions
- region/terrain eligibility rules
- deterministic per-chunk placement
- per-resource spawn probability and per-chunk limits
- minimum distance constraints
- cell occupancy flags

## Flow

```text
RegionId + TerrainId
        |
        v
 ResourceCatalog
        |
        v
   ResourcePass
        |
        +---- WorldRandomDomain.Resources + resource salt
        |
        v
GeneratedCell.Resource + HasResource
```

Resources are disabled by default through `WorldGenerationSettings.resourcesEnabled`. Enable the feature without changing the core rendering adapter; the adapter can later decide how each `ResourceId` is presented.

## Extension

Projects can create a custom `ResourceCatalog` and pass it to `ProceduralWorldGenerator`, or provide their own `IWorldGenerationPass` using the shared deterministic random service.
