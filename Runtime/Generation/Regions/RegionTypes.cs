using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>Stable identifier for a generated region or biome.</summary>
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

    /// <summary>Converts environmental field samples into stable region identifiers.</summary>
    public interface IRegionResolver
    {
        RegionId Resolve(EnvironmentSample sample);
    }
}
