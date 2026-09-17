# Changelog

## [0.1.83] - 2026-09-17

- Fixed `WorldPresentationRegionDemandChange` to own an immutable copy of its demanded-region snapshot.
- Suppressed redundant `DemandChanged` notifications when reconciliation does not change demanded region state.
- Prevented disposal cleanup from publishing a final demand-change notification while still releasing residency deterministically.
- Preserved deterministic demand ordering and existing aggregation/planning/residency behavior.
- Bumped the package version to 0.1.83.
- Incremented package version for this update.

## [0.1.82] - 2026-09-17

- Added `WorldPresentationRegionDemandChange` as an immutable notification payload for reconciled region demand state.
- Added `DemandChanged` notifications and a deterministic read-only `DemandedRegions` snapshot to `WorldPresentationRegionDemandCoordinator`.
- Preserved separation between demand aggregation, residency execution, and downstream observers.
- Added regression coverage for deterministic snapshots, snapshot stability, and post-disposal notification safety.
- Added a Unity `.meta` file for the new runtime asset.
- Bumped the package version to 0.1.82.
- Incremented package version for this update.

## [0.1.81] - 2026-09-17

- Made `WorldPresentationRegionDemandSourceCoordinator` explicitly disposable so source subscriptions and active demand are released through one lifecycle boundary.
- Added `IsDisposed` and a deterministic read-only `RegisteredSourceIds` view for coordinator state inspection.
- Hardened registration, refresh, batching, and event handling against use after disposal.
- Ensured disposing an active coordinator detaches all reactive subscriptions and releases demanded regions exactly once.
- Added regression coverage for lifecycle cleanup, post-disposal no-op behavior, source-order inspection, and disposed batches.
- Bumped the package version to 0.1.81.
- Incremented package version for this update.

## [0.1.80] - 2026-09-17

- Fixed the runtime Tilemap visual resolver so the default `CellTopology.Solid` state does not override ordinary terrain visuals.
- Preserved explicit water, lava, and chasm topology overrides while allowing terrain catalogs to render normal solid cells.
- Kept the existing runtime Tilemap material, renderer, and camera visibility fixes from 0.1.79.
- Bumped the package version to 0.1.80.
- Incremented package version for this update.

## [0.1.79] - 2026-09-17

- Fixed runtime Tilemap preview rendering by assigning an explicit unlit sprite material when a compatible shader is available.
- Normalized TilemapRenderer sorting layer and order so generated cells are not hidden by unexpected renderer settings.
- Hardened the 2D preview camera by resetting its rotation, enforcing a valid negative Z position, and using orthographic framing for the generated world.
- Preserved deterministic runtime tile generation and existing topology/terrain fallback behavior.
- Bumped the package version to 0.1.79.
- Incremented package version for this update.
