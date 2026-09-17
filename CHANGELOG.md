# Changelog

## [0.1.23] - 2026-09-17

- Fixed package test discovery documentation for Git-installed UPM packages.
- Documented the required project `Packages/manifest.json` `testables` entry for `com.jolybob.proceduralworld` tests.
- Confirmed the package test assembly remains configured with the Unity test-assembly support and the runtime assembly reference.
- Changed the Tilemap preview and primary generator test seed from `75319` / `86420` to `68124` for this revision.
- Bumped the package version to 0.1.23.
- Incremented package version for this update.

## [0.1.22] - 2026-09-17

- Added `IWorldChunkSink` as the rendering/persistence-independent boundary for chunk load and unload operations.
- Added `ChunkStreamingPlanner` to track active chunk coordinates and compute deterministic load/unload deltas.
- Added configurable load/unload radii, including unload hysteresis for smoother chunk lifetime management.
- Added `ChunkStreamingDelta` with stable Y-then-X coordinate ordering for predictable consumers and tests.
- Added `WorldChunkStreamingController` to connect chunk planning to the existing deterministic `ProceduralWorldGenerator`.
- Kept chunk generation rules independent from streaming and presentation.
- Added streaming regression tests covering initial loads, unchanged centers, hysteresis, unload boundaries, controller behavior, and deterministic generated data.
- Changed the Tilemap preview seed from `86420` to `75319` for this architecture update.
- Added chunk-streaming architecture documentation.
- Bumped the package version to 0.1.22.
- Incremented package version for this update.

## [0.1.21] - 2026-09-17

- Added `IWorldPostProcessStep` for composable, ordered world-data modifications after the main generation passes.
- Added `WorldPostProcessPipeline` with deterministic ordering by step `Order`.
- Added `WorldPostProcessContext` with chunk access, world-coordinate helpers, settings access, and a step-isolated deterministic random stream.
- Added `WorldPostProcessPass` at order `900` so post-processing has a stable insertion point after structures and before future streaming/persistence stages.
- Added a new `ProceduralWorldGenerator` constructor overload for injecting a post-process pipeline while preserving all existing constructor signatures.
- Kept the default post-process stage empty so existing generated output remains unchanged unless a project explicitly adds modifications.
- Added deterministic, ordering, and salted-stream regression tests for post-processing.
- Changed the Tilemap preview and primary deterministic test seed from `97531` to `86420` for this architecture update.
- Added post-process architecture documentation and usage examples.
- Bumped the package version to 0.1.21.
- Incremented package version for this update.

## [0.1.20] - 2026-09-17

- Added canonical `StructureId` state to generated cells.
- Added `StructureDefinition` and `StructureCatalog` for data-driven structure metadata.
- Added multi-cell footprint support with region and terrain eligibility rules.
- Added `StructurePass` as a deterministic post-resource structure placement stage.
- Added `GeneratedCellFlags.HasStructure` and `SetStructure` / `ClearStructure` helpers.
- Integrated `StructureCatalog` into `WorldGenerationContext` and the default generation pipeline.
- Added structure enablement settings while keeping structures disabled by default.
- Prevented structure footprints from overlapping caves, resources, or other structures.
- Added deterministic structure-placement and footprint regression tests.
- Changed the Tilemap preview and primary deterministic test seed from `24680` to `97531` for this architecture update.
- Added structure-layer documentation.
- Bumped the package version to 0.1.20.
- Incremented package version for this update.

## [0.1.19] - 2026-09-17

- Added `ResourceId`, `ResourceDefinition`, and `ResourceCatalog` for data-driven resource metadata.
- Added canonical resource state to `GeneratedCell` with `SetResource` and `ClearResource` helpers.
- Added `GeneratedCellFlags.HasResource` to record resource occupancy without coupling generation to rendering.
- Added `ResourcePass` as a deterministic post-cave resource placement stage.
- Added salted deterministic RNG streams so each resource type has an independent sequence within the Resources domain.
- Integrated `ResourceCatalog` into `WorldGenerationContext` and the default generation pipeline.
- Added resource enablement settings while keeping resources disabled by default for backward-compatible world output.
- Changed the Tilemap preview seed from `12345` to `24680` for this architecture update.
- Added deterministic resource-placement and resource-state regression tests.
- Bumped the package version to 0.1.19.
- Incremented package version for this update.

## [0.1.18] - 2026-09-17

- Preserved backward compatibility for integrations that still write `GeneratedCell.Biome` directly before terrain generation.
- Updated `TerrainPass` to re-synchronize legacy `Biome` writes into canonical `RegionId` state.
- Added a regression test for legacy biome compatibility.
- Bumped the package version to 0.1.18.
- Incremented package version for this update.

## [0.1.17] - 2026-09-17

- Promoted `RegionId` and `TerrainId` to canonical generated-cell state.
- Added `GeneratedCell.SetRegion` and `GeneratedCell.SetTerrain` helpers so compatibility mirrors stay synchronized.
- Added `WorldRandomDomain`, `IWorldRandom`, `DeterministicWorldRandom`, and `WorldRandomService` for deterministic per-world, per-domain, per-chunk random streams.
- Updated `WorldGenerationContext` and `ProceduralWorldGenerator` to expose the shared deterministic random service.
- Updated region and terrain passes to write/read canonical IDs instead of raw byte mappings.
- Added tests for canonical cell state, compatibility mirrors, deterministic random streams, and independent random domains.
- Bumped the package version to 0.1.17.
- Incremented package version for this update.

## [0.1.16] - 2026-09-17

- Added `GeneratedCellFlags` to let generation passes annotate cell modifications without coupling those changes to rendering.
- Added `ICaveFieldProvider` and deterministic `DefaultCaveFieldProvider`.
- Added `CavePass` as an independent post-terrain modifier stage.
- Added configurable cave settings while keeping caves disabled by default for backward-compatible world output.
- Updated `WorldGenerationContext` and `ProceduralWorldGenerator` to support injected cave field providers.
- Added deterministic and injected cave-generation tests.
- Added cave-layer documentation.
- Bumped the package version to 0.1.16.
- Incremented package version for this update.

## [0.1.15] - 2026-09-17

- Added stable `TerrainId` identifiers for data-driven terrain definitions.
- Added `RegionDefinition` and `RegionCatalog` for immutable region metadata and region-to-terrain defaults.
- Added `TerrainDefinition` and `TerrainCatalog` for immutable terrain metadata and rendering tile mapping.
- Refactored `TerrainPass` to resolve region and terrain data through catalogs instead of a hardcoded region-to-tile switch.
- Added constructor injection for custom region and terrain catalogs while preserving existing generator constructors.
- Preserved the existing `GeneratedCell.Biome` and `GeneratedCell.Tile` fields for backward compatibility.
- Added deterministic/custom-catalog tests and duplicate-ID validation coverage.
- Added documentation for the region definition/catalog layer.
- Bumped the package version to 0.1.15.
- Incremented package version for this update.

## [0.1.14] - 2026-09-17

- Added a dedicated field layer with `IEnvironmentFieldProvider` and `DefaultEnvironmentFieldProvider`.
- Moved `EnvironmentSample` into the field layer so it can be shared by regions, terrain, caves, resources, and structures.
- Updated `WorldGenerationContext` to expose reusable environment fields while preserving the existing noise API.
- Refactored `RegionBiomePass` so it consumes field samples and no longer owns temperature/moisture noise generation.
- Added constructor injection for custom environment field providers.
- Added field-layer documentation and deterministic/custom-provider tests.
- Bumped the package version to 0.1.14.
- Incremented package version for this update.

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
