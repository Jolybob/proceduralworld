using System;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Compatibility adapter that plans structures through the generic world-feature
    /// placement kernel while preserving the existing structure-specific API.
    /// </summary>
    public sealed class StructurePlacementPlanner
    {
        private readonly WorldFeaturePlacementPlanner featurePlanner;

        public StructurePlacementPlanner(int seed)
        {
            featurePlanner = new WorldFeaturePlacementPlanner(seed);
        }

        public StructurePlacement CreatePlacement(
            ChunkCoord ownerChunk,
            StructureDefinition definition,
            int anchorX,
            int anchorY,
            int chunkSize = 64)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            WorldFeaturePlacement placement = featurePlanner.CreatePlacement(
                ownerChunk,
                definition,
                anchorX,
                anchorY,
                chunkSize);

            return new StructurePlacement(definition, placement);
        }

        public int CollectOwnerChunk(
            ChunkCoord ownerChunk,
            StructureDefinition definition,
            int chunkSize,
            StructurePlacementSet output)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            var featurePlacements = new WorldFeaturePlacementSet();
            featurePlanner.CollectOwnerChunk(
                ownerChunk,
                definition,
                WorldRandomDomain.Structures,
                chunkSize,
                featurePlacements);

            int added = 0;
            foreach (WorldFeaturePlacement placement in featurePlacements.Placements)
            {
                if (output.Add(new StructurePlacement(definition, placement)))
                    added++;
            }

            return added;
        }
    }
}
