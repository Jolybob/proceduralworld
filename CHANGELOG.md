# Changelog

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

## [0.1.36] - 2026-09-17

- Added `WorldTilemapRenderer` as the first concrete Unity presentation adapter implementing both `IWorldChunkSink` and `IWorldChangeRenderer`.
- Chunk loads now render directly into a Tilemap and chunk unloads clear only the unloaded chunk bounds.
- Direct world edits update a single Tilemap position through the canonical `After.Tile` state.
- Transaction-scale rendering uses `Tilemap.SetTiles()` and coalesces repeated positions to the latest state in the logical batch.
- Missing `WorldTile` to `TileBase` mappings fail fast with a clear `KeyNotFoundException` instead of silently producing an invalid presentation.
- Updated `ProceduralWorldTilemap` to use the reusable `WorldTilemapRenderer` instead of maintaining a second chunk-rendering implementation.
- Added Tilemap integration tests for chunk coordinate mapping, unload clearing, direct cell updates, batch coalescing, and missing mappings.
- Updated the package test assembly to reference the Tilemap rendering assembly.
- Changed the architecture preview and rendering test seed from `149863` to `161729`.
- Updated root and editing architecture documentation with the concrete Tilemap adapter and streaming wiring.
- Bumped the package version to 0.1.36.
- Incremented package version for this update.
