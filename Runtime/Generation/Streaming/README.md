# Chunk streaming layer

The streaming layer controls which chunk coordinates are active. It does not decide how chunks are generated and does not depend on rendering technology.

## Main types

- `IWorldChunkSink` receives load/unload operations and can translate generated chunks into any presentation or world-management system.
- `ChunkStreamingPlanner` computes deterministic load/unload deltas around a center chunk.
- `ChunkStreamingDelta` contains the coordinates to load and unload for one update.
- `WorldChunkStreamingController` connects the planner to `ProceduralWorldGenerator` and a sink.
- `WorldPersistentChunkStreamingController` adds persistence-aware load, save-before-unload, and reset behavior.

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

The controller keeps loaded chunk references only for this lifecycle boundary. It does not know how the sink renders or stores the world, and it does not alter the generator's deterministic rules.

## Separation

Generation remains deterministic and authoritative for the generated base state. Streaming decides when a chunk should become active. Persistence stores edits separately from that generated base state. Rendering adapters can consume loaded chunks without becoming part of the streaming planner.

## Example

```csharp
var generator = new ProceduralWorldGenerator(60427, settings);
var store = new InMemoryWorldChunkStore();
var persistence = new WorldChunkPersistenceService(generator, store);
var planner = new ChunkStreamingPlanner(loadRadius: 2, unloadRadius: 3);
var streaming = new WorldPersistentChunkStreamingController(persistence, planner, sink);

streaming.Update(new ChunkCoord(10, -4));
```

The preview seed used by this architecture revision is `60427`.
