# Changelog

## [0.1.77] - 2026-09-17

- Fixed the procedural Tilemap preview bootstrap so an existing Tilemap also receives a required `TilemapRenderer`.
- Ensured the TilemapRenderer is enabled and uses deterministic bottom-left sorting for the generated preview.
- Hardened preview camera setup by centering on the generated world, correcting a non-negative camera Z that would place the camera on the Tilemap plane, and framing the complete preview for orthographic cameras.
- Added an opt-out `frameCameraOnWorld` setting for projects that manage their own camera framing.
- Bumped the package version to 0.1.77.
- Incremented package version for this update.

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

## [0.1.73] - 2026-09-17

- Integrated `WorldPresentationRegionDemandAggregator` into `WorldPresentationRegionDemandCoordinator` so multiple independent demand sources reconcile through the existing planner and residency layers.
- Added source demand registration, replacement, removal, and aggregate reconciliation APIs while preserving the legacy single-demand API.
- Preserved deterministic first-seen aggregation and planner load/unload ordering.
- Added regression coverage for shared regions, source removal, idempotent reconciliation, and disposal.
- Bumped the package version to 0.1.73.
- Incremented package version for this update.

## [0.1.72] - 2026-09-17

- Added `WorldPresentationRegionDemandAggregator` to compose region demand from multiple independent consumers before residency reconciliation.
- Preserved deterministic source registration order and first-seen region order across all demand sources.
- Added source replacement, removal, shared-region retention, and empty-state regression coverage.
- Added Unity `.meta` files for the new runtime and test assets.
- Bumped the package version to 0.1.72.
- Incremented package version for this update.

## [0.1.71] - 2026-09-17

- Fixed the deterministic demand planner regression test to match the planner's documented first-seen demand order.
- Preserved reverse current-residency order for unload operations.
- Bumped the package version to 0.1.71.
- Incremented package version for this update.

## [0.1.70] - 2026-09-17

- Fixed a Unity asset GUID collision caused by duplicate `WorldPresentationRegionResidencyCoordinator.cs` and `WorldPresentationRegionResidency.cs` source assets sharing the same GUID.
- Kept `WorldPresentationRegionResidency.cs` as the canonical source owner and moved the public `LoadedRegions` read-only snapshot onto that canonical asset.
- Removed the duplicate residency source and duplicate `.meta` file so Unity Package Manager can import the package without ignoring the residency asset.
- Bumped the package version to 0.1.70.
- Incremented package version for this update.
