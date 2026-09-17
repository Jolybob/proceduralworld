# Changelog

## [0.1.110] - 2026-09-18

- Added deterministic `WorldPlanSelector` and weighted `WorldPlanCandidate` selection using world seed, stable selection salt, canonical candidate IDs, enable flags, and required tags.
- Integrated authored world-plan candidate catalogs into `ProceduralWorldDefinitionAsset` while preserving the existing single-graph fallback.
- Added regression coverage for order-independent selection, tag filtering, disabled/zero-weight candidates, and empty catalogs.
- Added deterministic world-plan selection architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.110.

## [0.1.109] - 2026-09-18

- Fixed the resumable checkpoint regression test to match the documented zero-based initial execution step (`Start` = step 0).
- Bumped the package version to 0.1.109.

## [0.1.108] - 2026-09-18

- Added `WorldPlanRuntimeBuilder` as the authoritative orchestration boundary for semantic plan expansion, compilation, world-space layout, optional feature lowering, optional realization, and chunk indexing.
- Added `WorldPlanRuntimeSettings` so feature resolution, placement feasibility, realization semantics, and deterministic layout/lowering policies remain explicit runtime dependencies.
- Added an explicit world-plan `ChunkSize` contract; lowering settings must match it, and generators reject plan runtimes configured for a different chunk size.
- Added `WorldPlanRuntime` with a world-space realization index and chunk-intersection queries without creating chunk-local planning graphs.
- Added `IWorldPlanChunkGenerator` and `WorldPlanChunkGeneration` as the optional plan-aware capability boundary for chunk generators.
- Passed the immutable world-plan runtime through `WorldGenerationContext` so custom generation passes can consume global semantic intent during chunk materialization.
- Integrated optional `WorldPlanGraphAsset` references into `ProceduralWorldDefinitionAsset`, compiling and laying out the plan once per generator instance using the world definition seed and chunk size.
- Added regression coverage for plan/layout runtime creation, feature lowering and chunk-indexed realization, chunk-size invariants, context propagation, and capability detection.
- Restored the complete world-generation checkpoint regression test suite after the previous test-file regression and retained the required `System` import.
- Added world-plan runtime integration documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.108.

## [0.1.107] - 2026-09-17

- Added `WorldGenerationExecutionCheckpoint` for resumable generation phase state across bounded execution budgets.
- Added lease-bound checkpoint storage with stale-worker protection, monotonic progress validation, and deterministic active-work ordering.
- Added checkpoint rebinding so expired work resumes from its last committed progress under a new execution attempt.
- Added `IWorldGenerationResumableWorkExecutor` and `WorldGenerationResumableExecutionRunner` for one-step-at-a-time generation execution across frames or worker budgets.
- Preserved failed checkpoints across explicit retries while removing execution checkpoints after successful completion.
- Added regression coverage for budgeted resume, lease reclaim, stale checkpoint rejection, deterministic active-work resumption, and retry state.
- Added world-generation execution checkpoint architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.107.
