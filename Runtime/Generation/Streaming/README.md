# Chunk streaming layer

The streaming layer controls which chunk coordinates are active. It does not decide how chunks are generated and does not depend on rendering technology.

## Main types

- `IWorldChunkSink` receives load/unload operations and can translate generated chunks into any presentation or world-management system.
- `ChunkStreamingPlanner` computes deterministic load/unload deltas around a center chunk.
- `ChunkStreamingDelta` contains the coordinates to load and unload for one update.
- `WorldChunkStreamingController` connects the planner to `ProceduralWorldGenerator` and a sink.

## Radius model

`loadRadius` defines the square of chunks that must be active. `unloadRadius` can be larger to provide hysteresis and avoid repeated unload/load operations when the player moves near the load boundary.

A radius of `2` activates 25 chunks around the center. Coordinates are returned in stable Y-then-X order, which makes consumers easier to test and gives deterministic work ordering.

## Separation

Generation remains deterministic and authoritative for chunk contents. Streaming only decides when a chunk should become active. Persistence can later sit behind `IWorldChunkSink` without changing the generator, and rendering adapters can consume loaded chunks without becoming part of the streaming planner.

## Example

```csharp
var generator = new ProceduralWorldGenerator(75319, settings);
var planner = new ChunkStreamingPlanner(loadRadius: 2, unloadRadius: 3);
var streaming = new WorldChunkStreamingController(generator, planner, sink);

streaming.Update(new ChunkCoord(10, -4));
```

The preview seed used by this architecture revision is `75319`.
