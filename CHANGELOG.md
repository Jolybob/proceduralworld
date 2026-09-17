# Changelog

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

## [0.1.69] - 2026-09-17

- Added `WorldPresentationRegionDemandPlanner` as a pure, framework-neutral boundary that computes region load/unload transitions without performing lifecycle work.
- Added `WorldPresentationRegionDemandPlan` as an immutable description of deterministic load and unload operations.
- Updated `WorldPresentationRegionDemandCoordinator` to reconcile against actual residency rather than its previous demand snapshot.
- Exposed the deterministic loaded-region order from `WorldPresentationRegionResidencyCoordinator` as a read-only snapshot for planning.
- Preserved duplicate-demand elimination, first-seen load order, reverse teardown order, and idempotent residency behavior.
- Added Unity `.meta` files and regression coverage for demand planning and reconciliation.
- Bumped the package version to 0.1.69.
- Incremented package version for this update.

## [0.1.68] - 2026-09-17

- Added `WorldPresentationRegionDemandCoordinator` as the explicit boundary between desired region visibility and region residency.
- Added deterministic demand reconciliation so newly demanded regions load once and no-longer-demanded regions unload once.
- Preserved first-seen demand order while ignoring duplicate region requests.
- Added automatic reverse-demand-order clearing for predictable presentation teardown.
- Restored the 0.1.67 residency runtime and regression coverage on the architecture branch so the published lifecycle/residency contract remains internally complete.
- Added Unity `.meta` files for the demand runtime and tests.
- Bumped the package version to 0.1.68.
- Incremented package version for this update.
