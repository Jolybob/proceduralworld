using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Backwards-compatible alias for the world-space structure placement architecture.
    /// New integrations should use <see cref="StructurePlacementPass"/> directly.
    /// </summary>
    public sealed class StructurePass : IWorldGenerationPass
    {
        private readonly StructurePlacementPass implementation;

        public int Order => implementation.Order;

        public StructurePass(
            StructureCatalog catalog,
            IStructurePlacementSource source = null)
        {
            implementation = new StructurePlacementPass(catalog, source);
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            implementation.Execute(context);
        }
    }
}
