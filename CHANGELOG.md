# Changelog

## [0.1.13] - 2026-09-17

- Fixed the generation layer so `RegionBiomePass`, `RegionId`, `EnvironmentSample`, and `TerrainPass` are present together and compile as one coherent runtime API.
- Restored Unity metadata for the Regions folder and region generation assets.
- Updated `RegionBiomePass` to use the injectable `IRegionResolver` while keeping the default resolver deterministic.
- Restored terrain mapping for prototype region ID 4.
- Bumped the package version to 0.1.13.
- Incremented package version for this update.

## [0.1.12] - 2026-09-17

- Fixed missing region and terrain generation assets that caused `ProceduralWorldGenerator` compilation errors.
- Restored Unity `.meta` files for the Regions folder and generation pass assets.
- Restored `RegionId`, `EnvironmentSample`, and `IRegionResolver` definitions required by the region generation pipeline.
- Bumped the package version to 0.1.12.
- Incremented package version for this update.

## [0.1.11] - 2026-09-17

- Restored the complete runtime package tree after the `main` branch had been reduced to package-root files, which prevented the generated 2D map from appearing in Play mode.
- Restored the Runtime generator, generation pipeline, noise implementation, Tilemap adapter, assembly definitions, tests, and Unity metadata from the last complete package state.
- Bumped the package version to 0.1.11.
- Incremented package version for this update.

## [0.1.10] - 2026-09-17

- Added missing Unity `.meta` files for the package root `package.json` and `CHANGELOG.md`.
- Bumped the package version to 0.1.10.
- Incremented package version for this update.
