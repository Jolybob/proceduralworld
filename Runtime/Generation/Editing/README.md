# World editing architecture

The editing layer is the gameplay-facing mutation boundary for loaded world data.

```text
player / gameplay system
          |
          v
   WorldEditService
          |
          +----> IWorldChunkAccess ----> loaded chunk state
          |
          +----> IWorldChangeJournal -> history / persistence-independent change source
                                             |
                                             +----> WorldEditHistory
                                             |
                                             +----> WorldChangeObserverJournal
                                                        |
                                                        +----> IWorldChangeListener
                                                        |
                                                        +----> IWorldChangeBatchListener
                                                                   |
                                                                   +----> IWorldChangeRenderer
                                                                                 |
                                                                                 +----> WorldTilemapRenderer
```

## Responsibilities

- Keep gameplay edits behind `WorldEditService` instead of exposing chunk internals.
- Record successful mutations as explicit before/after changes.
- Preserve deterministic generation and persistence as separate systems.
- Allow a project to provide its own journal implementation for networking, undo/redo, analytics, or replay.
- Publish canonical changes to reactive consumers without coupling the journal to rendering or gameplay systems.
- Keep presentation code behind a renderer boundary so world state does not depend on Tilemap, mesh, UI, or other rendering technologies.

## Main types

- `WorldEditService` — performs controlled mutations against `IWorldChunkAccess`.
- `WorldEditTransaction` — groups multiple mutations into one commit/rollback lifecycle.
- `WorldEditOperationKind` — identifies the kind of mutation that occurred.
- `WorldCellChange` — captures position, before state, after state, and operation kind.
- `IWorldChangeJournal` — backend-neutral change recording contract.
- `IWorldChangeBatchJournal` — optional journal capability for publishing several recorded changes as one logical batch.
- `InMemoryWorldChangeJournal` — lightweight implementation for tests and prototypes.
- `WorldEditHistory` — grouped undo/redo over journal records.
- `IWorldChangeListener` — receives successfully recorded single-cell changes.
- `IWorldChangeBatchListener` — receives transaction-scale logical change batches.
- `WorldChangeBatch` — immutable snapshot of related changes.
- `WorldChangeObserverJournal` — decorates a journal with per-cell and logical-batch notifications.
- `IWorldChangeRenderer` — presentation contract that consumes canonical changes without owning world state.
- `WorldChangeRenderObserver` — connects the observable journal to an `IWorldChangeRenderer` and manages both subscriptions.
- `WorldTilemapRenderer` — concrete Unity Tilemap adapter that also implements `IWorldChunkSink` for streaming integration.

## Change tracking

A successful mutation is recorded only after the underlying access layer accepts the new cell state. No-op edits are not recorded, and edits to unloaded chunks fail without creating changes.

```csharp
var journal = new InMemoryWorldChangeJournal();
var edits = new WorldEditService(worldAccess, settings.chunkSize, journal);

WorldPosition position = new WorldPosition(12, -7);
if (edits.TrySetTile(position, WorldTile.Core))
{
    WorldCellChange change = journal.Changes[journal.Changes.Count - 1];
    // change.Before and change.After can be persisted, replicated, or undone.
}
```

## Transactions and logical batching

Use `WorldEditTransaction` when several mutations represent one logical operation. The transaction records changes locally until `Commit()` and can restore the original states with `Rollback()`.

When the target journal implements `IWorldChangeBatchJournal`, a committed transaction is published as one `WorldChangeBatch`. The underlying journal still retains every `WorldCellChange`, so history and persistence integrations keep their per-cell source of truth while presentation systems can update once per logical operation.

```csharp
var source = new InMemoryWorldChangeJournal();
var journal = new WorldChangeObserverJournal(source);
var transaction = new WorldEditTransaction(worldAccess, chunkSize, journal);

transaction.TrySetTile(new WorldPosition(10, 10), WorldTile.Core);
transaction.TrySetResource(new WorldPosition(11, 10), new ResourceId(12));
transaction.Commit();
```

## Reactive notifications

`WorldChangeObserverJournal` provides two notification levels. `Subscribe()` sends individual changes immediately after they are recorded. `SubscribeBatch()` sends one immutable `WorldChangeBatch` for a transaction commit when the commit uses a batching-capable journal.

Observers are called in registration order from a snapshot, so subscribing or unsubscribing during a callback does not invalidate the active notification iteration. Subscriptions are disposable.

## Presentation adapters

Presentation code should not inspect streaming controllers, persistence services, or generated chunks directly when it only needs to react to mutations. Implement `IWorldChangeRenderer` and connect it with `WorldChangeRenderObserver`.

```csharp
var journal = new WorldChangeObserverJournal(new InMemoryWorldChangeJournal());
var renderer = new MyWorldRenderer();
using (var observer = new WorldChangeRenderObserver(journal, renderer))
{
    var edits = new WorldEditService(worldAccess, chunkSize, journal);
    edits.TrySetTile(position, replacement);
}
```

`Render(WorldCellChange)` is used for direct edits. `RenderBatch(WorldChangeBatch)` is used for transaction commits, preventing the common error where a multi-cell operation causes one rendering pass per cell. A concrete implementation can translate these contracts into Tilemap updates, mesh invalidation, VFX, UI refreshes, or another presentation technology without changing the world-generation or editing layers.

`WorldTilemapRenderer` provides the first concrete adapter. It accepts a `UnityEngine.Tilemaps.Tilemap`, a chunk size, and a `WorldTile` to `TileBase` mapping. As an `IWorldChunkSink`, `Load()` paints a complete generated chunk and `Unload()` clears only that chunk's bounds. As an `IWorldChangeRenderer`, direct edits update one position while logical batches use `Tilemap.SetTiles()` and coalesce repeated positions to the latest `After.Tile` state.

Example streaming wiring:

```csharp
var tilemapRenderer = new WorldTilemapRenderer(tilemap, settings.chunkSize, tiles);
var streaming = new WorldPersistentChunkStreamingController(
    persistence,
    planner,
    tilemapRenderer);
```

The same adapter can be attached to `WorldChangeRenderObserver` to keep streamed presentation and edit presentation on the same Tilemap without making the generation or editing layers depend on Unity rendering types.

No rendering adapter owns canonical world state. `IWorldChunkAccess` remains the state boundary and `WorldCellChange` remains the shared data contract.
