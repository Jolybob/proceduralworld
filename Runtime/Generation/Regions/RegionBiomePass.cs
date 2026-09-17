using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Converts reusable environmental field samples into stable region identities.
    /// The pass does not generate noise or own field configuration.
    /// Position-aware resolvers receive world coordinates; legacy resolvers remain supported.
    /// </summary>
    public sealed class RegionBiomePass : IWorldGenerationPass
    {
        public int Order => 100;

        private readonly IRegionResolver resolver;

        public RegionBiomePass(int seed, WorldGenerationSettings settings)
            : this(new ThresholdRegionResolver())
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
        }

        public RegionBiomePass(int seed, WorldGenerationSettings settings, IRegionResolver resolver)
            : this(resolver)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
        }

        public RegionBiomePass(IRegionResolver resolver)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            int size = context.Chunk.Size;
            var positionAwareResolver = resolver as IPositionAwareRegionResolver;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int worldX = context.ChunkCoordinate.X * size + x;
                    int worldY = context.ChunkCoordinate.Y * size + y;

                    EnvironmentSample sample = context.EnvironmentFields.Sample(worldX, worldY);
                    RegionId region = positionAwareResolver != null
                        ? positionAwareResolver.Resolve(new WorldPosition(worldX, worldY), sample)
                        : resolver.Resolve(sample);

                    var cell = context.Chunk.GetCell(x, y);
                    cell.SetRegion(region);
                    context.Chunk.SetCell(x, y, cell);
                }
            }
        }
    }
}
