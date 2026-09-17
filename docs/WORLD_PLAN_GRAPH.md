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
- basic footprint and clearance metadata for world-space layout.

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
 HIERARCHICAL EXPANSION
           |
   exposed-port rewiring
   scoped identities
   nested templates
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
- connection ID;
- template ID;
- exposed template-port ID.

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

## Hierarchical templates

`WorldPlanSubgraphTemplateDefinition` packages a normal graph as a reusable semantic building block. `WorldPlanSubgraphPortDefinition` defines the explicit ports that may cross the template boundary.

A graph node becomes a template instance when its `TemplateId` is set:

```text
Dungeon
  |
  +-- Entrance ---> Wing01 ---> Boss
                     |
                     +-- template: DungeonWing
```

The template instance is not present in the runtime result. It expands to scoped concrete nodes:

```text
Wing01/Entrance
Wing01/RoomA
Wing01/RoomB
Wing01/BossGate
```

Two instances of the same template therefore cannot collide:

```text
Wing01/RoomA
Wing02/RoomA
```

Nested templates are supported. Parent exposed ports can target a nested template instance's exposed port, and the compiler resolves the chain to a concrete internal node/port before flattening the graph.

## Deterministic expansion

`WorldPlanSubgraphCompiler.Expand` canonicalizes template lowering by stable identifiers:

```text
node types   -> ordinal type ID
nodes        -> ordinal node ID
connections  -> source/port/target/port/kind/ID
exposed      -> ordinal exposed-port ID
```

Internal IDs are scoped from the template instance path:

```text
instance/child/room
instance/child/door
instance/child/connect
```

The expansion result therefore does not depend on serialized list order. The same semantic input graph produces the same flattened graph.

## Validation

Subgraph expansion validates the hierarchy before the existing flat compiler performs semantic validation.

Hierarchical checks include:

- null templates;
- duplicate template IDs;
- unknown template references;
- duplicate node IDs;
- missing exposed ports;
- invalid nested exposed-port chains;
- missing endpoint nodes;
- template recursion/reference cycles.

The flattened result then receives the normal graph validation for:

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

## Deterministic compilation

`WorldPlanCompiler.Compile(seed, definition)` canonicalizes a flat graph before creating runtime objects.

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

The hierarchical compiler is a lowering wrapper around this canonical flat compiler rather than a second runtime representation.

The seed is retained on the runtime plan for later deterministic expansion/layout stages. The current plan compilers do not consume the seed as mutable random state.

## Editor graph

`WorldPlanGraphAsset` is the Unity serialization boundary.

Create one with:

**Assets > Create > Procedural World > World Plan Graph**

Then define custom node types and open the graph using:

**Window > Procedural World > World Plan Graph**

The current graph editor provides:

- grid-based canvas navigation;
- node creation from registered node types;
- reusable subgraph instance creation from referenced template assets;
- exposed template-port visualization;
- node movement;
- semantic port visualization;
- compatible-port filtering;
- connection creation/removal;
- node deletion;
- validation;
- framing;
- Unity Undo/asset persistence.

The graph view stores only the visual `position` metadata back to the asset when nodes move. That value is excluded from `BuildDefinition()`.

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
WorldPlanSubgraphCompiler
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

## Relationship to world-space realization

Hierarchical expansion is intentionally before layout and placement:

```text
hierarchical semantic plan
          |
          v
flat semantic plan
          |
          v
world-space layout / constraints
          |
          v
feature placement / paths
          |
          v
chunk-local materialization
```

A template has no privileged relationship to chunks. An expanded template may describe a structure that spans multiple chunks, just like any other world-space feature.

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

## Current 0.1.96 scope

Implemented:

- typed runtime world-plan graph model;
- customizable node type schema;
- semantic ports;
- required/optional/derived connection kinds;
- node properties;
- deterministic flat compilation;
- hierarchical template/subgraph definitions;
- exposed template-port boundaries;
- deterministic recursive template expansion;
- scoped IDs for repeated and nested instances;
- hierarchical validation and flat semantic validation;
- Unity `WorldPlanGraphAsset` template/reference authoring;
- Unity GraphView subgraph-instance authoring and port visualization;
- regression coverage for expansion, ordering, nesting, missing templates, missing exposed ports, and connection rewiring.

Not yet implemented:

- deterministic weighted plan selection;
- deterministic world-space layout / constraint solving;
- plan-to-feature placement lowering;
- semantic port world positions;
- world-space corridor/path generation;
- direct `ProceduralWorldGenerator` integration;
- dedicated graph-side property/connection inspectors;
- graph preview of resolved world geometry;
- generation scheduling and background execution.
