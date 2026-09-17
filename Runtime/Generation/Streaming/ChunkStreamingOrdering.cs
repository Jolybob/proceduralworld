using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Defines the deterministic order in which newly requested chunks are processed.
    /// </summary>
    public interface IChunkStreamingOrder
    {
        void Sort(IList<ChunkCoord> coordinates, ChunkCoord center);
    }

    /// <summary>
    /// Prioritizes chunks by Manhattan distance from the streaming center.
    /// Ties prefer smaller absolute Y, then lower Y, then lower X.
    /// This keeps the center and horizontal neighbors available first while remaining deterministic.
    /// </summary>
    public sealed class NearestFirstChunkStreamingOrder : IChunkStreamingOrder
    {
        public void Sort(IList<ChunkCoord> coordinates, ChunkCoord center)
        {
            if (coordinates == null)
                throw new ArgumentNullException(nameof(coordinates));

            for (int i = 1; i < coordinates.Count; i++)
            {
                ChunkCoord value = coordinates[i];
                int j = i - 1;

                while (j >= 0 && Compare(coordinates[j], value, center) > 0)
                {
                    coordinates[j + 1] = coordinates[j];
                    j--;
                }

                coordinates[j + 1] = value;
            }
        }

        private static int Compare(ChunkCoord left, ChunkCoord right, ChunkCoord center)
        {
            long leftDistance = Math.Abs((long)left.X - center.X) + Math.Abs((long)left.Y - center.Y);
            long rightDistance = Math.Abs((long)right.X - center.X) + Math.Abs((long)right.Y - center.Y);

            int distance = leftDistance.CompareTo(rightDistance);
            if (distance != 0)
                return distance;

            long leftAbsoluteY = Math.Abs((long)left.Y - center.Y);
            long rightAbsoluteY = Math.Abs((long)right.Y - center.Y);
            int absoluteY = leftAbsoluteY.CompareTo(rightAbsoluteY);
            if (absoluteY != 0)
                return absoluteY;

            int y = left.Y.CompareTo(right.Y);
            if (y != 0)
                return y;

            return left.X.CompareTo(right.X);
        }
    }
}
