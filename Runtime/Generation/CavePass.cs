using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Carves terrain using a dedicated cave field. This pass only modifies cell state;
    /// rendering adapters do not need to know how caves were generated.
    /// </summary>
    public sealed class CavePass : IWorldGenerationPass
    {
        public int Order => 300;

        private readonly ICaveFieldProvider fieldProvider;
        private readonly float threshold;
        private readonly float minimumDistance;

        public CavePass(int seed, WorldGenerationSettings settings)
            : this(new DefaultCaveFieldProvider(seed, settings), settings)
        {
        }

        public CavePass(ICaveFieldProvider fieldProvider, WorldGenerationSettings settings)
        {
            this.fieldProvider = fieldProvider ?? throw new ArgumentNullException(nameof(fieldProvider));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            threshold = Mathf.Clamp01(settings.caveThreshold);
            minimumDistance = Mathf.Max(0f, settings.caveMinimumDistance);
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!context.Settings.cavesEnabled)
                return;

            int size = context.Chunk.Size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int worldX = context.ChunkCoordinate.X * size + x;
                    int worldY = context.ChunkCoordinate.Y * size + y;

                    if (!CanCarve(worldX, worldY))
                        continue;

                    float caveValue = fieldProvider.Sample(worldX, worldY);
                    if (caveValue < threshold)
                        continue;

                    var cell = context.Chunk.GetCell(x, y);
                    cell.Tile = WorldTile.Empty;
                    cell.Flags |= GeneratedCellFlags.Carved;
                    context.Chunk.SetCell(x, y, cell);
                }
            }
        }

        private bool CanCarve(int worldX, int worldY)
        {
            float distance = Mathf.Sqrt((float)worldX * worldX + (float)worldY * worldY);
            return distance >= minimumDistance;
        }
    }
}
