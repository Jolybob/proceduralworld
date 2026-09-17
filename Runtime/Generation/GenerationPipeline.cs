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
        public IEnvironmentFieldProvider EnvironmentFields { get; }
        public ICaveFieldProvider CaveFields { get; }
        public WorldRandomService Random { get; }
        public ResourceCatalog Resources { get; }
        public StructureCatalog Structures { get; }
        public TopologyPipeline Topology { get; }

        public WorldGenerationContext(
            int seed,
            WorldGenerationSettings settings,
            ChunkCoord chunkCoordinate,
            GeneratedChunk chunk,
            INoiseField noise)
            : this(seed, settings, chunkCoordinate, chunk, noise,
                new DefaultEnvironmentFieldProvider(seed, settings, noise),
                new DefaultCaveFieldProvider(seed, settings),
                new WorldRandomService(seed),
                ResourceCatalog.CreateDefault(),
                StructureCatalog.CreateDefault())
        {
        }

        public WorldGenerationContext(
            int seed,
            WorldGenerationSettings settings,
            ChunkCoord chunkCoordinate,
            GeneratedChunk chunk,
            INoiseField noise,
            IEnvironmentFieldProvider environmentFields)
            : this(seed, settings, chunkCoordinate, chunk, noise,
                environmentFields,
                new DefaultCaveFieldProvider(seed, settings),
                new WorldRandomService(seed),
                ResourceCatalog.CreateDefault(),
                StructureCatalog.CreateDefault())
        {
        }

        public WorldGenerationContext(
            int seed,
            WorldGenerationSettings settings,
            ChunkCoord chunkCoordinate,
            GeneratedChunk chunk,
            INoiseField noise,
            IEnvironmentFieldProvider environmentFields,
            ICaveFieldProvider caveFields)
            : this(seed, settings, chunkCoordinate, chunk, noise,
                environmentFields,
                caveFields,
                new WorldRandomService(seed),
                ResourceCatalog.CreateDefault(),
                StructureCatalog.CreateDefault())
        {
        }

        public WorldGenerationContext(
            int seed,
            WorldGenerationSettings settings,
            ChunkCoord chunkCoordinate,
            GeneratedChunk chunk,
            INoiseField noise,
            IEnvironmentFieldProvider environmentFields,
            ICaveFieldProvider caveFields,
            WorldRandomService random)
            : this(seed, settings, chunkCoordinate, chunk, noise,
                environmentFields,
                caveFields,
                random,
                ResourceCatalog.CreateDefault(),
                StructureCatalog.CreateDefault())
        {
        }

        public WorldGenerationContext(
            int seed,
            WorldGenerationSettings settings,
            ChunkCoord chunkCoordinate,
            GeneratedChunk chunk,
            INoiseField noise,
            IEnvironmentFieldProvider environmentFields,
            ICaveFieldProvider caveFields,
            WorldRandomService random,
            ResourceCatalog resources)
            : this(seed, settings, chunkCoordinate, chunk, noise,
                environmentFields,
                caveFields,
                random,
                resources,
                StructureCatalog.CreateDefault())
        {
        }

        public WorldGenerationContext(
            int seed,
            WorldGenerationSettings settings,
            ChunkCoord chunkCoordinate,
            GeneratedChunk chunk,
            INoiseField noise,
            IEnvironmentFieldProvider environmentFields,
            ICaveFieldProvider caveFields,
            WorldRandomService random,
            ResourceCatalog resources,
            StructureCatalog structures,
            TopologyPipeline topology = null)
        {
            Seed = seed;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            ChunkCoordinate = chunkCoordinate;
            Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
            Noise = noise ?? throw new ArgumentNullException(nameof(noise));
            EnvironmentFields = environmentFields ?? throw new ArgumentNullException(nameof(environmentFields));
            CaveFields = caveFields ?? throw new ArgumentNullException(nameof(caveFields));
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Resources = resources ?? throw new ArgumentNullException(nameof(resources));
            Structures = structures ?? throw new ArgumentNullException(nameof(structures));
            Topology = topology ?? new TopologyPipeline();
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
