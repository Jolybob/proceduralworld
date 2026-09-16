using System;

namespace Jolybob.ProceduralWorld
{
    [Serializable]
    public readonly struct RegionId : IEquatable<RegionId>
    {
        public readonly byte Value;
        public RegionId(byte value) => Value = value;
        public bool Equals(RegionId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is RegionId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(RegionId left, RegionId right) => left.Equals(right);
        public static bool operator !=(RegionId left, RegionId right) => !left.Equals(right);
        public override string ToString() => Value.ToString();
    }

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

    public interface IRegionResolver
    {
        RegionId Resolve(EnvironmentSample sample);
    }
}
