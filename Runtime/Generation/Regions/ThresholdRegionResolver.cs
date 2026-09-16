using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    [Serializable]
    public sealed class ThresholdRegionResolver : IRegionResolver
    {
        [SerializeField, Range(0f, 1f)] private float coldTemperature = 0.33f;
        [SerializeField, Range(0f, 1f)] private float wetMoisture = 0.66f;
        [SerializeField, Range(0f, 1f)] private float coreDistance = 0.14f;

        public RegionId Resolve(EnvironmentSample sample)
        {
            if (sample.Distance <= coreDistance)
                return new RegionId(0);
            if (sample.Temperature <= coldTemperature)
                return new RegionId(sample.Moisture >= wetMoisture ? (byte)1 : (byte)2);
            return new RegionId(sample.Moisture >= wetMoisture ? (byte)3 : (byte)4);
        }
    }
}
