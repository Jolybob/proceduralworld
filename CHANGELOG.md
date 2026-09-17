# Changelog

## [0.1.93] - 2026-09-17

- Fixed `Builder_IsDeterministicRegardlessOfInputOrder` so node and edge collections are compared independently, avoiding an out-of-range edge access when a graph has fewer edges than nodes.
- Bumped the package version to 0.1.93.

## [0.1.92] - 2026-09-17

- Added `WorldConnectivityNode`, `WorldConnectivityEdge`, and `WorldConnectivityGraph` as world-space graph primitives independent from chunk residency and rendering.
- Added `WorldConnectivitySettings` and `WorldConnectivityGraphBuilder` for deterministic sparse connectivity generation over feature placements.
- Added uniform-grid candidate discovery, deterministic forest construction, redundant short-link generation, connected-component queries, and world-rectangle integration through `WorldFeaturePlacementIndex`.
- Hardened graph spatial bucketing and distance arithmetic for negative and extreme world coordinates.
- Added regression coverage for deterministic input-order independence, connected backbones, degree caps, negative coordinates, and feature-index integration.
- Added world connectivity architecture documentation and Unity `.meta` files.
- Bumped the package version to 0.1.92.

## [0.1.91] - 2026-09-17

- Added `WorldFeaturePlacementQueryContext` and `IWorldFeaturePlacementQuerySource` for deterministic feature discovery without constructing a `GeneratedChunk`.
- Added `WorldFeaturePlacementIndex` with cached chunk queries, world-space point containment queries, rectangle intersection queries, de-duplication, and explicit invalidation.
- Updated `DeterministicWorldFeaturePlacementSource` to support both generation-context and lightweight query-context discovery.
- Added regression coverage for query caching, negative-coordinate floor division, cross-chunk de-duplication, and cache invalidation.
- Updated feature and package documentation to expose feature querying as a world-level gameplay/runtime boundary.
- Bumped the package version to 0.1.91.

## [0.1.90] - 2026-09-17

- Added a reusable world-space feature placement kernel through `IWorldFeaturePlacementDefinition`, `WorldFeaturePlacement`, `WorldFeaturePlacementSet`, `WorldFeaturePlacementPlanner`, and `IWorldFeaturePlacementSource`.
- Adapted structure definitions and deterministic structure planning/source discovery to use the generic feature placement architecture without changing the existing structure-facing APIs.
- Added regression coverage for generic feature placement determinism, cross-chunk footprints, negative coordinates, and placement de-duplication.
- Added Unity `.meta` files for the new feature-placement runtime and test assets.
- Bumped the package version to 0.1.90.

## [0.1.89] - 2026-09-17
