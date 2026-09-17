using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Composes topology modifiers independently from the main generation pipeline.
    /// Topology modifiers operate on canonical generated cell topology and can be ordered
    /// without coupling the main pipeline to individual topology features.
    /// </summary>
    public sealed class TopologyPipeline
    {
        private readonly List<ITopologyModifier> modifiers = new List<ITopologyModifier>();

        public IReadOnlyList<ITopologyModifier> Modifiers => modifiers;

        public TopologyPipeline Add(ITopologyModifier modifier)
        {
            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            modifiers.Add(modifier);
            modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
            return this;
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            for (int i = 0; i < modifiers.Count; i++)
                modifiers[i].Execute(context);
        }
    }

    /// <summary>
    /// Inserts the independent topology pipeline into the canonical world-generation order.
    /// </summary>
    public sealed class TopologyPass : IWorldGenerationPass
    {
        public int Order => 350;

        private readonly TopologyPipeline topology;

        public TopologyPass(TopologyPipeline topology)
        {
            this.topology = topology ?? throw new ArgumentNullException(nameof(topology));
        }

        public void Execute(WorldGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            topology.Execute(context);
        }
    }
}
