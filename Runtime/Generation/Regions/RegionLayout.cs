using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Defines macro geography independently from region content and generation passes.
    /// </summary>
    public interface IRegionLayout
    {
        RegionId Resolve(WorldPosition position);
    }

    /// <summary>
    /// Adapts a world-space region layout to the existing region-resolver contract.
    /// </summary>
    public sealed class RegionLayoutResolver : IPositionAwareRegionResolver
    {
        private readonly IRegionLayout layout;
        private readonly RegionId fallbackRegion;

        public RegionLayoutResolver(IRegionLayout layout, RegionId fallbackRegion)
        {
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
            this.fallbackRegion = fallbackRegion;
        }

        public RegionId Resolve(EnvironmentSample sample)
        {
            return fallbackRegion;
        }

        public RegionId Resolve(EnvironmentSample sample, int worldX, int worldY)
        {
            return layout.Resolve(new WorldPosition(worldX, worldY));
        }
    }
}
