# Region definitions and catalogs

Regions are generated identities. They describe environmental outcomes without owning noise generation or rendering.

## Definitions

`RegionDefinition` contains:

- a stable `RegionId`
- a display name and optional description
- the default `TerrainId` used by the terrain pass

Do not reuse a published region ID for a different meaning. Generated worlds depend on stable IDs for deterministic output and persistence compatibility.

## Catalogs

`RegionCatalog` provides immutable runtime lookup through `Get` and `TryGet`.

Use `RegionCatalog.CreateDefault()` for the package prototype. Projects can construct their own catalog and inject it into `ProceduralWorldGenerator` or `TerrainPass`.

A region catalog deliberately contains data only. Region selection remains the responsibility of `IRegionResolver` implementations such as `ThresholdRegionResolver`.

## Flow

```text
EnvironmentSample
       |
       v
 IRegionResolver
       |
       v
   RegionId
       |
       v
 RegionCatalog
       |
       v
RegionDefinition
       |
       v
 Default TerrainId
```

This separation lets later features add region-specific caves, resources, structures, traversal rules, or rendering metadata without putting those concerns into the resolver.
