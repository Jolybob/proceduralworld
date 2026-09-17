using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public sealed class WorldPresentationRegionDemandChange
    {
        private readonly IReadOnlyList<int> demandedRegions;

        public IReadOnlyList<int> DemandedRegions => demandedRegions;

        internal WorldPresentationRegionDemandChange(IReadOnlyList<int> demandedRegions)
        {
            this.demandedRegions = demandedRegions ?? throw new ArgumentNullException(nameof(demandedRegions));
        }
    }
}
