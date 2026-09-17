# Changelog

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

## [0.1.35] - 2026-09-17

- Added `IWorldChangeRenderer` as a presentation boundary for converting canonical world changes into rendering or other presentation updates.
- Added `WorldChangeRenderObserver` to bridge `WorldChangeObserverJournal` notifications into presentation code without coupling world state to a renderer implementation.
- Direct edits are delivered through `Render(WorldCellChange)`, while transaction commits are delivered once through `RenderBatch(WorldChangeBatch)` to avoid per-cell rendering duplication.
- Added deterministic disposal so both notification subscriptions are released safely and repeatedly disposing an observer is harmless.
- Added regression tests for direct rendering notifications, transaction-scale rendering batches, and disposal behavior.
- Changed the architecture preview and rendering test seed from `138427` to `149863`.
- Updated world-editing architecture documentation with the presentation adapter boundary.
- Bumped the package version to 0.1.35.
- Incremented package version for this update.

## [0.1.34] - 2026-09-17

- Added `WorldChangeBatch` as an immutable snapshot for related cell changes published as one logical world operation.
- Added `IWorldChangeBatchListener` for reactive systems that need transaction-scale notifications instead of one callback per cell.
- Added `IWorldChangeBatchJournal` as an optional journal capability that preserves per-cell history while exposing atomic logical batching.
- Extended `WorldChangeObserverJournal` with batch subscriptions and deterministic batch notification ordering without changing existing per-cell subscriptions.
- Updated `WorldEditTransaction.Commit()` to publish one batch when the target journal supports batching, while preserving per-change behavior for existing journals.
- Added regression tests for transaction batching, direct per-cell notification compatibility, empty batches, and legacy journal fallback.
- Changed the architecture preview and batching test seed from `127931` to `138427`.
- Updated package architecture and world-editing documentation with the logical change-batching boundary.
- Bumped the package version to 0.1.34.
- Incremented package version for this update.

## [0.1.33] - 2026-09-17

- Added `WorldEditTransaction` for isolated multi-edit workflows built on the existing `WorldEditService`.
- Transaction edits are recorded in a private journal and are not published to the target journal until `Commit()`.
- `Rollback()` restores every changed cell to its original before-state in reverse order without publishing transaction changes.
- Completed transactions reject further reads or writes so transaction lifecycle is explicit and deterministic.
- Added regression tests for grouped commit, rollback, empty transactions, and completed-transaction safety.
- Changed the architecture preview and transaction test seed from `116503` to `127931`.
- Updated world-editing documentation with transaction usage and lifecycle rules.
- Bumped the package version to 0.1.33.
- Incremented package version for this update.

## [0.1.32] - 2026-09-17

- Added `IWorldChangeListener` for reactive consumers of successful world edits.
- Added `WorldChangeObserverJournal` as a journal decorator that preserves the existing change-history source of truth while publishing changes to multiple observers.
- Added disposable subscriptions with stable registration-order notification and snapshot iteration so subscriptions can safely change during callbacks.
- Kept no-op and failed edits out of the notification stream because they are not recorded by `WorldEditService`.
- Added regression tests for exact change forwarding, no-op suppression, unsubscription, and deterministic multi-observer ordering.
- Changed the architecture preview and editing test seed from `105827` to `116503`.
- Updated package architecture and world-editing documentation with the notification boundary.
- Bumped the package version to 0.1.32.
- Incremented package version for this update.

## [0.1.31] - 2026-09-17

- Added `WorldEditHistoryEntry` to represent named groups of changes as one undo/redo operation.
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
