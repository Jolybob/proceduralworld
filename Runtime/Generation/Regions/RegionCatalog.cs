using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Stable, data-only description of a generated region.
    /// Region IDs are part of generated data and should not be reassigned after a world ships.
    /// </summary>
    public sealed class RegionDefinition
    {
        public RegionId Id { get; }
        public string Name { get; }
        public string Description { get; }
        public TerrainId DefaultTerrain { get; }

        public RegionDefinition(
            RegionId id,
            string name,
            TerrainId defaultTerrain,
            string description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Region name cannot be empty.", nameof(name));

            Id = id;
            Name = name;
            Description = description ?? string.Empty;
            DefaultTerrain = defaultTerrain;
        }
    }

    /// <summary>
    /// Immutable lookup table for region definitions.
    /// </summary>
    public sealed class RegionCatalog
    {
        private readonly Dictionary<RegionId, RegionDefinition> definitions;

        public IReadOnlyCollection<RegionDefinition> Definitions => definitions.Values;

        public RegionCatalog(IEnumerable<RegionDefinition> definitions)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            this.definitions = new Dictionary<RegionId, RegionDefinition>();
            foreach (RegionDefinition definition in definitions)
            {
                if (definition == null)
                    throw new ArgumentException("Region definitions cannot contain null entries.", nameof(definitions));

                if (this.definitions.ContainsKey(definition.Id))
                    throw new ArgumentException($"Duplicate region ID: {definition.Id}.", nameof(definitions));

                this.definitions.Add(definition.Id, definition);
            }

            if (this.definitions.Count == 0)
                throw new ArgumentException("Region catalog cannot be empty.", nameof(definitions));
        }

        public bool TryGet(RegionId id, out RegionDefinition definition)
        {
            return definitions.TryGetValue(id, out definition);
        }

        public RegionDefinition Get(RegionId id)
        {
            if (!definitions.TryGetValue(id, out RegionDefinition definition))
                throw new KeyNotFoundException($"Region ID {id} is not defined in this catalog.");

            return definition;
        }

        public static RegionCatalog CreateDefault()
        {
            return new RegionCatalog(new[]
            {
                new RegionDefinition(
                    new RegionId(0),
                    "Core",
                    new TerrainId(4),
                    "Central region."),
                new RegionDefinition(
                    new RegionId(1),
                    "Cold Wet",
                    new TerrainId(3),
                    "Cold and moisture-rich region."),
                new RegionDefinition(
                    new RegionId(2),
                    "Cold Dry",
                    new TerrainId(2),
                    "Cold and dry region."),
                new RegionDefinition(
                    new RegionId(3),
                    "Warm Wet",
                    new TerrainId(1),
                    "Warm and moisture-rich region."),
                new RegionDefinition(
                    new RegionId(4),
                    "Warm Dry",
                    new TerrainId(2),
                    "Warm and dry region.")
            });
        }
    }
}
