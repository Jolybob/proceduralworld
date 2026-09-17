# World-plan corridors

The corridor stage realizes compiled world-plan connections as deterministic world-space paths.

```text
WorldPlan
  -> WorldPlanLayout
  -> WorldPlanCorridorPlanner
       -> IWorldPlanCorridorTraversal
       -> WorldPlanCorridor
  -> world geometry / chunk materialization
```

## Design

- Connections are processed in canonical source/target/ID order.
- Without a traversal policy, the planner emits a deterministic Manhattan path between semantic port anchors.
- A traversal policy can reject terrain, water, caves, reserved cells, or protected regions and can assign positive traversal costs.
- With a policy, deterministic 4-neighbour A* searches a bounded world-space rectangle.
- Tie-breaking is based on total estimate, accumulated cost, then world X/Y, making equal-cost solutions stable across runs.
- Search limits prevent pathological plans from expanding without bound.
- Corridor identity retains the originating connection and both plan node IDs.

The planner is Unity-independent and does not require chunks to be loaded. A production terrain adapter can therefore query deterministic world truth directly, while chunk streaming remains a materialization concern.
