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
```

## Responsibilities

- Keep gameplay edits behind `WorldEditService` instead of exposing chunk internals.
- Record successful mutations as explicit before/after changes.
- Preserve deterministic generation and persistence as separate systems.
- Allow a project to provide its own journal implementation for networking, undo/redo, analytics, or replay.
- Publish canonical changes to reactive consumers without coupling the journal to rendering or gameplay systems.

## Main types

- `WorldEditService` — performs controlled mutations against `IWorldChunkAccess`.
- `WorldEditOperationKind` — identifies the kind of mutation that occurred.
- `WorldCellChange` — captures position, before state, after state, and operation kind.
- `IWorldChangeJournal` — backend-neutral change recording contract.
- `InMemoryWorldChangeJournal` — lightweight implementation for tests and prototypes.
- `WorldEditHistory` — grouped undo/redo over journal records.
- `IWorldChangeListener` — receives successfully recorded changes.
- `WorldChangeObserverJournal` — decorates any journal with change notifications and disposable subscriptions.

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

## Reactive notifications

Wrap the journal when other systems should react to the same canonical change records:

```csharp
var source = new InMemoryWorldChangeJournal();
var journal = new WorldChangeObserverJournal(source);
using (journal.Subscribe(listener))
{
    var edits = new WorldEditService(worldAccess, chunkSize, journal);
    edits.TrySetCell(position, replacement);
}
```

`WorldChangeObserverJournal` keeps the wrapped journal as the history source of truth and forwards each recorded change to subscribed `IWorldChangeListener` instances. Subscriptions are disposable and observer callbacks run in registration order from a snapshot, so subscribing or unsubscribing during a callback is safe.

This boundary is suitable for tilemap invalidation, UI updates, audio, gameplay reactions, multiplayer transport, replay recording, and analytics. No observer owns the world state; the canonical `WorldCellChange` remains the shared contract.
