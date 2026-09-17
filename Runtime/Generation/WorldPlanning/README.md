# World Planning Runtime

This module contains the typed semantic world-plan graph, deterministic compiler, reusable hierarchical subgraph expansion, deterministic world-plan selection, world-space layout, runtime lowering, and chunk-facing realization boundary.

The plan layer sits between authored semantic intent and world-space feature/materialization systems.

## Deterministic plan selection

`WorldPlanSelector` selects one authored candidate without process-local random state. Candidates are canonicalized by stable ID, filtered by enabled state, positive weight, and required tags, then selected from a seed-derived stable hash:

```text
world seed + selection salt + available tags
                  |
                  v
          eligible candidates
                  |
                  v
        canonical ID ordering
                  |
                  v
          weighted stable draw
                  |
                  v
             selected plan
```

The selected graph is still compiled, expanded, laid out, lowered, and realized by the same runtime pipeline. This means semantic variation happens before world geometry is materialized while preserving reproducibility for a fixed seed, catalog, salt, and tag set.

`ProceduralWorldDefinitionAsset` exposes a candidate list, weights, enable flags, required tags, and a selection salt. The legacy single `worldPlanGraph` field remains as the fallback when no candidate catalog is configured.

## Runtime stages

```text
candidate catalog
      |
      v
WorldPlanSelector
      |
      v
WorldPlanGraphDefinition
      |
      v
WorldPlanSubgraphCompiler
      |
      v
WorldPlanLayoutSolver
      |
      +----> WorldPlanFeatureLowerer (optional)
      |             |
      |             v
      |        WorldPlanRealizer (optional)
      |             |
      |             v
      +------> WorldRealizationMap
```

Selection is a world-level decision. It is not repeated per chunk, and chunk coordinates do not participate in choosing the plan. The selected semantic plan is therefore stable before streaming or chunk materialization begins.

## Runtime types

Current runtime types include `WorldPlanGraphDefinition`, `WorldPlanCompiler`, `WorldPlanSubgraphCompiler`, `WorldPlanSelector`, `WorldPlanCandidate`, `WorldPlanSelectionSettings`, `WorldPlanRuntimeBuilder`, `WorldPlanRuntime`, `WorldPlanLayoutSolver`, `WorldPlanFeatureLowerer`, and `WorldPlanRealizer`.

## Determinism

Selection, expansion, compilation, layout, lowering, realization ordering, and chunk indexing canonicalize their inputs by stable semantic identifiers and world coordinates. Reordering candidate or graph lists does not change selection or downstream world-space results.

Canvas positions and Unity authoring objects are not part of the deterministic runtime plan or runtime layout representation.

## Chunk-generation integration

`ProceduralWorldGenerator` exposes the same immutable `WorldPlanRuntime` through `WorldGenerationContext` and the optional `IWorldPlanChunkGenerator` capability. The runtime is built once for the world definition, not recreated inside each chunk.

World coordinates remain authoritative; chunk indexing is only an acceleration path for materialization and streaming.
