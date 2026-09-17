using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// A deterministic modification stage that runs after the primary generation passes.
    /// </summary>
    public interface IWorldPostProcessStep
    {
        int Order { get; }
        uint Salt { get; }
        void Execute(WorldPostProcessContext context);
    }

    /// <summary>
    /// Context exposed to post-process steps without making them depend on rendering systems.
    /// Each step receives an isolated deterministic random stream derived from its salt.
    /// </summary>
    public sealed class WorldPostProcessContext
    {
        private readonly IWorldRandom random;

        public WorldGenerationContext Generation { get; }
        public GeneratedChunk Chunk => Generation.Chunk;
        public int Seed => Generation.Seed;
        public WorldGenerationSettings Settings => Generation.Settings;
        public ChunkCoord ChunkCoordinate => Generation.ChunkCoordinate;
        public IWorldRandom Random => random;

        internal WorldPostProcessContext(WorldGenerationContext generation, uint salt)
        {
            Generation = generation ?? throw new ArgumentNullException(nameof(generation));
            random = generation.Random.Create(
                generation.ChunkCoordinate,
                WorldRandomDomain.PostProcess,
                salt);
        }

        public GeneratedCell GetCell(int x, int y)
        {
            return Chunk.GetCell(x, y);
        }

        public void SetCell(int x, int y, GeneratedCell cell)
        {
            Chunk.SetCell(x, y, cell);
        }

        public int GetWorldX(int localX)
        {
            return ChunkCoordinate.X * Chunk.Size + localX;
        }

        public int GetWorldY(int localY)
        {
            return ChunkCoordinate.Y * Chunk.Size + localY;
        }
    }

    /// <summary>
    /// Ordered collection of post-process steps.
    /// </summary>
    public sealed class WorldPostProcessPipeline
    {
        private readonly List<IWorldPostProcessStep> steps = new List<IWorldPostProcessStep>();

        public IReadOnlyList<IWorldPostProcessStep> Steps => steps;

        public WorldPostProcessPipeline Add(IWorldPostProcessStep step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));

            steps.Add(step);
            steps.Sort((a, b) => a.Order.CompareTo(b.Order));
            return this;
        }

        internal void Execute(WorldGenerationContext generationContext)
        {
            if (generationContext == null)
                throw new ArgumentNullException(nameof(generationContext));

            for (int i = 0; i < steps.Count; i++)
            {
                IWorldPostProcessStep step = steps[i];
                step.Execute(new WorldPostProcessContext(generationContext, step.Salt));
            }
        }
    }

    /// <summary>
    /// Adapts the post-process pipeline into the main ordered generation pipeline.
    /// </summary>
    public sealed class WorldPostProcessPass : IWorldGenerationPass
    {
        public int Order => 900;

        private readonly WorldPostProcessPipeline pipeline;

        public WorldPostProcessPass(WorldPostProcessPipeline pipeline)
        {
            this.pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            pipeline.Execute(context);
        }
    }
}
