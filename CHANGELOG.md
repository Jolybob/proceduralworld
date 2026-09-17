# Changelog

## [0.1.80] - 2026-09-17

- Fixed the runtime Tilemap visual resolver so the default `CellTopology.Solid` state does not override ordinary terrain visuals.
- Preserved explicit water, lava, and chasm topology overrides while allowing terrain catalogs to render normal solid cells.
- Kept the existing runtime Tilemap material, renderer, and camera visibility fixes from 0.1.79.
- Bumped the package version to 0.1.80.
- Incremented package version for this update.

## [0.1.79] - 2026-09-17

- Fixed runtime Tilemap preview rendering by assigning an explicit unlit sprite material when a compatible shader is available.
- Normalized TilemapRenderer sorting layer and order so generated cells are not hidden by unexpected renderer settings.
- Hardened the 2D preview camera by resetting its rotation, enforcing a valid negative Z position, and using orthographic framing for the generated world.
- Preserved deterministic runtime tile generation and existing topology/terrain fallback behavior.
- Bumped the package version to 0.1.79.
- Incremented package version for this update.

## [0.1.78] - 2026-09-17

- Fixed the runtime Tilemap preview so `CellTopology.Empty` falls through to the generated terrain visual instead of replacing it with a fully transparent tile.
- Preserved explicit topology visuals for solid, water, lava, and chasm cells.
- Kept the TilemapRenderer and camera visibility fixes from 0.1.77.
- Bumped the package version to 0.1.78.
- Incremented package version for this update.
