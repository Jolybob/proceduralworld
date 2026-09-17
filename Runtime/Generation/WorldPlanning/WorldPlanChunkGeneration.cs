using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Optional capability implemented by chunk generators that have a deterministic world-plan
    /// program. Chunk generation remains field/terrain focused; plan realization is queried through
    /// this explicit boundary so materializers can consume world-space truth without rebuilding a
    /// graph inside each chunk.
    /// </summary>
    public interface IWorldPlanChunkGenerator : IWorldChunkGenerator
    {
        WorldPlanRuntime WorldPlan { get; }

        void CollectWorldPlanRealizationEdits(
            ChunkCoord coordinate,
            List<WorldRealizationEdit> output);
    }

    public static class WorldPlanChunkGeneration
    {
        public static bool TryCollectRealizationEdits(
            IWorldChunkGenerator generator,
            ChunkCoord coordinate,
            List<WorldRealizationEdit> output)
        {
            if (generator == null)
                throw new ArgumentNullException(nameof(generator));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            IWorldPlanChunkGenerator planned = generator as IWorldPlanChunkGenerator;
            if (planned == null || planned.WorldPlan == null)
                return false;

            planned.CollectWorldPlanRealizationEdits(coordinate, output);
            return true;
        }
    }
}
