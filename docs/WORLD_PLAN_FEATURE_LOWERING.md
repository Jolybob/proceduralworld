# World-plan feature lowering

The world-plan pipeline now has an explicit lowering boundary:

```text
semantic WorldPlan
      |
      v
WorldPlanLayoutSolver
      |
      v
WorldPlanLayout
      |
      v
WorldPlanFeatureLowerer
   |             |
   |             +--> IWorldPlanPlacementFeasibility
   |                  (terrain/cave/water/reservation truth)
   |
   +--> IWorldPlanFeatureResolver
      |
      v
WorldFeaturePlacement
      |
      v
chunk materialization / feature index / connectivity
```

## Why this boundary exists

The plan graph describes intent; it should not know about chunks or concrete terrain storage. The layout solver turns that intent into stable world coordinates. Lowering then resolves each node to an existing `IWorldFeaturePlacementDefinition` and creates the world-space placement identity used by the feature kernel.

This keeps the invariant that **world coordinates define the truth; chunks define the execution and storage boundary**.

## Terrain-aware feasibility

`IWorldPlanPlacementFeasibility` is deliberately a policy interface. A game can implement it against deterministic elevation, region, cave, water, collision, protected-area, or reservation queries without making the core planner depend on a particular terrain representation.

The feasibility implementation should be deterministic and side-effect free for a given `WorldPlanPlacementContext`. This makes lowering suitable for background generation and reproducible seeds.

## Deterministic relocation

The layout position is the preferred anchor. If feasibility rejects it, the lowerer searches Manhattan-distance rings around that anchor in a stable order. `SearchRadius` and `CandidateStep` bound the work. The first accepted candidate becomes the placement anchor.

Negative world coordinates use mathematical floor division when assigning the placement owner chunk, so an anchor at `x = -1` with a 64-cell chunk size belongs to chunk `-1`, not chunk `0`.

## Output

Each lowered result retains the source plan node ID alongside the existing `WorldFeaturePlacement`. This preserves traceability without changing the feature kernel or chunk materialization APIs.

The next layer can therefore consume the same placement set used by cross-chunk structures, world-space feature queries, and connectivity graphs.
