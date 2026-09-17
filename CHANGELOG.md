# Changelog

## [0.1.53] - 2026-09-17

- Added `IWorldCellVisualResolver` to decouple Tilemap rendering from visual selection.
- Added `WorldCellVisualCatalog` for topology-first, terrain-fallback TileBase resolution.
- Updated `WorldTilemapRenderer` to accept an injected visual resolver while preserving existing constructors.
- Added regression coverage for resolver injection, topology precedence, and terrain fallback.
- Added Unity `.meta` files for the new presentation abstraction and catalog.
- Bumped the package version to 0.1.53.
- Incremented package version for this update.

## [0.1.52] - 2026-09-17

- Fixed a missing closing parenthesis in `WorldTilemapRendererTests.UnknownWorldTileFailsFast` that caused Unity compilation error CS1026.
- Bumped the package version to 0.1.52.
- Incremented package version for this update.

## [0.1.51] - 2026-09-17

- Added optional topology-to-TileBase mappings to `WorldTilemapRenderer`.
- Added topology-aware chunk loading and world-change rendering for `Empty`, `Solid`, `Water`, `Lava`, and `Chasm` states.
- Preserved the existing three-argument renderer constructor and terrain-only fallback for compatibility.
- Updated the runtime Tilemap preview with deterministic visual tiles for generated topology states.
- Added regression coverage for topology mapping, fallback behavior, and topology-aware direct changes.
- Bumped the package version to 0.1.51.
- Incremented package version for this update.

## [0.1.50] - 2026-09-17

- Added `LiquidTopologyPass` as the first reusable liquid-topology modifier.
- Added deterministic water and lava classification from environment elevation, moisture, and temperature fields.
- Added `liquidsEnabled` plus water/lava threshold settings; liquids remain disabled by default for compatibility.
- Preserved existing non-solid topology so liquids never overwrite caves, chasms, or other topology decisions.
- Kept liquid generation data-only, leaving fluid simulation and rendering to later systems.
- Added regression coverage for disabled behavior, water/lava classification, topology preservation, and deterministic generation.
- Added Unity `.meta` files for the liquid topology runtime and tests.
- Bumped the package version to 0.1.50.
- Incremented package version for this update.

## [0.1.49] - 2026-09-17

- Added a dedicated `TopologyPipeline` so topology modifiers are composed independently from the main generation pipeline.
- Added `TopologyPass` as the single integration boundary for topology generation at order `350`.
- Moved `ChasmPass` from `IWorldGenerationPass` to `ITopologyModifier`, making chasms one topology feature instead of a pipeline-level special case.
- Exposed the topology pipeline through `WorldGenerationContext` and `ProceduralWorldGenerator` for custom topology modifiers without replacing unrelated generation stages.
- Added regression coverage for topology modifier ordering and deterministic execution order.
- Added Unity `.meta` files for the new topology pipeline and tests.
- Bumped the package version to 0.1.49.
- Incremented package version for this update.
