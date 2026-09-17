using System;

namespace Jolybob.ProceduralWorld.Tilemap
{
    /// <summary>
    /// Identifies an independent supplementary visual channel.
    /// Multiple channels may render simultaneously for the same cell.
    /// </summary>
    public readonly struct WorldCellVisualOverlayChannel : IEquatable<WorldCellVisualOverlayChannel>
    {
        public readonly byte Value;

        public WorldCellVisualOverlayChannel(byte value)
        {
            Value = value;
        }

        public bool Equals(WorldCellVisualOverlayChannel other) => Value == other.Value;
        public override bool Equals(object obj) => obj is WorldCellVisualOverlayChannel other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(WorldCellVisualOverlayChannel left, WorldCellVisualOverlayChannel right) => left.Equals(right);
        public static bool operator !=(WorldCellVisualOverlayChannel left, WorldCellVisualOverlayChannel right) => !left.Equals(right);
        public override string ToString() => Value.ToString();
    }
}
