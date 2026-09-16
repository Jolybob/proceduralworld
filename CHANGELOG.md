# Changelog

## [0.1.11] - 2026-09-17

- Restored the complete runtime package tree after the `main` branch had been reduced to package-root files, which prevented the generated 2D map from appearing in Play mode.
- Restored the Runtime generator, generation pipeline, noise implementation, Tilemap adapter, assembly definitions, tests, and Unity metadata from the last complete package state.
- Bumped the package version to 0.1.11.
- Incremented package version for this update.

## [0.1.10] - 2026-09-17

- Added missing Unity `.meta` files for the package root `package.json` and `CHANGELOG.md`.
- Bumped the package version to 0.1.10.
- Incremented package version for this update.

## [0.1.9] - 2026-09-17

- Published the previously prepared 0.1.8 architecture changes on the `main` branch so Git-based UPM installs receive them.
- Bumped the package version to 0.1.9 to identify this published repository state.
- Incremented package version for this update.

## [0.1.8] - 2026-09-17

- Wired `RegionBiomePass` to the `IRegionResolver` abstraction instead of hardcoded biome selection.
- Made the default `ThresholdRegionResolver` responsible for core/cold/wet region thresholds.
- Corrected terrain mapping for all prototype region IDs, including region 4.
- Added constructor injection so custom region resolvers can be supplied without changing the generation pipeline.
- Incremented package version for this update.

## [0.1.7] - 2026-09-17

- Fixed missing `RegionId` and `EnvironmentSample` definitions required by `RegionBiomePass`.
- Added Unity metadata for the Regions folder and region types.
- Incremented package version for this update.

## [0.1.6] - 2026-09-17

- Refactored the default generator into explicit region and terrain stages.
- Added deterministic temperature and moisture fields for biome selection.
- Added `RegionBiomePass` for environmental region identity.
- Added `TerrainPass` to keep terrain selection separate from biome logic.
- Added clearer Runtime architecture documentation by responsibility.
- Added Unity metadata for new package assets.
- Incremented package version for this update.

## [0.1.5] - 2026-09-17

- Added a reusable ordered generation-pass pipeline.
- Added a pluggable deterministic `INoiseField` abstraction and seeded multi-octave Perlin implementation.
- Moved the prototype radial biome logic into `RadialBiomePass`.
- Added configurable noise octaves, persistence, and lacunarity to world generation settings.
- Kept the existing generator API compatible while allowing custom pipelines.
- Incremented package version for this update.

## [0.1.4] - 2026-09-17

- Removed temporary Tilemap fix marker files that were being imported as package assets without Unity metadata.
- Incremented package version for this update.

## [0.1.3] - 2026-09-17

- Fixed the `Tilemap` namespace/type collision in `ProceduralWorldTilemap` by explicitly aliasing Unity's Tilemap type.
- Added missing Unity metadata for package documentation assets.
- Incremented package version for this update.

## [0.1.2] - 2026-09-17

- Added Unity metadata for package documentation assets.
- Fixed the Tilemap namespace/type collision in the Tilemap adapter.
- Package version incremented for this update.

## [0.1.1]

- Added Unity package metadata and test assembly fixes.
