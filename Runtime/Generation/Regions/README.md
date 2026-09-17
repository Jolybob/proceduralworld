# Region definitions, catalogs, and macro layout

Regions are generated identities. They describe environmental outcomes without owning noise generation or rendering.

## Definitions

`RegionDefinition` contains:

- a stable `RegionId`
- a display name and optional description
- the default `TerrainId` used by the terrain pass

Do not reuse a published region ID for a different meaning. Generated worlds depend on stable IDs for deterministic output and persistence compatibility.

`MacroRegionDefinition` describes a large world-space region using:

- radial bounds (`MinRadius` / `MaxRadius`)
- an angular center and width, in radians
- optional deterministic radial boundary warp
- optional deterministic angular warp
- priority for intentional overlap resolution

## Catalogs

`RegionCatalog` provides immutable runtime lookup through `Get` and `TryGet`.

`MacroRegionCatalog` is an immutable ordered set of macro layout rules. `MacroRegionCatalog.CreateDefault()` supplies four prototype sectors around the origin.

Projects can construct their own catalogs and inject a custom resolver into `RegionBiomePass`. The default `ProceduralWorldGenerator` also supports the built-in macro layout through `WorldGenerationSettings.macroRegionsEnabled`.

## Resolver flow

```text
EnvironmentSample + World Position
              |
              v
  IPositionAwareRegionResolver
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

`IPositionAwareRegionResolver` extends the existing `IRegionResolver` contract rather than replacing it. Legacy environment-only resolvers continue to work unchanged.

`RadialSectorRegionResolver` is deterministic for a given seed, coordinate, and catalog. It evaluates radial and angular membership, applies optional boundary warping, resolves overlaps by priority and then score, and falls back to a configured region when no macro rule matches.

This layer establishes macro-scale geography independently from terrain, topology, resources, structures, and rendering. Later generation passes can consume the resulting stable `RegionId` to specialize local content.
