# World change tracking

The editing layer is the gameplay-facing mutation boundary for loaded world data.

## Responsibilities

- Keep gameplay edits behind `WorldEditService` instead of exposing chunk internals.
- Record successful mutations as explicit before/after changes.
- Preserve deterministic generation and persistence as separate systems.
- Allow a project to provide its own journal implementation for networking, undo/redo, analytics, or replay.

## Main types

- `WorldEditService` — performs validated mutations against `IWorldChunkAccess`.
- `WorldEditOperationKind` — identifies the kind of mutation that occurred.
- `WorldCellChange` — captures position, before state, after state, and operation kind.
- `IWorldChangeJournal` — backend-neutral change recording contract.
- `InMemoryWorldChangeJournal` — lightweight implementation for tests and prototypes.

## Data flow

```text
player / gameplay system
          |
          v
   WorldEditService
          |
          +----> IWorldChunkAccess ----> loaded chunk state
          |
          +----> IWorldChangeJournal -> change history / networking / undo
```

A successful mutation is recorded only after the underlying access layer accepts the new cell state. No-op edits are not recorded, and edits to unloaded chunks fail without creating changes.

## Example

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

The journal is intentionally not part of procedural generation. Generated chunks remain deterministic, while edits become explicit world-state changes that higher-level systems can consume.
