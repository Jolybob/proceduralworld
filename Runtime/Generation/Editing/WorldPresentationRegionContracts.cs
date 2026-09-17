using System;

namespace Jolybob.ProceduralWorld
{
    public interface IWorldPresentationRegionResolver
    {
        int ResolveRegion(WorldPosition position);
    }

    public interface IWorldPresentationRegionRenderer
    {
        void RenderRegion(int regionId, WorldChangeBatch batch);
    }
}
