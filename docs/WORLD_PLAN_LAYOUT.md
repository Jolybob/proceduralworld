# World Plan Layout

`WorldPlanLayoutSolver` is the next world-space realization boundary after semantic world-plan compilation.

The design follows the package invariant:

> **World coordinates define the truth; chunks define the execution and storage boundary.**

The plan is first flattened and validated, then laid out in world coordinates. Chunk generation is not involved.

## Pipeline

```text
WorldPlan
   |
   v
WorldPlanLayoutSolver
   |
   +-- connected components
   +-- deterministic graph-distance layers
   +-- footprint + clearance packing
   +-- semantic port anchors
   |
   v
WorldPlanLayout
   |
   +-- node world rectangles
   +-- port world positions
   +-- port facing sides
   |
   +----> feature placement lowering
   +----> corridor/path planning
   +----> chunk-local materialization
```

## Determinism

The solver does not depend on serialized list order, Unity editor coordinates, chunk load order, or mutable random state.

Nodes and connections are processed in canonical ordinal ID order. Connected components use the lowest node ID as their root. Breadth-first graph distance creates layers, and nodes inside each layer are sorted by stable node ID.

The current layout is deliberately non-random. `WorldPlan.Seed` remains available for future deterministic layout strategies that need seed-based variation without changing the layout API.

## Footprints and clearance

Every `WorldPlanNodeTypeDefinition` already exposes:

- `MinimumWidth`;
- `MinimumHeight`;
- `MinimumClearance`.

The solver treats clearance as occupied space around the node while packing the layout. Node rectangles are therefore guaranteed not to overlap when the returned result contains no `LayoutOverlap` errors.

This creates a deterministic constraint boundary for future geometry realization. A dungeon room can therefore reserve walls or construction margins before corridors are planned.

## Connected components

Each connected component is laid out independently.

Within a component, nodes are arranged into breadth-first layers from the lowest stable node ID:

```text
layer 0       A
              |
layer 1     B   C
             \ /
layer 2       D
```

The current solver centers each layer against the component width and then packs disconnected components horizontally with a deterministic component gap.

This is intentionally simple and predictable. More sophisticated constraint solving can be added later without changing the `WorldPlanLayout` data contract.

## Semantic port anchors

Port anchors are world positions on a node's perimeter.

Connected source ports are assigned to the right side and connected target ports to the left side. Unconnected input/output ports follow their semantic direction, while unconnected bidirectional ports are placed on the bottom side.

Ports on the same side are ordered by stable port ID and distributed along the corresponding node axis.

The result is a stable world-space answer for:

```text
source port -> corridor planner
                 |
                 v
            target port
```

The path planner can therefore work from world-space anchors without understanding node authoring data or chunk boundaries.

## Relationship to Core Keeper-style generation

The package is intentionally moving toward the same useful separation described for Core Keeper's generated dungeons: larger structures are represented as multiple connected areas and can cross chunk boundaries rather than being treated as isolated chunk-local decorations.

That distinction matters architecturally:

```text
semantic dungeon plan
        |
        v
world-space room layout
        |
        v
world-space corridor/path plan
        |
        v
chunk intersections
        |
        v
local materialization
```

The chunk remains an execution/storage boundary rather than becoming the source of dungeon topology.

## Current 0.1.97 scope

Implemented:

- immutable world-plan layout primitives;
- deterministic connected-component and layer layout;
- minimum footprint and clearance-aware packing;
- deterministic world-space port anchors;
- layout regression coverage.

Not yet implemented:

- terrain-aware constraint solving;
- placement feasibility scoring;
- semantic port alignment optimization;
- plan-to-feature placement lowering;
- world-space corridor/path generation;
- layout preview of resolved world geometry;
- backtracking when a future geometry constraint cannot be satisfied.

Those stages can now consume a stable `WorldPlanLayout` without introducing a chunk-local planning layer.
