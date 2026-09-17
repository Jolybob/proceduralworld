# Chunk streaming layer

The streaming layer controls which chunk coordinates are active. It does not decide how chunks are generated and does not depend on rendering technology.

## Main types

- `IWorldChunkGenerator` isolates deterministic chunk generation behind a small scheduling boundary.
- `IWorldChunkSink` receives load/unload operations and can translate generated chunks into any presentation or world-management system.
- `IChunkStreamingOrder` defines deterministic priority/order for newly loaded chunks.
- `NearestFirstChunkStreamingOrder` prioritizes chunks by Manhattan distance from the streaming center, then stable Y/X order.
- `ChunkStreamingPlanner` computes deterministic load/unload deltas around a center chunk and applies the configured load ordering.
- `ChunkStreamingDelta` contains the coordinates to load and unload for one update.
- `WorldChunkStreamingController` keeps the original immediate-generation workflow.
- `IChunkGenerationScheduler` queues generation requests independently from chunk activation.
- `DeterministicChunkGenerationScheduler` provides stable priority ordering, duplicate coalescing, and cancellation.
- `BudgetedChunkGenerationService` processes a bounded number of queued chunks per call.
- `WorldScheduledChunkStreamingController` composes planning, generation scheduling, cancellation, and sink delivery for frame-budgeted streaming.
- `WorldPersistentChunkStreamingController` adds persistence-aware load, save-before-unload, and reset behavior.
- `WorldScheduledPersistentChunkStreamingController` combines budgeted generation with persistence-aware loading, unloading, and loaded-world access.
- `IWorldChunkAccess` exposes the minimal read/write boundary for currently loaded chunks.
- `WorldChunkCoordinates` converts world positions into chunk coordinates and local cell coordinates, including negative positions.
- `WorldEditService` provides high-level gameplay/player mutation operations on loaded cells.

## Load prioritization

Newly requested chunks are now ordered independently from the active-set calculation. The default `NearestFirstChunkStreamingOrder` processes the closest chunks first, then breaks equal-priority ties by stable Y/X order. This keeps startup and movement work deterministic while allowing a project to provide another scheduling policy.

```csharp
var order = new NearestFirstChunkStreamingOrder();
var planner = new ChunkStreamingPlanner(
    loadRadius: 3,
    unloadRadius: 4,
    loadOrder: order);
```

A custom order receives the exact requested coordinates and the current center and can sort them for distance, gameplay importance, camera direction, biome priority, or another deterministic policy. The planner's active set is unchanged by ordering, so this extension only changes processing order.

The default order uses `long` distance arithmetic to avoid overflow at extreme integer chunk coordinates. Unload ordering remains stable Y-then-X.

## Budgeted generation

Generation can be separated from the streaming update loop. `IWorldChunkGenerator` preserves the existing synchronous generator contract while allowing schedulers to work with alternate generators in tests or production.

`DeterministicChunkGenerationScheduler` stores each coordinate at an integer priority and uses insertion order as the deterministic tie-break. Re-enqueuing an existing coordinate updates its priority instead of creating a duplicate request. Pending work can be cancelled when streaming moves far enough away that the planner unloads the coordinate.

`BudgetedChunkGenerationService.Process(maxChunks, output)` generates at most `maxChunks` requests. No worker thread or Unity object access is introduced by the package: the budget is an explicit main-thread scheduling hook, which keeps generation deterministic and safe for standard Unity presentation adapters.

`WorldScheduledChunkStreamingController` combines the pieces:

```csharp
var controller = new WorldScheduledChunkStreamingController(
    new ProceduralWorldGenerator(196423, settings),
    new ChunkStreamingPlanner(loadRadius: 2, unloadRadius: 3),
    sink);

controller.Update(new ChunkCoord(10, -4));
controller.Process(maxChunks: 2);
```

`Update()` plans activation and queues work; `Process()` performs only the configured amount of generation and forwards completed chunks to the sink. Unloaded coordinates are cancelled from the pending queue before the sink receives their unload notification. The original `WorldChunkStreamingController` remains available for projects that want immediate generation.

## Budgeted persistent streaming

`WorldScheduledPersistentChunkStreamingController` extends the same scheduling boundary to persistent worlds. Planning and queuing remain separate from execution, but each generated request is resolved through `WorldChunkPersistenceService.LoadChunk`, so the generated base state and any saved overrides are restored only when the request actually runs.

```text
ChunkStreamingPlanner
        |
        v
IChunkGenerationScheduler
        |
        v
Budgeted generation
        |
        v
WorldChunkPersistenceService
        |
        +----> loaded world state
        |
        v
IWorldChunkSink
```

```csharp
var controller = new WorldScheduledPersistentChunkStreamingController(
    persistence,
    new ChunkStreamingPlanner(loadRadius: 2, unloadRadius: 3),
    sink);

controller.Update(new ChunkCoord(10, -4));
controller.Process(maxChunks: 2);
```

This means a chunk that becomes irrelevant before its generation budget is reached never needs to be generated or restored. When a loaded chunk leaves the active set, it is saved before the sink receives its unload notification. `Reset()` saves all loaded chunks and clears both planned and pending generation work. The controller also implements `IWorldChunkAccess`, so gameplay code can use the same loaded-world boundary as the immediate persistent controller.

## Radius model

`loadRadius` defines the square of chunks that must be active. `unloadRadius` can be larger to provide hysteresis and avoid repeated unload/load operations when the player moves near the load boundary.

A radius of `2` activates 25 chunks. Newly loaded coordinates are emitted nearest-first by default, while equal-priority ties remain deterministic. This makes streaming work easier to schedule and test.

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

The scheduled persistent controller preserves those lifecycle guarantees while adding a generation budget between planning and persistence.

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

## Separation

Generation remains deterministic and authoritative for the generated base state. Streaming decides when a chunk should become active. Load ordering is a scheduling policy, not part of generation, so changing the ordering strategy does not alter generated chunk contents. The generation scheduler adds a second independent policy: how much pending generation work is allowed to execute during one update. Persistence stores edits separately from that generated base state. The scheduled persistent controller composes both concerns without moving persistence into the scheduler itself. `IWorldChunkAccess` exposes only loaded state to gameplay systems. Rendering adapters can consume loaded chunks without becoming part of the planner, scheduler, or persistence backend.

The preview seed used by this architecture revision is `196423`.
