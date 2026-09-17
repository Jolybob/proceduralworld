# Changelog

## [0.1.96] - 2026-09-17

- Added reusable hierarchical world-plan subgraphs through `WorldPlanSubgraphTemplateDefinition`, `WorldPlanSubgraphPortDefinition`, and template-backed `WorldPlanNodeDefinition` instances.
- Added `WorldPlanSubgraphCompiler` to deterministically expand nested template instances into the existing flat runtime plan representation.
- Added stable scope-based node, type, and connection identities so repeated template instances remain independent while preserving deterministic input-order behaviour.
- Added exposed-port rewiring across template boundaries, including nested exposed ports, with semantic direction/type validation delegated to the existing flat compiler.
- Added template-cycle, duplicate-template, missing-template, missing-exposed-port, and missing-endpoint validation diagnostics.
- Extended `WorldPlanGraphAsset` authoring with reusable-template metadata, exposed ports, referenced template assets, template instances, and recursive runtime-definition construction.
- Extended the Unity 6 GraphView editor with reusable subgraph instance creation and exposed-port visualization, including nested template port resolution.
- Hardened graph inspector/window validation and compilation against authoring reference-cycle exceptions.
- Added regression coverage for template expansion, deterministic instance ordering, nested flattening, missing templates, missing exposed ports, and connection rewiring.
- Added world-plan subgraph architecture documentation and Unity `.meta` metadata for the new runtime/test assets.
- Bumped the package version to 0.1.96.

## [0.1.95] - 2026-09-17

- Fixed Unity 6 `WorldPlanGraphWindow` editor compilation errors caused by using the inaccessible `UnityEngine.UIElements.Toolbar` type; the editor now uses a plain `VisualElement` toolbar container.
- Qualified GraphView manipulator registration with `this.AddManipulator(...)` so Unity's `VisualElementExtensions.AddManipulator` extension is resolved correctly.
- Updated world-plan graph ports to use Unity 6's generic `Port.Create<Edge>(...)` factory and removed the redundant custom edge connector listener.
- Preserved graph editing, typed-port compatibility filtering, connection persistence, node movement, deletion, validation, and framing behaviour.
- Bumped the package version to 0.1.95.

## [0.1.94] - 2026-09-17

- Added a typed world-plan graph runtime model with deterministic node and connection ordering.
- Added customizable node types, semantic ports, connection kinds, node properties, and footprint/clearance metadata.
- Added `WorldPlanCompiler` and `WorldPlanValidationResult` for deterministic compilation and structural validation.
- Added a Unity `WorldPlanGraphAsset` authoring asset that keeps editor canvas positions separate from runtime plan semantics.
- Added a Unity node graph editor with draggable nodes, custom port rendering, semantic-type compatibility filtering, connection creation/removal, graph validation, framing, and asset-backed undo/save behaviour.
- Added regression coverage for deterministic compilation, unknown node types, incompatible port types, required ports, and node properties.
- Updated the Editor assembly to reference the Authoring assembly.
- Bumped the package version to 0.1.94.

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

- Added Unity `.meta` files for the `docs` folder and `docs/ARCHITECTURE.md` so package documentation is imported correctly from immutable UPM package folders.
- Bumped the package version to 0.1.89.

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
- Added `RegionLayoutResolver` to adapt world-space layouts to the existing region resolver contract without breaking legacy integrations.
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
