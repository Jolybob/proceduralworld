using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Data-only description of a resource that may spawn in generated terrain.
    /// Resource deposits can optionally grow into deterministic clusters.
    /// </summary>
    public sealed class ResourceDefinition
    {
        public ResourceId Id { get; }
        public string Name { get; }
        public RegionId Region { get; }
        public TerrainId Terrain { get; }
        public float SpawnChance { get; }
        public byte MaxPerChunk { get; }
        public int MinimumDistanceFromOrigin { get; }
        public byte DepositSizeMin { get; }
        public byte DepositSizeMax { get; }
        public float DepositGrowthChance { get; }

        public ResourceDefinition(
            ResourceId id,
            string name,
            RegionId region,
            TerrainId terrain,
            float spawnChance,
            byte maxPerChunk,
            int minimumDistanceFromOrigin = 0,
            byte depositSizeMin = 1,
            byte depositSizeMax = 1,
            float depositGrowthChance = 0f)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Resource name cannot be empty.", nameof(name));
            if (spawnChance < 0f || spawnChance > 1f)
                throw new ArgumentOutOfRangeException(nameof(spawnChance));
            if (maxPerChunk == 0)
                throw new ArgumentOutOfRangeException(nameof(maxPerChunk));
            if (minimumDistanceFromOrigin < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumDistanceFromOrigin));
            if (depositSizeMin == 0)
                throw new ArgumentOutOfRangeException(nameof(depositSizeMin));
            if (depositSizeMax < depositSizeMin)
                throw new ArgumentOutOfRangeException(nameof(depositSizeMax));
            if (depositGrowthChance < 0f || depositGrowthChance > 1f)
                throw new ArgumentOutOfRangeException(nameof(depositGrowthChance));

            Id = id;
            Name = name;
            Region = region;
            Terrain = terrain;
            SpawnChance = spawnChance;
            MaxPerChunk = maxPerChunk;
            MinimumDistanceFromOrigin = minimumDistanceFromOrigin;
            DepositSizeMin = depositSizeMin;
            DepositSizeMax = depositSizeMax;
            DepositGrowthChance = depositGrowthChance;
        }
    }

    /// <summary>Immutable lookup table for resource definitions.</summary>
    public sealed class ResourceCatalog
    {
        private readonly Dictionary<ResourceId, ResourceDefinition> definitions;

        public IReadOnlyCollection<ResourceDefinition> Definitions => definitions.Values;

        public ResourceCatalog(IEnumerable<ResourceDefinition> definitions)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            this.definitions = new Dictionary<ResourceId, ResourceDefinition>();
            foreach (ResourceDefinition definition in definitions)
            {
                if (definition == null)
                    throw new ArgumentException("Resource definitions cannot contain null entries.", nameof(definitions));
                if (this.definitions.ContainsKey(definition.Id))
                    throw new ArgumentException($"Duplicate resource ID: {definition.Id}.", nameof(definitions));

                this.definitions.Add(definition.Id, definition);
            }

            if (this.definitions.Count == 0)
                throw new ArgumentException("Resource catalog cannot be empty.", nameof(definitions));
        }

        public bool TryGet(ResourceId id, out ResourceDefinition definition)
        {
            return definitions.TryGetValue(id, out definition);
        }

        public ResourceDefinition Get(ResourceId id)
        {
            if (!definitions.TryGetValue(id, out ResourceDefinition definition))
                throw new KeyNotFoundException($"Resource ID {id} is not defined in the catalog.");

            return definition;
        }

        public static ResourceCatalog CreateDefault()
        {
            return new ResourceCatalog(new[]
            {
                new ResourceDefinition(
                    new ResourceId(1),
                    "Crystal",
                    new RegionId(1),
                    new TerrainId(3),
                    0.025f,
                    6,
                    20,
                    2,
                    6,
                    0.65f),
                new ResourceDefinition(
                    new ResourceId(2),
                    "Ore",
                    new RegionId(2),
                    new TerrainId(2),
                    0.04f,
                    8,
                    35,
                    2,
                    5,
                    0.6f),
                new ResourceDefinition(
                    new ResourceId(3),
                    "Rare Ore",
                    new RegionId(3),
                    new TerrainId(1),
                    0.0125f,
                    4,
                    60,
                    1,
                    3,
                    0.55f)
            });
        }
    }
}
