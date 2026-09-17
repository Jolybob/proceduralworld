# Changelog

## [0.1.76] - 2026-09-17

- Added `WorldPresentationRegionDemandSourceBatch` as a scoped batching boundary for demand-source updates.
- Coalesced repeated reactive changes per source while preserving deterministic first-seen refresh order.
- Added nested batch support and deferred `RefreshAll` behavior inside active batches.
- Removed pending refresh work when a source is unregistered and preserved safe event detachment.
- Added regression coverage for coalescing, nesting, deferred refresh-all, and existing source lifecycle behavior.
- Added a Unity `.meta` file for the new runtime asset.
- Bumped the package version to 0.1.76.
- Incremented package version for this update.

## [0.1.75] - 2026-09-17

- Extended `IWorldPresentationRegionDemandSource` with a source-identified `DemandChanged` event for reactive demand updates.
- Updated `WorldPresentationRegionDemandSourceCoordinator` to subscribe and unsubscribe deterministically as sources are registered, replaced, unregistered, or cleared.
- Preserved pull-based `Refresh` and `RefreshAll` APIs for explicit reconciliation and compatibility.
- Added regression coverage for reactive refresh, stale-source detachment, and existing deterministic source behavior.
- Bumped the package version to 0.1.75.
- Incremented package version for this update.

## [0.1.74] - 2026-09-17

- Added `IWorldPresentationRegionDemandSource` for independent, pull-based region demand providers.
- Added `WorldPresentationRegionDemandSourceCoordinator` to register, replace, refresh, unregister, and clear demand sources deterministically.
- Kept aggregation, planning, residency, and lifecycle execution separated behind existing architecture boundaries.
- Added regression coverage for source registration, replacement, unregistration, shared demand, and clearing.
- Added Unity `.meta` files for the new runtime and test assets.
- Bumped the package version to 0.1.74.
- Incremented package version for this update.
