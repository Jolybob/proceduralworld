# World authoring

`WorldGenerationProfile` is the Unity-facing entry point for authoring a reusable procedural world.

## Why this layer exists

The runtime package deliberately keeps generation logic and immutable catalogs separate from Unity asset authoring. A project should be able to create a world without writing C# constructors for every region, terrain, resource, or structure.

Create an asset with:

**Assets > Create > Procedural World > World Generation Profile**

The asset can author:

- `WorldGenerationSettings`
- macro-region layout and seed rotation
- region catalog entries
- terrain catalog entries
- resource catalog entries
- structure catalog entries

Empty catalogs use the package defaults. Populated arrays replace only that catalog.

## Runtime flow

```text
WorldGenerationProfile.asset
          |
          v
      CreateGenerator(seed)
          |
          +--> immutable RegionCatalog
          +--> immutable TerrainCatalog
          +--> immutable ResourceCatalog
          +--> immutable StructureCatalog
          +--> IRegionResolver
          |
          v
   WorldGenerationPipeline
          |
          v
      GeneratedChunk
```

The asset is configuration; it is not world state. Player edits and persistence continue to use the existing world-access and persistence layers.

## Macro-region example

A profile can describe a central ring followed by an outer ring or sector layout. Set `Use Macro Regions` through `SetMacroRegions(...)` in code or by serializing entries in the Unity inspector. `macroFallbackRegionId` must reference a region in the region catalog.

The optional seed rotation allows the same authored sector layout to rotate deterministically for different world seeds. Disable rotation for fixed absolute world directions.

## Validation

`TryValidate(out error)` constructs the runtime catalogs and validates macro fallback configuration without generating a chunk. It is suitable for editor tooling and CI checks.

The runtime generator remains deterministic: the same seed, profile configuration, and chunk coordinate produce the same generated data.
