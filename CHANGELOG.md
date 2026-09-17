# Changelog

## [0.1.50] - 2026-09-17

- Added `LiquidTopologyPass` as the first reusable liquid-topology modifier.
- Added deterministic water and lava classification from environment elevation, moisture, and temperature fields.
- Added `liquidsEnabled` plus water/lava threshold settings; liquids remain disabled by default for compatibility.
- Preserved existing non-solid topology so liquids never overwrite caves, chasms, or other topology decisions.
- Kept liquid generation data-only, leaving fluid simulation and rendering to later systems.
- Added regression coverage for disabled behavior, water/lava classification, topology preservation, and deterministic generation.
- Added Unity `.meta` files for the liquid topology runtime and tests.
- Bumped the package version to 0.1.50.
- Incremented package version for this update.

## [0.1.49] - 2026-09-17

- Added a dedicated `TopologyPipeline` so topology modifiers are composed independently from the main generation pipeline.
- Added `TopologyPass` as the single integration boundary for topology generation at order `350`.
- Moved `ChasmPass` from `IWorldGenerationPass` to `ITopologyModifier`, making chasms one topology feature instead of a pipeline-level special case.
- Exposed the topology pipeline through `WorldGenerationContext` and `ProceduralWorldGenerator` for custom topology modifiers without replacing unrelated generation stages.
- Added regression coverage for topology modifier ordering and deterministic execution order.
- Added Unity `.meta` files for the new topology pipeline and tests.
- Bumped the package version to 0.1.49.
- Incremented package version for this update.

## [0.1.48] - 2026-09-17

- Fixed the topology generation contract so `ChasmPass` compiles against the canonical `WorldGenerationSettings` and `GeneratedCell` APIs.
- Added canonical `CellTopology` state to generated cells with `SetTopology`, including the `Chasm` flag.
- Added chasm settings to `WorldGenerationSettings` and integrated `ChasmPass` into the default generation pipeline.
- Updated cave carving to keep topology state synchronized with empty tiles.
- Updated persistence equality and hashing to include topology state.
- Fixed the chasm minimum-distance calculation to use integer arithmetic without an invalid float-to-long conversion.
- Bumped the package version to 0.1.48.
- Incremented package version for this update.

## [0.1.47] - 2026-09-17

- Added missing Unity `.meta` files for the topology folder, topology scripts/documentation, and topology runtime tests.
- Prevents Unity Package Manager immutable-folder warnings from causing topology assets to be ignored when the package is installed under `Packages/`.
- Preserved the topology runtime API and generation behavior.
- Bumped the package version to 0.1.47.
- Incremented package version for this update.

## [0.1.46] - 2026-09-17

- Added `MacroRegionDefinition` and immutable `MacroRegionCatalog` for configurable large-scale radial/sector world layouts.
- Added `IPositionAwareRegionResolver` so region resolvers can use deterministic world-space coordinates without breaking existing field-only resolvers.
- Added `RadialSectorRegionResolver` with radial bounds, angular sectors, deterministic boundary noise, radial/angular warping, priorities, stable overlap resolution, and fallback regions.
- Added `macroRegionsEnabled` to `WorldGenerationSettings`; the default generator can now opt into macro-region layout while preserving the existing threshold resolver by default.
- Added regression coverage for resolver determinism, sector selection, fallback behavior, and generator integration.
- Bumped the package version to 0.1.46.
- Incremented package version for this update.

## [0.1.45] - 2026-09-17

- Added explicit `ChunkStreamingState` lifecycle states for scheduled streaming: `Inactive`, `Pending`, and `Loaded`.
- Added `IChunkGenerationScheduler.Contains` so schedulers can expose whether a coordinate is currently waiting for generation without consuming the request.
- Added `GetState(ChunkCoord)` to scheduled and scheduled-persistent controllers for deterministic lifecycle inspection by gameplay, UI, and orchestration code.
- Scheduled controllers now track loaded coordinates explicitly, keeping pending work and completed activation separate from the planner's active set.
- Added regression coverage for pending, loaded, and inactive transitions plus scheduler membership inspection.
- Preserved the existing generation, persistence, sink, and scheduling behavior and constructor APIs.
- Changed the preview seed from `258947` to `281604`.
- Bumped the package version to 0.1.45.
- Incremented package version for this update.

## [0.1.44] - 2026-09-17

- Fixed the scheduled persistence regression tests that could accidentally write the same tile value as deterministic generation and therefore produce no persisted modification.
- Tile mutation tests now always choose a tile different from the generated value before validating save, unload, and restore behavior.
- Preserved the runtime world and persistence APIs; the correction is isolated to deterministic test setup.
- Changed the preview seed from `243731` to `258947`.
- Bumped the package version to 0.1.44.
- Incremented package version for this update.
