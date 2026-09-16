using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public interface IWorldGenerationPass
    {
        int Order { get; }
        void Execute(WorldGenerationContext context);
    }

    public sealed class WorldGenerationContext
    {
        public int Seed { get; }
        public WorldGenerationSettings Settings { get; }
        public ChunkCoord ChunkCoordinate { get; }
        public GeneratedChunk Chunk { get; }
        public INoiseField Noise { get; }

        public WorldGenerationContext(
            int seed,
            WorldGenerationSettings settings,
            ChunkCoord chunkCoordinate,
            GeneratedChunk chunk,
            INoiseField noise)
        {
            Seed = seed;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            ChunkCoordinate = chunkCoordinate;
            Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
            Noise = noise ?? throw new ArgumentNullException(nameof(noise));
        }
    }

    public sealed class WorldGenerationPipeline
    {
        private readonly List<IWorldGenerationPass> passes = new List<IWorldGenerationPass>();

        public IReadOnlyList<IWorldGenerationPass> Passes => passes;

        public WorldGenerationPipeline Add(IWorldGenerationPass pass)
        {
            if (pass == null) throw new ArgumentNullException(nameof(pass));
            passes.Add(pass);
            passes.Sort((a, b) => a.Order.CompareTo(b.Order));
            return this;
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            for (int i = 0; i < passes.Count; i++)
                passes[i].Execute(context);
        }
    }
}
