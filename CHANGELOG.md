# Changelog

## [0.1.99] - 2026-09-17

- Added `WorldPlanCorridor`, `WorldPlanCorridorPlanner`, and deterministic world-space corridor realization for compiled plan connections.
- Added `IWorldPlanCorridorTraversal` and `WorldPlanCorridorContext` so terrain, water, cave, reservation, and protected-area rules can control corridor traversal and costs without coupling the planner to a specific world representation.
- Added deterministic 4-neighbour A* with bounded search, stable tie-breaking, positive traversal-cost handling, and Manhattan fallback when no traversal policy is supplied.
- Added connection/node traceability and structured diagnostics for missing anchors and unreachable corridors.
- Added regression coverage for connection-order determinism and traversal-policy detours.
- Added world-plan corridor architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.99.

## [0.1.98] - 2026-09-17

- Added `IWorldPlanFeatureResolver` and `WorldPlanFeatureLowerer` to lower semantic world-plan nodes into the existing world-space feature placement kernel.
- Added `IWorldPlanPlacementFeasibility` and `WorldPlanPlacementContext` so terrain, cave, water, reservation, and protected-area rules can reject or accept candidate footprints without coupling the core planner to a specific world representation.
- Added deterministic bounded Manhattan-ring relocation when the preferred layout anchor is not feasible.
- Added mathematical floor-division owner-chunk assignment for negative world coordinates.
- Added source-node traceability through `WorldPlanFeaturePlacement` and structured lowering diagnostics.
- Added regression coverage for lowering, deterministic feasibility relocation, and negative-coordinate owner chunks.
- Added world-plan feature-lowering architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.98.

## [0.1.97] - 2026-09-17

- Added `WorldPlanLayoutSettings`, `WorldPlanNodeLayout`, `WorldPlanLayoutPort`, and `WorldPlanLayout` as a Unity-independent world-space layout representation.
- Added `WorldPlanLayoutSolver` to deterministically arrange compiled world-plan nodes by connected-component graph distance and canonical node ID ordering.
- Added minimum-footprint and minimum-clearance aware spacing so generated plan layouts do not overlap occupied node regions.
- Added deterministic semantic port anchors with stable left/right/bottom side assignment and ordinal port ordering.
- Added regression coverage for input-order independence, clearance-aware spacing, deterministic layer advancement, and connection-driven port anchoring.
- Added world-plan layout architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.97.

## [0.1.96] - 2026-09-17

- Added reusable hierarchical world-plan subgraphs through `WorldPlanSubgraphTemplateDefinition`, `WorldPlanSubgraphPortDefinition`, and template-backed `WorldPlanNodeDefinition` instances.
- Added `WorldPlanSubgraphCompiler` to deterministically expand nested template instances into the existing flat runtime plan representation.
- Added stable scope-based node, type, and connection identities so repeated template instances remain independent while preserving deterministic input-order behaviour.
- Added exposed-port rewiring across template boundaries, including nested exposed ports, with semantic direction/type validation delegated to the existing flat compiler.
- Added template-cycle, duplicate-template, missing-template, missing-exposed-port, and missing-endpoint validation diagnostics.
- Extended `WorldPlanGraphAsset` authoring with reusable-template metadata, exposed ports, referenced template assets, and template instances.
- Extended the Unity 6 GraphView editor with reusable subgraph instance creation and exposed-port visualization, including nested template port resolution.
- Hardened graph inspector/window validation and compilation against authoring reference-cycle exceptions.
- Added regression coverage for template expansion, deterministic instance ordering, nested flattening, missing templates, missing exposed ports, and connection rewiring.
- Added world-plan subgraph architecture documentation and Unity `.meta` metadata.
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
- Added a Unity `WorldPlanGraphAsset` authoring asset that keeps editor canvas positions separate from runtime graph semantics.
- Added a Unity node graph editor with draggable nodes, custom port rendering, semantic-type compatibility filtering, connection creation/removal, graph validation, framing, and asset-backed undo/save behaviour.
- Added regression coverage for deterministic compilation, unknown node types, incompatible port types, required ports, and node properties.
- Updated the Editor assembly to reference the Authoring assembly.
- Bumped the package version to 0.1.94.
