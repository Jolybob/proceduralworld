using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Stable domains for deterministic generation streams. Adding a domain does not change
    /// the sequence produced by existing domains.
    /// </summary>
    public enum WorldRandomDomain : uint
    {
        General = 0x13579BDFu,
        Terrain = 0x2468ACE1u,
        Caves = 0x31415926u,
        Resources = 0x27182818u,
        Structures = 0xC001D00Du,
        PostProcess = 0x5EED1234u
    }

    /// <summary>
    /// Small deterministic random stream intended for procedural generation.
    /// </summary>
    public interface IWorldRandom
    {
        uint NextUInt();
        int NextInt(int minInclusive, int maxExclusive);
        float NextFloat01();
        bool Chance(float probability);
    }

    /// <summary>
    /// Deterministic xorshift-based random stream with an explicit per-world domain and chunk seed.
    /// It does not use UnityEngine.Random or runtime-dependent hash functions.
    /// </summary>
    public sealed class DeterministicWorldRandom : IWorldRandom
    {
        private uint state;

        public DeterministicWorldRandom(int seed, ChunkCoord chunk, WorldRandomDomain domain)
        {
            state = Mix((uint)seed);
            state = Mix(state ^ domain.GetHashCode());
            state = Mix(state ^ unchecked((uint)chunk.X));
            state = Mix(state ^ unchecked((uint)chunk.Y));

            if (state == 0u)
                state = 0x6D2B79F5u;
        }

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));

            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public float NextFloat01()
        {
            return (NextUInt() & 0x00FFFFFFu) / 16777216f;
        }

        public bool Chance(float probability)
        {
            if (probability <= 0f)
                return false;
            if (probability >= 1f)
                return true;
            return NextFloat01() < probability;
        }

        private static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }
    }

    /// <summary>
    /// Creates independent deterministic random streams for a world, subsystem, and chunk.
    /// </summary>
    public sealed class WorldRandomService
    {
        private readonly int seed;

        public WorldRandomService(int seed)
        {
            this.seed = seed;
        }

        public IWorldRandom Create(ChunkCoord chunk, WorldRandomDomain domain)
        {
            return new DeterministicWorldRandom(seed, chunk, domain);
        }
    }
}
