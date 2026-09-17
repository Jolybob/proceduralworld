# Post-process layer

The post-process layer is the final data-only stage of chunk generation before streaming, persistence, and presentation.

## Responsibilities

- Apply composable world-data modifications after caves, resources, and structures.
- Keep modification rules independent from Tilemap, sprites, GameObjects, or other renderers.
- Preserve deterministic behavior by giving every step an explicit random-stream salt.
- Allow projects to add or replace modification steps without changing the core generator.

## Main types

- `IWorldPostProcessStep` defines one ordered modification step.
- `WorldPostProcessPipeline` sorts and executes post-process steps.
- `WorldPostProcessContext` exposes the generated chunk, settings, world coordinates, cell access, and a step-isolated deterministic random stream.
- `WorldPostProcessPass` plugs the post-process pipeline into `WorldGenerationPipeline` at order `900`.

## Determinism

A post-process step should give itself a stable `Salt` value. The random stream is derived from the world seed, chunk coordinate, the `PostProcess` random domain, and that salt.

Changing the implementation of one step should not require changing the random stream of unrelated post-process steps.

## Example

```csharp
public sealed class ReserveRareAreaStep : IWorldPostProcessStep
{
    public int Order => 100;
    public uint Salt => 0x52415245u;

    public void Execute(WorldPostProcessContext context)
    {
        for (int y = 0; y < context.Chunk.Size; y++)
        {
            for (int x = 0; x < context.Chunk.Size; x++)
            {
                var cell = context.GetCell(x, y);
                if (cell.Tile == WorldTile.Empty)
                    continue;

                if (!context.Random.Chance(0.01f))
                    continue;

                cell.Flags |= GeneratedCellFlags.Reserved;
                context.SetCell(x, y, cell);
                return;
            }
        }
    }
}
```

Projects can inject a `WorldPostProcessPipeline` into `ProceduralWorldGenerator`. The default generator also creates an empty post-process stage so the architecture has a stable insertion point for future world-modification systems.
