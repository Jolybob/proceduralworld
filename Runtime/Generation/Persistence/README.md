# Persistence layer

The persistence layer stores player/world modifications separately from deterministic generation.

## Responsibilities

- Regenerate a chunk from the normal `ProceduralWorldGenerator` first.
- Apply persisted cell overrides after generation.
- Save only cells whose current state differs from deterministic generation.
- Leave the storage format and backend to the consuming project.
- Remain independent from Tilemap, GameObjects, streaming, and other presentation systems.

## Main types

- `WorldCellModification` — one local cell override.
- `WorldChunkSaveData` — chunk coordinate, format version, and override collection.
- `IWorldChunkStore` — persistence backend contract.
- `WorldChunkPersistenceService` — compares, saves, regenerates, and restores chunk state.
- `InMemoryWorldChunkStore` — lightweight store for tests and prototypes.

## Data flow

```text
ProceduralWorldGenerator
        |
        v
 deterministic chunk
        |
        +---- load persisted overrides ----> loaded chunk
        |
        +---- compare against current ----> sparse save data
                                             |
                                             v
                                       IWorldChunkStore
```

This keeps generated terrain reproducible while allowing player edits to survive unloading and later regeneration.

## Storage format

The package intentionally does not require JSON, binary serialization, a database, or a specific filesystem layout. `IWorldChunkStore` is the boundary where a project can implement any durable format.

`WorldChunkSaveData.FormatVersion` provides an explicit migration point for future save-format changes.

## Example

```csharp
var generator = new ProceduralWorldGenerator(43017, settings);
var store = new InMemoryWorldChunkStore();
var persistence = new WorldChunkPersistenceService(generator, store);

GeneratedChunk chunk = persistence.LoadChunk(new ChunkCoord(10, -4));

GeneratedCell cell = chunk.GetCell(5, 5);
cell.SetResource(new ResourceId(12));
chunk.SetCell(5, 5, cell);

persistence.SaveChunk(chunk);
```

For a real game, replace `InMemoryWorldChunkStore` with a disk, database, cloud, or platform-specific implementation.

## Interaction with streaming

Streaming remains responsible only for which chunks are active. Persistence is responsible for restoring and saving modifications. These systems can be composed later without making either system depend on rendering.
