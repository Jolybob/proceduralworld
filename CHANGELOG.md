# Changelog

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

## [0.1.67] - 2026-09-17

- Added `WorldPresentationRegionResidencyCoordinator` to make presentation-region load state explicit and idempotent.
- Prevented duplicate region loads and unloads through tracked residency state.
- Added deterministic reverse-load-order clearing so dependent presentation state can be released predictably.
- Kept residency orchestration framework-neutral and independent from concrete streaming or Tilemap implementations.
- Added regression coverage for idempotent transitions, deterministic clearing, and disposal behavior.
- Added Unity `.meta` files for the residency runtime and tests.
- Bumped the package version to 0.1.67.
- Incremented package version for this update.

## [0.1.66] - 2026-09-17

- Added `IWorldPresentationRegionLifecycle` as a framework-neutral load/unload boundary for region presentation and future chunk streaming.
- Added `WorldPresentationRegionLifecycleCoordinator` to guard region lifecycle operations with explicit disposal semantics.
- Kept lifecycle orchestration independent from generation, Tilemap, and concrete streaming implementations.
- Added regression coverage for region identity forwarding and post-disposal no-op behavior.
- Added Unity `.meta` files for the lifecycle runtime and tests.
- Bumped the package version to 0.1.66.
- Incremented package version for this update.
