# Topology generation

Topology is the layer that answers **what occupies a cell** independently from region identity, material, content, or rendering.

## Canonical state

`GeneratedCell.Topology` currently supports:

- `Solid`
- `Empty`
- `Water`
- `Lava`
- `Chasm`

This makes caves and liquids explicit instead of using `WorldTile.Empty` as hidden gameplay state.

## Topology pipeline

Topology modifiers are composed through `TopologyPipeline` and inserted into the main generation flow by `TopologyPass`.

```csharp
var topology = new TopologyPipeline()
    .Add(new LiquidTopologyPass(settings))
    .Add(new ChasmPass(settings));

var pipeline = new WorldGenerationPipeline()
    .Add(new RegionBiomePass(resolver))
    .Add(new TerrainPass(regions, terrains))
    .Add(new CavePass(settings))
    .Add(new TopologyPass(topology))
    .Add(new ResourcePass(resources));
```

A custom topology feature implements `ITopologyModifier` and chooses an order within the topology layer. The main generation pipeline remains unaware of whether the topology layer contains liquids, chasms, erosion, bridges, or future modifiers.

## Liquids

`LiquidTopologyPass` provides a deterministic first-stage liquid model. Water is assigned from low elevation plus sufficient moisture; lava is assigned from high elevation plus sufficient temperature. The pass only changes cells that are still `Solid`, so caves and other earlier topology decisions are not overwritten.

Liquids are disabled by default so existing generated worlds retain their previous topology. Enable `WorldGenerationSettings.liquidsEnabled` and tune `waterElevationThreshold`, `waterMoistureThreshold`, `lavaElevationThreshold`, and `lavaHeatThreshold`.

This is intentionally **not** a fluid simulation. It establishes a stable generated topology classification that later gameplay or simulation systems may evolve without changing deterministic world generation.

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

var topology = new TopologyPipeline()
    .Add(new ChasmPass(settings));
```

Topology modifiers are data-only. Rendering adapters decide how `Water`, `Lava`, and `Chasm` should appear in a particular game.
