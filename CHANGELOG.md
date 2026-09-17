# Changelog

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

## [0.1.65] - 2026-09-17

- Fixed Unity compilation errors caused by presentation-region contracts being removed from `WorldPresentationDirtyRegionSet` without preserving their runtime definitions.
- Restored `IWorldPresentationRegionResolver` and `IWorldPresentationRegionRenderer` as dedicated presentation-region contracts so existing region-aware APIs compile cleanly.
- Added the corresponding Unity `.meta` file for the contract source.
- Bumped the package version to 0.1.65.
- Incremented package version for this update.
