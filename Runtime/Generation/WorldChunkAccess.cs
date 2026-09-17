using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Read/write access to chunks that are currently part of the active world state.
    /// Implementations decide how chunks are loaded and persisted.
    /// </summary>
    public interface IWorldChunkAccess
    {
        bool TryGetChunk(ChunkCoord coordinate, out GeneratedChunk chunk);
        bool TryGetCell(WorldPosition position, out GeneratedCell cell);
        bool SetCell(WorldPosition position, GeneratedCell cell);
    }

    /// <summary>
    /// Utility methods for converting world coordinates into chunk-local coordinates.
    /// </summary>
    public static class WorldChunkCoordinates
    {
        public static ChunkCoord ToChunk(WorldPosition position, int chunkSize)
        {
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));

            return new ChunkCoord(
                FloorDiv(position.X, chunkSize),
                FloorDiv(position.Y, chunkSize));
        }

        public static int ToLocalX(WorldPosition position, int chunkSize)
        {
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            return Mod(position.X, chunkSize);
        }

        public static int ToLocalY(WorldPosition position, int chunkSize)
        {
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            return Mod(position.Y, chunkSize);
        }

        private static int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            if (remainder < 0)
                quotient--;
            return quotient;
        }

        private static int Mod(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }
    }
}
