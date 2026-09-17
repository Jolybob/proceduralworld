# Changelog

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

## [0.1.40] - 2026-09-17

- Added `WorldScheduledPersistentChunkStreamingController` to combine deterministic generation scheduling with persistence-aware chunk lifecycle management.
- Pending chunk generations are budgeted and cancellable before persistence loading occurs.
- Completed scheduled chunks enter the controller's active `IWorldChunkAccess` state only after processing budget is granted.
- Loaded persistent chunks are saved before unload and before reset, matching the immediate persistent streaming lifecycle.
- Added regression tests covering deferred persistent loads, bounded processing, cancellation through movement, and save-before-unload behavior.
- Changed the preview seed from `196423` to `207341`.
- Bumped the package version to 0.1.40.
- Incremented package version for this update.

## [0.1.39] - 2026-09-17

- Added `IWorldChunkGenerator` to isolate chunk generation from streaming and scheduling.
- Added `IChunkGenerationScheduler` and `DeterministicChunkGenerationScheduler` for stable, cancellable, duplicate-safe generation queues.
- Added `ChunkGenerationRequest` with explicit integer priority and deterministic insertion ordering.
- Added `BudgetedChunkGenerationService` so callers can cap generation work per update without introducing worker-thread or Unity-object access concerns.
- Added `WorldScheduledChunkStreamingController` to separate activation planning from generation execution while preserving deterministic load order.
- Pending generation work is cancelled when streaming unloads a coordinate before it has been generated.
- Preserved `WorldChunkStreamingController` as the immediate-generation compatibility path.
- Added regression tests for scheduler priority, stable ordering, duplicate coalescing, cancellation, generation budgets, and scheduled streaming movement.
- Changed the preview seed from `184667` to `196423`.
- Bumped the package version to 0.1.39.
- Incremented package version for this update.

## [0.1.38] - 2026-09-17

- Fixed `NearestFirstChunkStreamingOrder` tie-breaking so equal-priority chunks follow the documented deterministic order used by the streaming tests.
- Fixed `ChunkStreamingPlanner` radius iteration at extreme `int` coordinates by performing boundary calculations in `long` and casting each generated coordinate only after the bounds are established.
- Preserved the nearest-first active-load strategy while keeping unload ordering deterministic.
- Updated the legacy `ChunkStreamingTests` expectation to match the intentional nearest-first default introduced by the streaming-order architecture.
- Added regression coverage for deterministic tie ordering and extreme-coordinate load generation.
- Changed the preview seed from `173921` to `184667`.
- Bumped the package version to 0.1.38.
- Incremented package version for this update.

## [0.1.37] - 2026-09-17

- Added `IChunkStreamingOrder` to separate deterministic chunk load prioritization from active-set calculation.
- Added `NearestFirstChunkStreamingOrder` as the default strategy, prioritizing chunks by Manhattan distance from the streaming center with deterministic Y/X tie-breaking.
- Updated `ChunkStreamingPlanner` to accept a custom load-order strategy without changing its existing radius or active-chunk semantics.
- Hardened chunk-distance and boundary arithmetic with `long` calculations so extreme integer chunk coordinates cannot overflow during prioritization or radius checks.
- Added regression tests for nearest-first ordering, deterministic tie-breaking, custom ordering, and extreme coordinate safety.
- Changed the architecture preview and streaming test seed from `161729` to `173921`.
- Updated streaming documentation with the scheduling boundary and custom ordering contract.
- Updated the preview component seed to `173921`.
- Bumped the package version to 0.1.37.
- Incremented package version for this update.
