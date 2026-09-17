using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class TopologyPipelineTests
    {
        [Test]
        public void ModifiersAreSortedByOrder()
        {
            var pipeline = new TopologyPipeline()
                .Add(new RecordingModifier(30, "late"))
                .Add(new RecordingModifier(10, "early"))
                .Add(new RecordingModifier(20, "middle"));

            Assert.AreEqual(3, pipeline.Modifiers.Count);
            Assert.AreEqual(10, pipeline.Modifiers[0].Order);
            Assert.AreEqual(20, pipeline.Modifiers[1].Order);
            Assert.AreEqual(30, pipeline.Modifiers[2].Order);
        }

        [Test]
        public void ModifiersExecuteInStableOrder()
        {
            var calls = new List<string>();
            var pipeline = new TopologyPipeline()
                .Add(new RecordingModifier(20, "middle", calls))
                .Add(new RecordingModifier(10, "early", calls))
                .Add(new RecordingModifier(30, "late", calls));

            var settings = new WorldGenerationSettings { chunkSize = 1 };
            var chunk = new GeneratedChunk(new ChunkCoord(0, 0), 1);
            var context = new WorldGenerationContext(
                1,
                settings,
                new ChunkCoord(0, 0),
                chunk,
                new SeededPerlinNoiseField(1, 0.02f));

            pipeline.Execute(context);

            CollectionAssert.AreEqual(new[] { "early", "middle", "late" }, calls);
        }

        private sealed class RecordingModifier : ITopologyModifier
        {
            private readonly string name;
            private readonly IList<string> calls;

            public int Order { get; }

            public RecordingModifier(int order, string name, IList<string> calls = null)
            {
                Order = order;
                this.name = name;
                this.calls = calls;
            }

            public void Execute(WorldGenerationContext context)
            {
                if (calls != null)
                    calls.Add(name);
            }
        }
    }
}
