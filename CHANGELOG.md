# Changelog

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

## [0.1.59] - 2026-09-17

- Added `WorldCellVisualOverlayChannel` so supplementary visuals can render simultaneously on independent channels.
- Added `IWorldCellVisualOverlayChannelLayer` for channel-aware overlay implementations while preserving the existing layer contract.
- Added `WorldCellVisualOverlayChannelCatalog` for deterministic per-channel composition and explicit unresolved-channel omission.
- Added `WorldTilemapOverlayChannelRenderer` to render multiple overlay channels through dedicated Tilemaps without allowing one overlay to overwrite another.
- Updated `WorldCellVisualStateOverlayLayer` with an optional channel while preserving its existing constructor behavior.
- Added regression coverage for simultaneous channels, same-channel order precedence, and unresolved-channel fallthrough.
- Added Unity `.meta` files for the multi-channel overlay runtime and tests.
- Bumped the package version to 0.1.59.
- Incremented package version for this update.

## [0.1.58] - 2026-09-17

- Added `WorldCellVisualState` as a presentation-only flag set for transient cell states such as selection, hover, damage, and interaction.
- Added `IWorldCellVisualStateProvider` and `WorldCellVisualStateStore` so transient visual state remains external to generated world data.
- Added position-aware overlay resolution through `IWorldCellVisualOverlayContextLayer` while preserving the legacy overlay-layer contract.
- Added `WorldCellVisualStateOverlayLayer` for state-driven Tilemap overlays without modifying `GeneratedCell`.
- Updated `WorldTilemapOverlayRenderer` to pass world-position context during chunk, direct-change, and batch rendering.
- Added regression coverage for state defaults, clearing, state-driven resolution, fallthrough, and contextual catalog compatibility.
- Added Unity `.meta` files for the new visual-state runtime and tests.
- Bumped the package version to 0.1.58.
- Incremented package version for this update.

## [0.1.57] - 2026-09-17

- Added `IWorldCellVisualOverlayLayer` for supplementary presentation such as overlays, decals, and cell states without changing `GeneratedCell`.
- Added `WorldCellVisualOverlayCatalog` for deterministic order-based overlay composition with explicit fallthrough.
- Added `WorldTilemapOverlayRenderer` as an independent Tilemap sink/renderer for supplementary visuals, keeping the base terrain renderer isolated.
- Preserved chunk loading, unloading, direct changes, and batch change rendering semantics for overlay presentation.
- Added regression coverage for overlay ordering, fallthrough, empty resolution, duplicate orders, and invalid negative orders.
- Added Unity `.meta` files for the new overlay runtime and tests.
- Bumped the package version to 0.1.57.
- Incremented package version for this update.

## [0.1.56] - 2026-09-17

- Added `ResourceWorldCellVisualLayer` for resource-specific presentation with flag-gated fallthrough.
- Added `StructureWorldCellVisualLayer` for structure-specific presentation with flag-gated fallthrough.
- Updated the Tilemap preview to compose topology, resource, and structure layers over terrain fallback.
- Added deterministic default layer ordering: topology, resource, structure, then terrain fallback.
- Added regression coverage for resource and structure resolution, unmapped fallthrough, and layer ordering.
- Added Unity `.meta` files for the new visual-layer runtime and tests.
- Bumped the package version to 0.1.56.
- Incremented package version for this update.

## [0.1.55] - 2026-09-17

- Fixed Unity test compilation error CS0104 by explicitly qualifying `UnityEngine.Object` in `WorldCellVisualLayerCatalogTests`.
- Bumped the package version to 0.1.55.
- Incremented package version for this update.

## [0.1.54] - 2026-09-17

- Added `IWorldCellVisualLayer` for independently composable visual overrides.
- Added `WorldCellVisualLayerCatalog` for deterministic order-based layer composition with a required fallback resolver.
- Added `TopologyWorldCellVisualLayer` so topology presentation is a reusable layer instead of renderer-specific logic.
- Updated the Tilemap preview to compose topology over terrain through the visual-layer architecture.
- Added regression coverage for layer ordering, fallthrough, and fallback resolution.
- Added Unity `.meta` files for the new visual-layer runtime and catalog.
- Bumped package version to 0.1.54.
- Incremented package version for this update.

## [0.1.53] - 2026-09-17

- Added `IWorldCellVisualResolver` to decouple Tilemap rendering from visual selection.
- Added `WorldCellVisualCatalog` for topology-first, terrain-fallback TileBase resolution.
- Updated `WorldTilemapRenderer` to accept an injected visual resolver while preserving existing constructors.
- Added regression coverage for resolver injection, topology precedence, and terrain fallback.
- Added Unity `.meta` files for the new presentation abstraction and catalog.
- Bumped package version to 0.1.53.
- Incremented package version for this update.

## [0.1.52] - 2026-09-17

- Fixed a missing closing parenthesis in `WorldTilemapRendererTests.UnknownWorldTileFailsFast` that caused Unity compilation error CS1026.
- Bumped package version to 0.1.52.
- Incremented package version for this update.

## [0.1.51] - 2026-09-17

- Added optional topology-to-TileBase mappings to `WorldTilemapRenderer`.
- Added topology-aware chunk loading and world-change rendering for `Empty`, `Solid`, `Water`, `Lava`, and `Chasm` states.
- Preserved the existing three-argument renderer constructor and terrain-only fallback for compatibility.
- Updated the runtime Tilemap preview with deterministic visual tiles for generated topology states.
- Added regression coverage for topology mapping, fallback behavior, and topology-aware direct changes.
- Bumped package version to 0.1.51.
- Incremented package version for this update.

## [0.1.50] - 2026-09-17

- Added `LiquidTopologyPass` as the first reusable liquid-topology modifier.
- Added deterministic water and lava classification from environment elevation, moisture, and temperature fields.
- Added `liquidsEnabled` plus water/lava threshold settings; liquids remain disabled by default for compatibility.
- Preserved existing non-solid topology so liquids never overwrite caves, chasms, or other topology decisions.
- Kept liquid generation data-only, leaving fluid simulation and rendering to later systems.
- Added regression coverage for disabled behavior, water/lava classification, topology preservation, and deterministic generation.
- Added Unity `.meta` files for the liquid topology runtime and tests.
- Bumped package version to 0.1.50.
- Incremented package version for this update.

## [0.1.49] - 2026-09-17

- Added a dedicated `TopologyPipeline` so topology modifiers are composed independently from the main generation pipeline.
- Added `TopologyPass` as the single integration boundary for topology generation at order `350`.
- Moved `ChasmPass` from `IWorldGenerationPass` to `ITopologyModifier`, making chasms one topology feature instead of a pipeline-level special case.
- Exposed the topology pipeline through `WorldGenerationContext` and `ProceduralWorldGenerator` for custom topology modifiers without replacing unrelated generation stages.
- Added regression coverage for topology modifier ordering and deterministic execution order.
- Added Unity `.meta` files for the new topology pipeline and tests.
- Bumped package version to 0.1.49.
- Incremented package version for this update.
