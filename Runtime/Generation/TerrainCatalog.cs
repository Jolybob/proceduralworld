using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Stable, data-only description of generated terrain.
    /// </summary>
    public sealed class TerrainDefinition
    {
        public TerrainId Id { get; }
        public string Name { get; }
        public WorldTile Tile { get; }
        public string Description { get; }

        public TerrainDefinition(
            TerrainId id,
            string name,
            WorldTile tile,
            string description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Terrain name cannot be empty.", nameof(name));

            Id = id;
            Name = name;
            Tile = tile;
            Description = description ?? string.Empty;
        }
    }

    /// <summary>
    /// Immutable lookup table for terrain definitions.
    /// </summary>
    public sealed class TerrainCatalog
    {
        private readonly Dictionary<TerrainId, TerrainDefinition> definitions;

        public IReadOnlyCollection<TerrainDefinition> Definitions => definitions.Values;

        public TerrainCatalog(IEnumerable<TerrainDefinition> definitions)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            this.definitions = new Dictionary<TerrainId, TerrainDefinition>();
            foreach (TerrainDefinition definition in definitions)
            {
                if (definition == null)
                    throw new ArgumentException("Terrain definitions cannot contain null entries.", nameof(definitions));

                if (this.definitions.ContainsKey(definition.Id))
                    throw new ArgumentException($"Duplicate terrain ID: {definition.Id}.", nameof(definitions));

                this.definitions.Add(definition.Id, definition);
            }

            if (this.definitions.Count == 0)
                throw new ArgumentException("Terrain catalog cannot be empty.", nameof(definitions));
        }

        public bool TryGet(TerrainId id, out TerrainDefinition definition)
        {
            return definitions.TryGetValue(id, out definition);
        }

        public TerrainDefinition Get(TerrainId id)
        {
            if (!definitions.TryGetValue(id, out TerrainDefinition definition))
                throw new KeyNotFoundException($"Terrain ID {id} is not defined in this catalog.");

            return definition;
        }

        public static TerrainCatalog CreateDefault()
        {
            return new TerrainCatalog(new[]
            {
                new TerrainDefinition(new TerrainId(0), "Empty", WorldTile.Empty),
                new TerrainDefinition(new TerrainId(1), "Deep", WorldTile.Deep),
                new TerrainDefinition(new TerrainId(2), "Mid", WorldTile.Mid),
                new TerrainDefinition(new TerrainId(3), "Inner", WorldTile.Inner),
                new TerrainDefinition(new TerrainId(4), "Core", WorldTile.Core)
            });
        }
    }
}
