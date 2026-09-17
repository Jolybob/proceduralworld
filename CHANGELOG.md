# Changelog

## [0.1.29] - 2026-09-17

- Added `WorldEditOperationKind` to classify gameplay/world mutations without coupling them to presentation or persistence.
- Added `WorldCellChange` containing world position plus complete before/after generated-cell state.
- Added `IWorldChangeJournal` as a backend-neutral boundary for change history, replay, networking, analytics, or undo/redo systems.
- Added `InMemoryWorldChangeJournal` for tests and prototypes.
- Updated `WorldEditService` so successful mutations are recorded after the underlying world access accepts them.
- Suppressed no-op edits from the journal and ensured failed edits never create change records.
- Added regression tests covering cell changes, no-op suppression, named edit operations, and failed edits.
- Updated the architecture preview seed from `71593` to `82641` for this revision.
- Added dedicated world-editing/change-tracking documentation.
- Bumped the package version to 0.1.29.
- Incremented package version for this update.

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
