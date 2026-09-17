using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Immutable collection of macro-region layout definitions.
    /// </summary>
    public sealed class MacroRegionCatalog
    {
        private readonly IReadOnlyList<MacroRegionDefinition> definitions;

        public IReadOnlyList<MacroRegionDefinition> Definitions => definitions;

        public MacroRegionCatalog(IEnumerable<MacroRegionDefinition> definitions)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            var copy = new List<MacroRegionDefinition>();
            foreach (MacroRegionDefinition definition in definitions)
            {
                if (definition == null)
                    throw new ArgumentException("Macro region definitions cannot contain null entries.", nameof(definitions));
                copy.Add(definition);
            }

            if (copy.Count == 0)
                throw new ArgumentException("Macro region catalog cannot be empty.", nameof(definitions));

            this.definitions = copy.AsReadOnly();
        }

        public static MacroRegionCatalog CreateDefault()
        {
            return new MacroRegionCatalog(new[]
            {
                new MacroRegionDefinition(
                    new RegionId(1),
                    48f,
                    210f,
                    0f,
                    MathF.PI / 2f,
                    boundaryWarp: 14f,
                    boundaryNoiseScale: 0.012f,
                    angularWarp: 0.18f,
                    priority: 10),
                new MacroRegionDefinition(
                    new RegionId(2),
                    48f,
                    210f,
                    MathF.PI / 2f,
                    MathF.PI / 2f,
                    boundaryWarp: 14f,
                    boundaryNoiseScale: 0.012f,
                    angularWarp: 0.18f,
                    priority: 10),
                new MacroRegionDefinition(
                    new RegionId(3),
                    48f,
                    210f,
                    MathF.PI,
                    MathF.PI / 2f,
                    boundaryWarp: 14f,
                    boundaryNoiseScale: 0.012f,
                    angularWarp: 0.18f,
                    priority: 10),
                new MacroRegionDefinition(
                    new RegionId(4),
                    48f,
                    210f,
                    MathF.PI * 1.5f,
                    MathF.PI / 2f,
                    boundaryWarp: 14f,
                    boundaryNoiseScale: 0.012f,
                    angularWarp: 0.18f,
                    priority: 10)
            });
        }
    }
}
