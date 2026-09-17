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
- Bumped package version to 0.1.62.
- Incremented package version for this update.

## [0.1.61] - 2026-09-17

- Fixed `WorldPresentationDirtySetTests` to use `WorldTile` values that are actually defined by the package.
- Removed invalid `WorldTile.Wall` and `WorldTile.Floor` test references that caused Unity compilation errors CS0117.
- Preserved the incremental presentation dirty-set implementation and its regression coverage.
- Bumped package version to 0.1.61.
- Incremented package version for this update.

## [0.1.60] - 2026-09-17

- Added `WorldPresentationDirtySet` as a presentation-side change accumulator for incremental rendering.
- Coalesced repeated changes for the same `WorldPosition` while preserving the first `Before` state and latest `After` state and operation.
- Preserved deterministic first-seen position order so downstream renderers receive stable batches without sorting world coordinates.
- Added batch marking, draining, and clearing APIs so frame/update orchestration can decide when presentation work is flushed.
- Kept the dirty set independent from Tilemap, visual channels, and generated world data so it can feed terrain and multi-channel presentation sinks alike.
- Added regression coverage for coalescing, ordering, draining, batch ingestion, and null validation.
- Added Unity `.meta` files for the incremental presentation runtime and tests.
- Bumped package version to 0.1.60.
- Incremented package version for this update.

## [0.1.59] - 2026-09-17

- Added `WorldCellVisualOverlayChannel` for simultaneous supplementary visuals on independent channels.
- Added channel-aware overlay layer and catalog abstractions with deterministic ordering and unresolved-channel fallthrough.
- Added `WorldTilemapOverlayChannelRenderer` for dedicated multi-channel Tilemap presentation.
- Added regression coverage and Unity `.meta` files.
- Bumped package version to 0.1.59.
- Incremented package version for this update.

## [0.1.58] - 2026-09-17

- Added presentation-only `WorldCellVisualState` flags for selection, hover, damage, and interaction.
- Added external visual-state storage and position-aware overlay resolution.
- Added state-driven Tilemap overlay presentation and regression coverage.
- Bumped package version to 0.1.58.
- Incremented package version for this update.

## [0.1.57] - 2026-09-17

- Added composable supplementary overlay presentation independent from base terrain rendering.
- Added deterministic overlay catalogs and an independent Tilemap overlay renderer.
- Added regression coverage and Unity `.meta` files.
- Bumped package version to 0.1.57.
- Incremented package version for this update.

## [0.1.56] - 2026-09-17

- Added resource and structure visual layers with deterministic topology/resource/structure/terrain ordering.
- Added regression coverage and Unity `.meta` files.
- Bumped package version to 0.1.56.
- Incremented package version for this update.

## [0.1.55] - 2026-09-17

- Fixed Unity test compilation error CS0104 by explicitly qualifying `UnityEngine.Object`.
- Bumped package version to 0.1.55.
- Incremented package version for this update.

## [0.1.54] - 2026-09-17

- Added independently composable visual layers and deterministic order-based visual-layer catalogs.
- Added topology visual-layer extraction and regression coverage.
- Bumped package version to 0.1.54.
- Incremented package version for this update.

## [0.1.53] - 2026-09-17

- Added `IWorldCellVisualResolver` and topology-first, terrain-fallback visual catalogs.
- Updated Tilemap rendering for resolver injection while preserving existing constructors.
- Added regression coverage and Unity `.meta` files.
- Bumped package version to 0.1.53.
- Incremented package version for this update.

## [0.1.52] - 2026-09-17

- Fixed a missing closing parenthesis causing Unity compilation error CS1026.
- Bumped package version to 0.1.52.
- Incremented package version for this update.

## [0.1.51] - 2026-09-17

- Added topology-aware Tilemap rendering for empty, solid, water, lava, and chasm states while preserving terrain fallback.
- Added regression coverage.
- Bumped package version to 0.1.51.
- Incremented package version for this update.

## [0.1.50] - 2026-09-17

- Added reusable deterministic liquid topology generation for water and lava.
- Preserved existing non-solid topology and kept fluid simulation separate from generation.
- Added regression coverage and Unity `.meta` files.
- Bumped package version to 0.1.50.
- Incremented package version for this update.

## [0.1.49] - 2026-09-17

- Added a dedicated `TopologyPipeline` and topology modifier abstraction.
- Integrated topology generation through `TopologyPass` and moved chasms into the topology stage.
- Added deterministic ordering regression coverage and Unity `.meta` files.
- Bumped package version to 0.1.49.
- Incremented package version for this update.
