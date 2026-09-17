# Changelog

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
