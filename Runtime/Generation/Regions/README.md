# Region definitions, catalogs, and macro layout

Regions are generated identities. They describe environmental outcomes without owning rendering.

## Definitions

`RegionDefinition` contains:

- a stable `RegionId`
- a display name and optional description
- the default `TerrainId` used by the terrain pass

Do not reuse a published region ID for a different meaning. Generated worlds depend on stable IDs for deterministic output and persistence compatibility.

## Catalogs

`RegionCatalog` provides immutable runtime lookup through `Get` and `TryGet`.

Use `RegionCatalog.CreateDefault()` for the package prototype. Projects can construct their own catalog and inject it into `ProceduralWorldGenerator` or `TerrainPass`.

A region catalog deliberately contains data only. Region selection remains the responsibility of `IRegionResolver` implementations.

## Macro regions

`MacroRegionDefinition` adds a world-layout layer for games that need large deterministic rings and sectors instead of only temperature/moisture thresholds.

A macro region is defined by:

- `MinRadius` and `MaxRadius` for a radial band
- `CenterAngle` and `AngularWidth` for an angular sector
- `BoundaryWarp` for wavy radial borders
- `BoundaryNoiseScale` for per-region default boundary noise frequency
- `AngularWarp` for wavy sector borders
- `Priority` for deliberate overlap resolution

`MacroRegionDefinition.FullRing(...)` creates a ring-shaped region. This is useful for central regions, walls, or other annular world features.

`IPositionAwareRegionResolver` extends `IRegionResolver` without breaking existing implementations. `RegionBiomePass` supplies the absolute `WorldPosition` to a position-aware resolver and falls back to the legacy `Resolve(EnvironmentSample)` method for existing resolvers.

`RadialSectorRegionResolver` combines radius, angle, deterministic seed rotation, and boundary noise. Seed rotation can be disabled with `rotateBySeed: false` when an authored layout must keep fixed absolute angles.

## Core Keeper-like example

```csharp
const float QuarterTurn = 1.57079632679f;

var macroRegions = new MacroRegionCatalog(new[]
{
    MacroRegionDefinition.FullRing(
        new RegionId(0),
        0f,
        140f),

    MacroRegionDefinition.FullRing(
        new RegionId(1),
        440f,
        520f,
        boundaryWarp: 24f,
        boundaryNoiseScale: 0.006f),

    new MacroRegionDefinition(
        new RegionId(2),
        140f,
        440f,
        0f,
        QuarterTurn * 2f,
        boundaryWarp: 18f,
        boundaryNoiseScale: 0.008f,
        angularWarp: 0.05f),

    new MacroRegionDefinition(
        new RegionId(3),
        140f,
        440f,
        QuarterTurn * 2f,
        QuarterTurn * 2f,
        boundaryWarp: 18f,
        boundaryNoiseScale: 0.008f,
        angularWarp: 0.05f)
});

var resolver = new RadialSectorRegionResolver(
    macroRegions,
    seed,
    fallbackRegion: new RegionId(0));

var pipeline = new WorldGenerationPipeline()
    .Add(new RegionBiomePass(resolver))
    .Add(new TerrainPass(regions, terrains));
```

The package does not hard-code Core Keeper content or radii. The example only demonstrates how a consuming game can express a ring-and-sector layout.

## Flow

```text
WorldPosition + EnvironmentSample
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
        TerrainPass
```

Macro regions determine **where** a region exists. Terrain, caves, resources, structures, persistence, and rendering remain separate generation or presentation responsibilities.
