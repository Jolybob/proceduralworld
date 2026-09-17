# Changelog

## [0.1.63] - 2026-09-17

- Added `IWorldPresentationRegionResolver` so presentation regions can map to chunks, streaming cells, or other caller-defined spatial units without coupling generation to a chunking strategy.
- Added `WorldPresentationDirtyRegionSet` to coalesce presentation changes by position and group them by region in deterministic first-seen order.
- Added `IWorldPresentationRegionRenderer` and `WorldPresentationRegionFlushCoordinator` so only affected regions need to be presented during a flush.
- Preserved the existing `WorldPresentationFlushCoordinator` and `IWorldChangeRenderer` path for consumers that do not need region-aware rendering.
- Added regression coverage for region grouping, per-region coalescing, draining, and validation.
- Added Unity `.meta` files for the region-aware presentation runtime and tests.
- Bumped the package version to 0.1.63.
- Incremented package version for this update.

## [0.1.62] - 2026-09-17

- Added `WorldPresentationFlushCoordinator` as the presentation orchestration boundary between world-change notifications and rendering.
- Coalesced direct and logical-batch change notifications through `WorldPresentationDirtySet` before rendering.
- Added deterministic single-batch flushing with empty-flush no-op behavior and explicit lifecycle disposal.
- Kept the coordinator independent from Tilemap and concrete visual channels so existing `IWorldChangeRenderer` implementations remain reusable.
- Added regression coverage for direct-change coalescing, logical batches, clean flushes, and disposal.
- Added Unity `.meta` files for the presentation coordinator runtime and tests.
- Bumped the package version to 0.1.62.
- Incremented package version for this update.

## [0.1.61] - 2026-09-17

- Fixed `WorldPresentationDirtySetTests` to use `WorldTile` values that are actually defined by the package.
- Removed invalid `WorldTile.Wall` and `WorldTile.Floor` test references that caused Unity compilation errors CS0117.
- Preserved the incremental presentation dirty-set implementation and its regression coverage.
- Bumped the package version to 0.1.61.
- Incremented package version for this update.

## [0.1.60] - 2026-09-17

- Added `WorldPresentationDirtySet` as a presentation-side change accumulator for incremental rendering.
- Coalesced repeated changes for the same `WorldPosition` while preserving the first `Before` state and latest `After` state and operation.
- Preserved deterministic first-seen position order so downstream renderers receive stable batches without sorting world coordinates.
- Added batch marking, draining, and clearing APIs so frame/update orchestration can decide when presentation work is flushed.
- Kept the dirty set independent from Tilemap, visual channels, and generated world data so it can feed terrain and multi-channel presentation sinks alike.
- Added regression coverage for coalescing, ordering, draining, batch ingestion, and null validation.
- Added Unity `.meta` files for the incremental presentation runtime and tests.
- Bumped the package version to 0.1.60.
- Incremented package version for this update.
