# Changelog

## [0.1.48] - 2026-09-17

- Added `WorldGenerationProfile` as a Unity-authored ScriptableObject entry point for reusable procedural worlds.
- Added serialized authoring entries for world settings, macro regions, regions, terrain, resources, and structures.
- Added `CreateGenerator(seed)` to build immutable runtime catalogs and a deterministic generation pipeline from an authoring profile.
- Added `TryValidate(out error)` for catalog and macro-region configuration validation before chunk generation.
- Added explicit macro fallback-region configuration and seed-rotation controls.
- Added world-authoring documentation and EditMode regression coverage.
- Bumped the package version to 0.1.48.

## [0.1.47] - 2026-09-17

- Added canonical `CellTopology` state to generated cells with Solid, Empty, Water, Lava, and Chasm values.
- Added `GeneratedCellFlags.Chasm` and topology mutation helpers.
- Updated cave generation to write canonical Empty topology instead of relying on the presentation tile as hidden state.
- Added deterministic `IVoronoiEdgeField` and `VoronoiEdgeField` implementations for world-space Voronoi boundaries that remain continuous across chunk edges.
- Added `ChasmPass` as an ordered generation pass with configurable width, feature-cell size, minimum distance from origin, and seed offset.
- Added topology settings to `WorldGenerationSettings` and inserted chasm generation after caves and before resources/structures.
- Included topology in persistence equality and cell hashing so topology edits are saved and restored correctly.
- Added topology extension contracts for future water, lava, and other data-only modifiers.
- Bumped the package version to 0.1.47.

## [0.1.46] - 2026-09-17

- Added configurable macro regions composed of radial bands and angular sectors.
- Added deterministic radial and angular boundary distortion and optional seed rotation.
- Added `IPositionAwareRegionResolver` while preserving the existing `IRegionResolver` contract.
- Added macro-region regression coverage and architecture documentation.
- Bumped the package version to 0.1.46.

## [0.1.45] - 2026-09-17

- Added explicit `ChunkStreamingState` lifecycle states for scheduled streaming: `Inactive`, `Pending`, and `Loaded`.
- Added `IChunkGenerationScheduler.Contains` so schedulers can expose whether a coordinate is currently waiting for generation without consuming the request.
- Added `GetState(ChunkCoord)` to scheduled and scheduled-persistent controllers for deterministic lifecycle inspection by gameplay, UI, and orchestration code.
- Scheduled controllers now track loaded coordinates explicitly, keeping pending work and completed activation separate from the planner's active set.
- Added regression coverage for pending, loaded, and inactive transitions plus scheduler membership inspection.
- Preserved the existing generation, persistence, sink, and scheduling behavior and constructor APIs.
- Changed the preview seed from `258947` to `281604`.
- Bumped the package version to 0.1.45.
