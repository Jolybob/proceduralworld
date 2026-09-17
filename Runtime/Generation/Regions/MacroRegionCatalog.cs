using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Immutable collection of macro-region layout rules.
    /// </summary>
    public sealed class MacroRegionCatalog
    {
        private readonly List<MacroRegionDefinition> definitions;

        public IReadOnlyList<MacroRegionDefinition> Definitions => definitions;

        public MacroRegionCatalog(IEnumerable<MacroRegionDefinition> definitions)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            this.definitions = new List<MacroRegionDefinition>();
            foreach (MacroRegionDefinition definition in definitions)
            {
                if (definition == null)
                    throw new ArgumentException("Macro-region definitions cannot contain null entries.", nameof(definitions));

                this.definitions.Add(definition);
            }

            if (this.definitions.Count == 0)
                throw new ArgumentException("Macro-region catalog cannot be empty.", nameof(definitions));
        }
    }
}
