# Changelog

## [0.1.88] - 2026-09-17

- Restored the six-argument `ProceduralWorldGenerator` constructor shape used by the 0.1.87 structure-placement regression test.
- Preserved the existing advanced constructor surface while keeping the compatibility overload delegated to the full implementation.
- Bumped the package version to 0.1.88.

## [0.1.87] - 2026-09-17

- Added `StructurePlacement` as an immutable world-space structure identity with an explicit owner chunk and intersectable footprint.
- Added `StructurePlacementSet` for deterministic unique placement collection.
- Added `IStructurePlacementSource` so structure planning is decoupled from chunk materialization.
- Added `StructurePlacementPlanner` for deterministic owner-chunk anchor generation and reusable placement planning.
- Added `DeterministicStructurePlacementSource` to discover placements from neighboring owner chunks whose footprints intersect the requested chunk.
- Reworked structure materialization into `StructurePlacementPass`, which stamps only the local intersection of each world-space structure footprint.
- Preserved the existing `StructurePass` API as a compatibility wrapper over the new placement architecture.
- Updated the default generation pipeline to use `StructurePlacementPass`.
- Added regression coverage for cross-chunk footprints, negative chunk coordinates, and chunk-local materialization.
- Bumped the package version to 0.1.87.

## [0.1.86] - 2026-09-17

- Added configurable resource deposit sizes with `DepositSizeMin` and `DepositSizeMax`.
- Added deterministic `DepositGrowthChance` so resources can form compact ore/crystal deposits instead of only isolated cells.
- Updated the default Crystal, Ore, and Rare Ore definitions to generate clustered deposits while preserving deterministic generation.
- Added regression coverage for single-cell backwards-compatible defaults and deterministic five-cell deposits.
- Bumped the package version to 0.1.86.

## [0.1.85] - 2026-09-17

- Added `IRegionLayout` as a generic world-space geography boundary separate from region content and generation passes.
- Added `RegionLayoutResolver` to adapt world-space layouts to the existing resolver contract without breaking legacy integrations.
- Extended `RadialSectorRegionResolver` to implement `IRegionLayout` while preserving its existing API and deterministic behaviour.
- Added the optional `Jolybob.ProceduralWorld.Authoring` assembly with `ProceduralWorldDefinitionAsset` for Unity-authored world settings, region profiles, terrain profiles, and radial macro-region layouts.
- Allowed `ProceduralWorldGenerator` to accept a custom region resolver while retaining the default pipeline and existing constructor overloads.
- Added regression coverage for layout delegation and custom-resolver generator construction.
- Bumped the package version to 0.1.85.

## [0.1.84] - 2026-09-17

- Fixed the demand snapshot stability regression test to retain each published snapshot independently across later reconciliations.
- Added explicit assertions for both the first and subsequent demand-change snapshots.
- Preserved the runtime snapshot ownership fix introduced in 0.1.83.
- Bumped the package version to 0.1.84.
- Incremented package version for this update.

## [0.1.83] - 2026-09-17

- Fixed `WorldPresentationRegionDemandChange` snapshots to own an immutable copy of the demanded-region sequence.
- Prevented disposal cleanup from publishing a final demand-change notification.
- Preserved deterministic load/unload ordering and normal demand notifications.
- Bumped the package version to 0.1.83.
- Incremented package version for this update.

## [0.1.82] - 2026-09-17

- Added `WorldPresentationRegionDemandChange` as an immutable notification payload for reconciled region demand state.
- Added `DemandChanged` notifications and a deterministic read-only `DemandedRegions` snapshot to `WorldPresentationRegionDemandCoordinator`.
- Preserved separation between demand aggregation, residency execution, and downstream observers.
- Added regression coverage for deterministic snapshots, snapshot stability, and post-disposal notification safety.
- Added a Unity `.meta` file for the new runtime asset.
- Bumped the package version to 0.1.82.
- Incremented package version for this update.
