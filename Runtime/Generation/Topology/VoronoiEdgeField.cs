using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Deterministic one-point-per-cell Voronoi field. Sampling uses world coordinates, so
    /// the pattern remains continuous across chunk boundaries.
    /// </summary>
    public sealed class VoronoiEdgeField : IVoronoiEdgeField
    {
        private readonly int seed;
        private readonly float cellSize;

        public float CellSize => cellSize;

        public VoronoiEdgeField(int seed, float cellSize)
        {
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize));

            this.seed = seed;
            this.cellSize = cellSize;
        }

        public float SampleEdgeDistance(int worldX, int worldY)
        {
            int cellX = Mathf.FloorToInt(worldX / cellSize);
            int cellY = Mathf.FloorToInt(worldY / cellSize);

            float nearest = float.PositiveInfinity;
            float secondNearest = float.PositiveInfinity;

            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    int latticeX = cellX + offsetX;
                    int latticeY = cellY + offsetY;
                    Vector2 featurePoint = GetFeaturePoint(latticeX, latticeY);
                    float dx = worldX - featurePoint.x;
                    float dy = worldY - featurePoint.y;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    if (distance < nearest)
                    {
                        secondNearest = nearest;
                        nearest = distance;
                    }
                    else if (distance < secondNearest)
                    {
                        secondNearest = distance;
                    }
                }
            }

            return Mathf.Max(0f, secondNearest - nearest);
        }

        private Vector2 GetFeaturePoint(int latticeX, int latticeY)
        {
            uint state = Hash(seed, latticeX, latticeY);
            float x = HashTo01(state);
            float y = HashTo01(Mix(state));

            return new Vector2(
                (latticeX + x) * cellSize,
                (latticeY + y) * cellSize);
        }

        private static uint Hash(int seed, int x, int y)
        {
            unchecked
            {
                uint h = (uint)seed;
                h ^= (uint)x * 0x9E3779B9u;
                h ^= (uint)y * 0x85EBCA6Bu;
                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;
                return h;
            }
        }

        private static uint Mix(uint value)
        {
            unchecked
            {
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                return value;
            }
        }

        private static float HashTo01(uint value)
        {
            return (value & 0x00FFFFFFu) / 16777215f;
        }
    }
}
