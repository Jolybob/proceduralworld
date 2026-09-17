using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Deterministically assigns surface liquid topology from reusable environment fields.
    /// This is intentionally a topology decision only; rendering and simulation remain outside generation.
    /// </summary>
    public sealed class LiquidTopologyPass : ITopologyModifier
    {
        public int Order => 50;

        private readonly float waterElevationThreshold;
        private readonly float waterMoistureThreshold;
        private readonly float lavaElevationThreshold;
        private readonly float lavaHeatThreshold;

        public LiquidTopologyPass(WorldGenerationSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            waterElevationThreshold = Mathf.Clamp01(settings.waterElevationThreshold);
            waterMoistureThreshold = Mathf.Clamp01(settings.waterMoistureThreshold);
            lavaElevationThreshold = Mathf.Clamp01(settings.lavaElevationThreshold);
            lavaHeatThreshold = Mathf.Clamp01(settings.lavaHeatThreshold);
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            int size = context.Chunk.Size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    GeneratedCell cell = context.Chunk.GetCell(x, y);
                    if (cell.Topology != CellTopology.Solid)
                        continue;

                    int worldX = context.ChunkCoordinate.X * size + x;
                    int worldY = context.ChunkCoordinate.Y * size + y;
                    EnvironmentSample sample = context.EnvironmentFields.Sample(worldX, worldY);

                    if (sample.Elevation <= waterElevationThreshold &&
                        sample.Moisture >= waterMoistureThreshold)
                    {
                        cell.SetTopology(CellTopology.Water, cell.Tile);
                    }
                    else if (sample.Elevation >= lavaElevationThreshold &&
                             sample.Temperature >= lavaHeatThreshold)
                    {
                        cell.SetTopology(CellTopology.Lava, cell.Tile);
                    }

                    context.Chunk.SetCell(x, y, cell);
                }
            }
        }
    }
}
