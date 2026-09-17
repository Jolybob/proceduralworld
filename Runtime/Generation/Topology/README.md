# Topology generation

Topology is the layer that answers **what occupies a cell** independently from region identity, material, content, or rendering.

## Canonical state

`GeneratedCell.Topology` currently supports:

- `Solid`
- `Empty`
- `Water`
- `Lava`
- `Chasm`

This makes caves and future liquids/chasm systems explicit instead of using `WorldTile.Empty` as hidden gameplay state.

## Chasms

`VoronoiEdgeField` creates a deterministic one-point-per-lattice-cell Voronoi diagram using world coordinates. `ChasmPass` converts a configurable distance band around those Voronoi boundaries into `CellTopology.Chasm`.

Because the field is sampled in world space, chunk boundaries do not reset the pattern. The same seed and world coordinate always produce the same edge distance.

```csharp
var settings = new WorldGenerationSettings
{
    chasmsEnabled = true,
    chasmCellSize = 80f,
    chasmWidth = 2.5f,
    chasmMinimumDistance = 180f,
    chasmSeedOffset = 606
};

var pipeline = new WorldGenerationPipeline()
    .Add(new RegionBiomePass(resolver))
    .Add(new TerrainPass(regions, terrains))
    .Add(new CavePass(settings))
    .Add(new ChasmPass(settings))
    .Add(new ResourcePass(resources));
```

Topology modifiers are data-only. Rendering adapters decide how `Water`, `Lava`, and `Chasm` should appear in a particular game.
