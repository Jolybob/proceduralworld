# Changelog

## [0.1.5] - 2026-09-17

- Added a reusable ordered generation-pass pipeline.
- Added a pluggable deterministic `INoiseField` abstraction and seeded multi-octave Perlin implementation.
- Moved the prototype radial biome logic into `RadialBiomePass`.
- Added configurable noise octaves, persistence, and lacunarity to world generation settings.
- Kept the existing generator API compatible while allowing custom pipelines.
- Incremented package version for this update.

## [0.1.4] - 2026-09-17

- Removed temporary Tilemap fix marker files that were being imported as package assets without Unity metadata.
- Incremented package version for this update.

## [0.1.3] - 2026-09-17

- Fixed the `Tilemap` namespace/type collision in `ProceduralWorldTilemap` by explicitly aliasing Unity's Tilemap type.
- Added missing Unity metadata for package documentation assets.
- Incremented package version for this update.

## [0.1.2] - 2026-09-17

- Added Unity metadata for package documentation assets.
- Fixed the Tilemap namespace/type collision in the Tilemap adapter.
- Package version incremented for this update.

## [0.1.1]

- Added Unity package metadata and test assembly fixes.
