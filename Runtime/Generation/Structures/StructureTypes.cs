using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Data-only description of a structure that may be placed in generated terrain.
    /// </summary>
    public sealed class StructureDefinition
    {
        public StructureId Id { get; }
        public string Name { get; }
        public RegionId Region { get; }
        public TerrainId Terrain { get; }
        public float SpawnChance { get; }
        public byte MaxPerChunk { get; }
        public int MinimumDistanceFromOrigin { get; }
        public int Width { get; }
        public int Height { get; }

        public StructureDefinition(
            StructureId id,
            string name,
            RegionId region,
            TerrainId terrain,
            float spawnChance,
            byte maxPerChunk,
            int width = 1,
            int height = 1,
            int minimumDistanceFromOrigin = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Structure name cannot be empty.", nameof(name));
            if (spawnChance < 0f || spawnChance > 1f)
                throw new ArgumentOutOfRangeException(nameof(spawnChance));
            if (maxPerChunk == 0)
                throw new ArgumentOutOfRangeException(nameof(maxPerChunk));
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (minimumDistanceFromOrigin < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumDistanceFromOrigin));

            Id = id;
            Name = name;
            Region = region;
            Terrain = terrain;
            SpawnChance = spawnChance;
            MaxPerChunk = maxPerChunk;
            Width = width;
            Height = height;
            MinimumDistanceFromOrigin = minimumDistanceFromOrigin;
        }
    }

    /// <summary>Immutable lookup table for structure definitions.</summary>
    public sealed class StructureCatalog
    {
        private readonly Dictionary<StructureId, StructureDefinition> definitions;

        public IReadOnlyCollection<StructureDefinition> Definitions => definitions.Values;

        public StructureCatalog(IEnumerable<StructureDefinition> definitions)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            this.definitions = new Dictionary<StructureId, StructureDefinition>();
            foreach (StructureDefinition definition in definitions)
            {
                if (definition == null)
                    throw new ArgumentException("Structure definitions cannot contain null entries.", nameof(definitions));
                if (this.definitions.ContainsKey(definition.Id))
                    throw new ArgumentException($"Duplicate structure ID: {definition.Id}.", nameof(definitions));

                this.definitions.Add(definition.Id, definition);
            }

            if (this.definitions.Count == 0)
                throw new ArgumentException("Structure catalog cannot be empty.", nameof(definitions));
        }

        public bool TryGet(StructureId id, out StructureDefinition definition)
        {
            return definitions.TryGetValue(id, out definition);
        }

        public StructureDefinition Get(StructureId id)
        {
            if (!definitions.TryGetValue(id, out StructureDefinition definition))
                throw new KeyNotFoundException($"Structure ID {id} is not defined in this catalog.");

            return definition;
        }

        public static StructureCatalog CreateDefault()
        {
            return new StructureCatalog(new[]
            {
                new StructureDefinition(
                    new StructureId(1),
                    "Small Ruin",
                    new RegionId(2),
                    new TerrainId(2),
                    0.0125f,
                    1,
                    3,
                    3,
                    45),
                new StructureDefinition(
                    new StructureId(2),
                    "Crystal Shrine",
                    new RegionId(1),
                    new TerrainId(3),
                    0.006f,
                    1,
                    5,
                    5,
                    80)
            });
        }
    }
}
