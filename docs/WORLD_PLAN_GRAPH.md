# World Plan Graph

`WorldPlanGraph` is the semantic planning layer of `com.jolybob.proceduralworld`.

It is designed to make procedural world structure customizable without coupling generation to Unity editor objects, Tilemaps, or scene hierarchies.

## Purpose

The graph expresses:

- what semantic things a generated world should contain;
- which node types are available;
- which ports a node exposes;
- which ports may connect;
- which relationships are required or optional;
- which custom properties a node accepts;
- basic footprint and clearance metadata for future layout solving.

It is not itself the world. It is an authoring/planning representation that compiles into a deterministic runtime `WorldPlan`.

## Architecture

```text
CUSTOM NODE TYPE SCHEMAS
          |
          v
     GRAPH ASSET
   /      |       \
 nodes  connections  canvas positions
   |       |              |
   +-------+--------------+
           |
           v
 WORLD PLAN COMPILER
           |
    validation + ordering
           |
           v
      RUNTIME PLAN
           |
    +------+------+------+
    |             |      |
    v             v      v
 placement      paths   queries/graph
```

The canvas is intentionally outside the deterministic runtime representation.

## Stable identities

The following identities are semantic and may become generation inputs:

- node type ID;
- node ID;
- port ID within a node type;
- connection ID.

Editor canvas coordinates are not semantic.

```text
Move node on canvas
       |
       X
       |
   no generation change
```

Renaming or replacing a stable ID is therefore a semantic change and should be treated as an intentional authoring migration rather than a visual edit.

## Node types

A `WorldPlanNodeTypeDefinition` describes a reusable node schema.

It contains:

- stable ID;
- display name/category;
- minimum width and height;
- minimum clearance;
- typed ports;
- optional property-key schema.

This enables custom world domains without hardcoding gameplay-specific node classes into the runtime.

Example schemas:

```text
Settlement
  output: district
  output: gate

District
  input: district
  output: building

Building
  input: building
  output: room

DungeonEntrance
  input: outside
  output: corridor
```

The runtime only knows that these are node types with ports and constraints. A specific game gives them meaning.

## Ports

`WorldPlanPortDefinition` supports:

- `Input`;
- `Output`;
- `Bidirectional`.

Ports also have a semantic type, such as:

```text
road
corridor
door
gate
river
entrance
settlement-link
```

Compatible connections require compatible directions and, when both sides specify a semantic type, matching semantic types.

Each port can also be required and can allow either one connection or multiple connections.

## Connections

`WorldPlanConnectionDefinition` identifies a source node/port and target node/port.

Connections have three meanings:

```text
Required
--------
Must resolve and pass validation.

Optional
--------
May exist in the selected plan.

Derived
-------
May be inferred later from spatial or graph analysis instead of being authored as semantic intent.
```

This preserves the distinction between a designed relationship and a relationship discovered after placement.

## Properties

Node instances may carry arbitrary key/value properties constrained by the node type's declared property keys.

The current runtime represents property values as strings so the planning core stays independent of Unity's serialization/editor types.

Future authoring tooling may expose richer typed property editors while compiling them into deterministic runtime values.

## Validation

`WorldPlanCompiler.Validate` is intentionally separate from layout solving.

It currently checks:

- null node types/nodes/connections/ports/properties;
- duplicate node type IDs;
- duplicate port IDs within a node type;
- duplicate node IDs;
- unknown node types;
- duplicate connection IDs;
- missing source/target nodes;
- missing source/target ports;
- invalid input/output direction combinations;
- incompatible semantic port types;
- duplicate endpoint relationships;
- required unconnected ports;
- connection multiplicity violations;
- undeclared properties as warnings.

This establishes the semantic correctness boundary before world-space layout is attempted.

## Deterministic compilation

`WorldPlanCompiler.Compile(seed, definition)` canonicalizes the input before creating runtime objects.

The deterministic ordering is:

```text
node types
   -> ordinal type ID

nodes
   -> ordinal node ID

connections
   -> source node ID
   -> source port ID
   -> target node ID
   -> target port ID
   -> connection kind
   -> connection ID
```

The compiler therefore does not depend on serialized list order.

The seed is retained on the runtime plan for later deterministic expansion/layout stages. The 0.1.94 compiler itself does not use the seed to randomly mutate the graph.

## Editor graph

`WorldPlanGraphAsset` is the Unity serialization boundary.

Create one with:

**Assets > Create > Procedural World > World Plan Graph**

Then define custom node types and open the graph using:

**Window > Procedural World > World Plan Graph**

The current graph editor provides:

- grid-based canvas navigation;
- node creation from registered node types;
- node movement;
- semantic port visualization;
- compatible-port filtering;
- connection creation/removal;
- node deletion;
- validation;
- framing;
- Unity Undo/asset persistence.

The graph view stores only the visual `position` metadata back to the asset when nodes move. That value is excluded by `BuildDefinition()`.

## Authoring vs runtime

The intended boundary is:

```text
Unity Editor
     |
     v
WorldPlanGraphAsset
     |
     v
WorldPlanGraphDefinition
     |
     v
WorldPlanCompiler
     |
     v
WorldPlan
```

No `UnityEditor` or `GraphView` type enters the runtime world-plan classes.

This makes the compiled plan usable for:

- runtime generation;
- headless generation;
- automated validation;
- deterministic preview;
- future background generation;
- non-Unity tooling.

## Future hierarchical plans

The current 0.1.94 model represents a flat node/connection graph. The next planned layer is reusable hierarchical expansion:

```text
Template: Settlement
    |
    +-- TownCenter
    +-- District*
            |
            +-- Building*
                    |
                    +-- Room*
```

A future deterministic expander should:

1. select a concrete template/subgraph from authored rules;
2. derive child identities from parent identity + stable template identity;
3. preserve semantic connection IDs through expansion;
4. enforce recursion limits;
5. compile the expanded result into the same flat runtime `WorldPlan` representation.

This allows one graph schema to describe worlds, regions, towns, dungeons, buildings, rooms, and smaller reusable structures.

## Future layout

After plan compilation, a future `WorldPlanLayoutSolver` should convert semantic structure into world-space placement:

```text
WorldPlan
   |
   +-- footprint constraints
   +-- clearance constraints
   +-- port alignment
   +-- region/terrain eligibility
   |
   v
initial deterministic placement
   |
   v
overlap / separation solving
   |
   v
world-space feature placements
```

The solver must run in world coordinates and must not be partitioned by chunk.

## Future path/corridor planning

Semantic connections eventually lower to world-space paths:

```text
PlanConnection
      |
      v
source port world position
      |
      v
world traversability query
      |
      v
deterministic corridor/path
      |
      v
chunk-local materialization
```

This is the intended place to incorporate the useful path-generation concept from grammar/graph-based procedural generators while retaining the world-space and chunk-boundary guarantees of this package.

## Relationship to connectivity

The existing `WorldConnectivityGraph` remains a derived graph over world-space feature placements.

The intended relationship is:

```text
semantic plan graph
        |
        | expresses intent
        v
world-space placement
        |
        +------> derived connectivity graph
        |
        +------> corridor/path planner
```

A spatial edge does not automatically imply that a semantic required connection has been fulfilled. Required plan connections need to be checked against actual placement/path feasibility.

## Current 0.1.94 scope

Implemented:

- typed runtime world-plan graph model;
- customizable node type schema;
- semantic ports;
- required/optional/derived connection kinds;
- node properties;
- deterministic compilation;
- semantic validation;
- Unity graph asset;
- Unity GraphView editor.

Not yet implemented:

- hierarchical template/subgraph expansion;
- deterministic plan selection from weighted alternatives;
- world-space layout solving;
- plan-to-feature placement lowering;
- semantic port world positions;
- world-space corridor/path generation;
- direct `ProceduralWorldGenerator` integration;
- dedicated graph-side property/connection inspectors;
- graph preview of resolved world geometry.
