using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>Stable identifier for generated resource types.</summary>
    public readonly struct ResourceId : IEquatable<ResourceId>
    {
        public readonly byte Value;

        public ResourceId(byte value) => Value = value;

        public bool Equals(ResourceId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ResourceId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(ResourceId left, ResourceId right) => left.Equals(right);
        public static bool operator !=(ResourceId left, ResourceId right) => !left.Equals(right);
        public override string ToString() => Value.ToString();
    }

    /// <summary>
    /// Data-only description of a resource that may spawn in generated terrain.
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

        public ResourceDefinition(
            ResourceId id,
            string name,
            RegionId region,
            TerrainId terrain,
            float spawnChance,
            byte maxPerChunk,
            int minimumDistanceFromOrigin = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Resource name cannot be empty.", nameof(name));
            if (spawnChance < 0f || spawnChance > 1f)
                throw new ArgumentOutOfRangeException(nameof(spawnChance));
            if (maxPerChunk == 0)
                throw new ArgumentOutOfRangeException(nameof(maxPerChunk));
            if (minimumDistanceFromOrigin < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumDistanceFromOrigin));

            Id = id;
            Name = name;
            Region = region;
            Terrain = terrain;
            SpawnChance = spawnChance;
            MaxPerChunk = maxPerChunk;
            MinimumDistanceFromOrigin = minimumDistanceFromOrigin;
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
                throw new KeyNotFoundException($"Resource ID {id} is not defined in this catalog.");

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
                    20),
                new ResourceDefinition(
                    new ResourceId(2),
                    "Ore",
                    new RegionId(2),
                    new TerrainId(2),
                    0.04f,
                    8,
                    35),
                new ResourceDefinition(
                    new ResourceId(3),
                    "Rare Ore",
                    new RegionId(3),
                    new TerrainId(1),
                    0.0125f,
                    4,
                    60)
            });
        }
    }
}
