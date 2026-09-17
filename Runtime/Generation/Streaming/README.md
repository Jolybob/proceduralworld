# Chunk streaming layer

The streaming layer controls which chunk coordinates are active. It does not decide how chunks are generated and does not depend on rendering technology.

## Main types

- `IWorldChunkSink` receives load/unload operations and can translate generated chunks into any presentation or world-management system.
- `ChunkStreamingPlanner` computes deterministic load/unload deltas around a center chunk.
- `ChunkStreamingDelta` contains the coordinates to load and unload for one update.
- `WorldChunkStreamingController` connects the planner to `ProceduralWorldGenerator` and a sink.
- `WorldPersistentChunkStreamingController` adds persistence-aware load, save-before-unload, and reset behavior.
- `IWorldChunkAccess` exposes the minimal read/write boundary for currently loaded chunks.
- `WorldChunkCoordinates` converts world positions into chunk coordinates and local cell coordinates, including negative positions.
- `WorldEditService` provides high-level gameplay/player mutation operations on loaded cells.

## Radius model

`loadRadius` defines the square of chunks that must be active. `unloadRadius` can be larger to provide hysteresis and avoid repeated unload/load operations when the player moves near the load boundary.

A radius of `2` activates 25 chunks around the center. Coordinates are returned in stable Y-then-X order, which makes consumers easier to test and gives deterministic work ordering.

## Persistent lifecycle

`WorldPersistentChunkStreamingController` composes three independent boundaries:

```text
ChunkStreamingPlanner
        |
        v
persistent controller
   |           |
   v           v
WorldChunkPersistenceService   IWorldChunkSink
   |
   v
IWorldChunkStore
```

On load, the controller asks `WorldChunkPersistenceService` for the chunk, so deterministic generation happens first and saved overrides are then applied. On unload, the currently loaded chunk is saved before the sink receives its unload notification. `Reset()` saves and unloads every currently loaded chunk before clearing the planner state.

The controller now implements `IWorldChunkAccess`. This creates a narrow boundary between world lifecycle management and gameplay systems: gameplay code does not need to know about the streaming planner, persistence backend, or rendering sink.

## World editing boundary

`WorldEditService` sits above `IWorldChunkAccess` and provides explicit mutation operations for loaded cells. It can read or replace a complete `GeneratedCell`, change a cell's tile, add/remove a resource, and add/remove a structure.

World positions are mapped with mathematical floor division so negative coordinates behave correctly:

```text
world (-1, 2), chunk size 4
        |
        v
chunk (-1, 0), local (3, 2)
```

Edits only succeed for currently loaded chunks. This prevents gameplay code from silently mutating an unloaded chunk and keeps chunk activation as a streaming responsibility.

Example:

```csharp
var generator = new ProceduralWorldGenerator(71593, settings);
var store = new InMemoryWorldChunkStore();
var persistence = new WorldChunkPersistenceService(generator, store);
var planner = new ChunkStreamingPlanner(loadRadius: 2, unloadRadius: 3);
var streaming = new WorldPersistentChunkStreamingController(persistence, planner, sink);
var edits = new WorldEditService(streaming, settings.chunkSize);

streaming.Update(new ChunkCoord(10, -4));
edits.TrySetTile(new WorldPosition(641, -255), WorldTile.Core);
```

## Separation

Generation remains deterministic and authoritative for the generated base state. Streaming decides when a chunk should become active. Persistence stores edits separately from that generated base state. `IWorldChunkAccess` exposes only loaded state to gameplay systems. Rendering adapters can consume loaded chunks without becoming part of the streaming planner or edit service.

## Example

```csharp
var generator = new ProceduralWorldGenerator(71593, settings);
var store = new InMemoryWorldChunkStore();
var persistence = new WorldChunkPersistenceService(generator, store);
var planner = new ChunkStreamingPlanner(loadRadius: 2, unloadRadius: 3);
var streaming = new WorldPersistentChunkStreamingController(persistence, planner, sink);

streaming.Update(new ChunkCoord(10, -4));
```

The preview seed used by this architecture revision is `71593`.
