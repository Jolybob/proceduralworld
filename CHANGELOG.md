# Changelog

## [0.1.32] - 2026-09-17

- Added `IWorldChangeListener` for reactive consumers of successful world edits.
- Added `WorldChangeObserverJournal` as a journal decorator that preserves the existing change-history source of truth while publishing changes to multiple observers.
- Added disposable subscriptions with stable registration-order notification and snapshot iteration so subscriptions can safely change during callbacks.
- Kept no-op and failed edits out of the notification stream because they are not recorded by `WorldEditService`.
- Added regression tests for exact change forwarding, no-op suppression, unsubscription, and deterministic multi-observer ordering.
- Changed the architecture preview and editing test seed from `105827` to `116503` for this update.
- Updated package architecture and world-editing documentation with the notification boundary.
- Bumped the package version to 0.1.32.
- Incremented package version for this update.

## [0.1.31] - 2026-09-17

- Added `WorldEditHistoryEntry` to represent named groups of cell changes as one undo/redo operation.
- Added `WorldEditHistory` to build grouped history entries from `IWorldChangeJournal` records.
- Added exact before/after undo and redo application through `IWorldChunkAccess`.
- Added automatic capture of pending journal changes when undo or redo is requested without an explicit commit.
- Added redo-branch invalidation when a new edit is committed after undo.
- Added history clearing support and safe reset behavior when the underlying journal is cleared.
- Added regression tests for grouped edits, exact undo/redo restoration, reverse-order undo, redo invalidation, and pending edits.
- Changed the architecture preview and edit-history test seed from `93417` to `105827`.
- Updated package architecture documentation with the undo/redo history boundary.
- Bumped the package version to 0.1.31.
- Incremented package version for this update.

## [0.1.30] - 2026-09-17

- Fixed `NamedEditOperationsRecordBeforeAndAfterState` so its tile edit always changes state instead of accidentally becoming a no-op when the generated tile is already `WorldTile.Core`.
- Kept no-op suppression in `WorldEditService`; the regression now selects the opposite tile from the current state before testing the named operation sequence.
- Changed the architecture preview seed from `82641` to `93417` for this fix revision.
- Updated world-editing documentation to clarify that named operations are journaled only when they actually change state.
- Bumped the package version to 0.1.30.
- Incremented package version for this update.

## [0.1.29] - 2026-09-17

- Added `WorldEditOperationKind` to classify gameplay/world mutations without coupling them to presentation or persistence.
- Added `WorldCellChange` containing world position plus complete before/after generated-cell state.
- Added `IWorldChangeJournal` as a backend-neutral change-history boundary.
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
