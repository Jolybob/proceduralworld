namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Normalized environmental values produced by the field layer.
    /// Region, terrain, cave, and structure systems can consume the same sample.
    /// </summary>
    public readonly struct EnvironmentSample
    {
        public readonly float Temperature;
        public readonly float Moisture;
        public readonly float Elevation;
        public readonly float Distance;

        public EnvironmentSample(float temperature, float moisture, float elevation, float distance)
        {
            Temperature = temperature;
            Moisture = moisture;
            Elevation = elevation;
            Distance = distance;
        }
    }
}
