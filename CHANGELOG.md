# Changelog

## [0.1.26] - 2026-09-17

- Fixed Unity compilation compatibility in `WorldCellModification.GetHashCode` by removing the unsupported 9-argument `HashCode.Combine` call.
- Replaced it with stable two-stage hash composition compatible with the Unity/.NET profile used by the package.
- Changed the Tilemap preview and primary test seed from `43017` to `51746` for this fix revision.
- Bumped the package version to 0.1.26.
- Incremented package version for this update.

## [0.1.25] - 2026-09-17

- Added `WorldCellModification` for sparse persisted cell overrides.
- Added `WorldChunkSaveData` with explicit format versioning for future save migrations.
- Added `IWorldChunkStore` as a backend-neutral persistence boundary.
- Added `WorldChunkPersistenceService` to regenerate deterministic chunks, apply saved overrides, and save only cells that differ from generated state.
- Added `InMemoryWorldChunkStore` for tests and prototypes.
- Kept persistence independent from rendering and chunk streaming.
- Added persistence regression tests for sparse saves, restoration, cleanup, determinism, and invalid chunk data.
- Changed the Tilemap preview seed from `59273` to `43017` for this architecture update.
- Added persistence-layer documentation and integration guidance.
- Bumped the package version to 0.1.25.
- Incremented package version for this update.

## [0.1.24] - 2026-09-17

- Fixed the brittle `DifferentSeedsUsuallyProduceDifferentData` regression test that incorrectly required two seeds to produce different coarse region/terrain output in one specific chunk.
- Replaced it with `DifferentSeedsProduceDifferentEnvironmentFields`, which verifies seed influence directly across multiple world positions.
- Preserved the generator's deterministic coarse region behavior instead of changing world-generation rules to satisfy a probabilistic test.
- Changed the Tilemap preview and primary test seed from `68124` to `59273` for this revision.
- Bumped the package version to 0.1.24.
- Incremented package version for this update.

## [0.1.23] - 2026-09-17

- Fixed package test discovery documentation for Git-installed UPM packages.
- Documented the required project `Packages/manifest.json` `testables` entry for `com.jolybob.proceduralworld` tests.
- Confirmed the package test assembly remains configured with Unity test-assembly support and the runtime assembly reference.
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
