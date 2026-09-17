# Changelog

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

## [0.1.43] - 2026-09-17

- Fixed scheduling regression tests that referenced APIs not present in the canonical world model (`GeneratedCell.SetTile` and `InMemoryWorldChunkStore.Contains`).
- Updated tile mutation coverage to use the existing `GeneratedCell.SetTerrain` API while preserving the expected rendered tile state.
- Updated persistence assertions to use the existing `IWorldChunkStore.TryLoad` contract instead of adding a test-only store API.
- Kept the runtime API unchanged; the correction is isolated to tests and package integration validation.
- Changed the preview seed from `231509` to `243731`.
- Bumped the package version to 0.1.43.
- Incremented package version for this update.

## [0.1.42] - 2026-09-17

- Fixed a duplicate `WorldScheduledPersistentChunkStreamingController` definition that caused Unity compilation errors when both scheduled-persistent source files were imported.
- Removed the redundant `ScheduledPersistentChunkStreaming.cs` implementation and its Unity metadata, keeping the canonical `PersistentChunkGenerationScheduling.cs` implementation as the single source of truth.
- Preserved the 0.1.41 scheduled persistent streaming API and behavior without changing its public contract.
- Changed the preview seed from `219877` to `231509`.
- Bumped the package version to 0.1.42.
- Incremented package version for this update.

## [0.1.41] - 2026-09-17

- Added `WorldScheduledPersistentChunkStreamingController` as the combined scheduling + persistence streaming boundary.
- Pending chunk generation is now budgeted before `WorldChunkPersistenceService.LoadChunk` runs, so deferred chunks do not consume persistence work until actually processed.
- Added loaded-world access through `IWorldChunkAccess` to the scheduled persistent controller, matching the immediate persistent controller contract.
- Preserved save-before-unload and save-before-reset semantics for already loaded chunks while cancelling not-yet-generated work first.
- Added regression tests for deferred persistent loading, zero-budget behavior, pending cancellation without persistence writes, save/restore across unload, and reset cleanup.
- Changed the preview seed from `207341` to `219877`.
- Bumped the package version to 0.1.41.
- Incremented package version for this update.
