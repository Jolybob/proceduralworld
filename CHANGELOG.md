# Changelog

## [0.1.111] - 2026-09-18

- Added `WorldSceneDefinition`, `WorldSceneCatalog`, and deterministic `WorldSceneSelector` for reusable scenes, dungeon pieces, and landmarks.
- Added weighted biome/tag filtering, unique-scene consumption, world-instance limits, minimum origin distance, and orientation metadata to scene definitions.
- Added Unity-authored `WorldSceneDefinitionAsset` and `WorldSceneCatalogAsset` and exposed the catalog from `ProceduralWorldDefinitionAsset`.
- Added regression coverage for canonical ordering, deterministic selection, tag filtering, uniqueness, world-instance limits, and empty pools.
- Documented scene/dungeon catalog architecture and preserved the generic world-space feature placement boundary.
- Bumped the package version to 0.1.111.

## [0.1.110] - 2026-09-18

- Added deterministic `WorldPlanSelector` and weighted `WorldPlanCandidate` selection using world seed, stable selection salt, canonical candidate IDs, enable flags, and required tags.
- Integrated authored world-plan candidate catalogs into `ProceduralWorldDefinitionAsset` while preserving the existing single-graph fallback.
- Added regression coverage for order-independent selection, tag filtering, disabled/zero-weight candidates, and empty catalogs.
- Added deterministic world-plan selection architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.110.

## [0.1.109] - 2026-09-18

- Fixed the resumable checkpoint regression test to match the documented zero-based initial execution step (`Start` = step 0).
- Bumped the package version to 0.1.109.
