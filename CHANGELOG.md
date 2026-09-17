# Changelog

## [0.1.28] - 2026-09-17

- Added `IWorldChunkAccess` as the narrow read/write boundary for currently loaded world chunks.
- Added `WorldChunkCoordinates` with correct floor-division mapping for positive and negative world positions.
- Added `WorldEditService` as a gameplay-facing mutation layer for cell, tile, resource, and structure edits.
- Connected `WorldPersistentChunkStreamingController` to `IWorldChunkAccess` so gameplay code does not depend directly on streaming internals.
- Ensured unloaded-world access fails safely instead of throwing when no chunks are active.
- Added regression tests for coordinate mapping, loaded-cell editing, and unloaded-cell rejection.
- Changed the architecture preview and persistent-streaming/edit test seed from `60427` to `71593`.
- Updated streaming documentation with the new world-access and editing boundaries.
- Bumped the package version to 0.1.28.
- Incremented package version for this update.

## [0.1.27] - 2026-09-17

- Added `WorldPersistentChunkStreamingController` to connect chunk streaming with the existing persistence service without coupling generation to storage or rendering.
- Added load-time restoration through `WorldChunkPersistenceService` and save-before-unload behavior for active chunks.
- Added lifecycle-safe `Reset()` behavior that saves and unloads every currently loaded chunk before clearing streaming state.
- Added persistent streaming regression tests covering restoration on load, save-before-unload, and reset cleanup.
- Updated the Tilemap preview seed from `51746` to `60427` for this architecture update.
- Updated streaming and package architecture documentation with the persistence-aware lifecycle.
- Bumped the package version to 0.1.27.
- Incremented package version for this update.

## [0.1.26] - 2026-09-17

- Fixed Unity compilation compatibility in `WorldCellModification.GetHashCode` by removing the unsupported 9-argument `HashCode.Combine` call.
- Replaced it with stable two-stage hash composition compatible with the Unity/.NET profile used by the package.
- Changed the Tilemap preview and primary test seed from `43017` to `51746` for this fix revision.
- Bumped the package version to 0.1.26.
- Incremented package version for this update.
