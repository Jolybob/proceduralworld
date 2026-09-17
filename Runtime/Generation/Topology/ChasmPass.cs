using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Creates long deterministic chasms along Voronoi cell boundaries.
    /// The pass modifies only canonical generated cell topology and presentation tile state.
    /// </summary>
    public sealed class ChasmPass : IWorldGenerationPass
    {
        public int Order => 350;

        private readonly IVoronoiEdgeField field;
        private readonly float width;
        private readonly float minimumDistance;

        public ChasmPass(WorldGenerationSettings settings)
            : this(null, settings)
        {
        }

        public ChasmPass(IVoronoiEdgeField field, WorldGenerationSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            this.field = field ?? new VoronoiEdgeField(
                settings.chasmSeedOffset,
                settings.chasmCellSize);
            width = Mathf.Max(0f, settings.chasmWidth);
            minimumDistance = Mathf.Max(0f, settings.chasmMinimumDistance);
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!context.Settings.chasmsEnabled)
                return;

            int size = context.Chunk.Size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int worldX = context.ChunkCoordinate.X * size + x;
                    int worldY = context.ChunkCoordinate.Y * size + y;

                    if (!CanCreateChasm(worldX, worldY))
                        continue;
                    if (field.SampleEdgeDistance(worldX, worldY) > width)
                        continue;

                    GeneratedCell cell = context.Chunk.GetCell(x, y);
                    if (cell.Topology == CellTopology.Water || cell.Topology == CellTopology.Lava)
                        continue;

                    cell.SetTopology(CellTopology.Chasm, WorldTile.Empty);
                    context.Chunk.SetCell(x, y, cell);
                }
            }
        }

        private bool CanCreateChasm(int worldX, int worldY)
        {
            long squaredDistance = (long)worldX * worldX + (long)worldY * worldY;
            long minimumSquared = (long)minimumDistance * (long)minimumDistance;
            return squaredDistance >= minimumSquared;
        }
    }
}
