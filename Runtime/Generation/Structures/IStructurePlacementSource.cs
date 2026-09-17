using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Supplies world-space structure placements that intersect the requested chunk.
    /// Implementations are deterministic for a fixed world seed and structure catalog.
    /// </summary>
    public interface IStructurePlacementSource
    {
        void Collect(WorldGenerationContext context, StructurePlacementSet output);
    }

    /// <summary>
    /// Compatibility adapter over the generic world-feature placement source.
    /// Structure definitions provide the placement rules; the structure catalog remains
    /// the authoritative owner of structure content.
    /// </summary>
    public sealed class DeterministicStructurePlacementSource : IStructurePlacementSource
    {
        public void Collect(WorldGenerationContext context, StructurePlacementSet output)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            var featureSource = new DeterministicWorldFeaturePlacementSource(
                context.Structures.Definitions,
                WorldRandomDomain.Structures);
            var featurePlacements = new WorldFeaturePlacementSet();
            featureSource.Collect(context, featurePlacements);

            foreach (WorldFeaturePlacement placement in featurePlacements.Placements)
            {
                if (!context.Structures.TryGet(new StructureId((byte)placement.FeatureId), out StructureDefinition definition))
                    continue;

                output.Add(new StructurePlacement(definition, placement));
            }
        }
    }
}
