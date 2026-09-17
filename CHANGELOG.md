# Changelog

## [0.1.84] - 2026-09-17

- Fixed the demand snapshot stability regression test to retain each published snapshot independently across later reconciliations.
- Added explicit assertions for both the first and subsequent demand-change snapshots.
- Preserved the runtime snapshot ownership fix introduced in 0.1.83.
- Bumped the package version to 0.1.84.
- Incremented package version for this update.

## [0.1.83] - 2026-09-17

- Fixed `WorldPresentationRegionDemandChange` snapshots to own an immutable copy of the demanded-region sequence.
- Prevented disposal cleanup from publishing a final demand-change notification.
- Preserved deterministic reconciliation, load/unload ordering, and normal demand notifications.
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
