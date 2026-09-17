using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPlanRuntimeIntegrationTests
    {
        private sealed class FeatureDefinition : IWorldFeaturePlacementDefinition
        {
            public int FeatureId { get; }
            public int Width { get; }
            public int Height { get; }
            public float SpawnChance => 1f;
            public int MaxPerChunk => 1;
            public int MinimumDistanceFromOrigin => 0;

            public FeatureDefinition(int featureId, int width, int height)
            {
                FeatureId = featureId;
                Width = width;
                Height = height;
            }
        }

        private sealed class FeatureResolver : IWorldPlanFeatureResolver
        {
            private readonly IWorldFeaturePlacementDefinition feature;

            public FeatureResolver(IWorldFeaturePlacementDefinition feature)
            {
                this.feature = feature;
            }

            public IWorldFeaturePlacementDefinition Resolve(WorldPlanNode node)
            {
                return node.TypeId == "room" ? feature : null;
            }
        }

        private sealed class RealizationSource : IWorldPlanRealizationSource
        {
            public IEnumerable<WorldRealizationEdit> CreateEdits(WorldPlanFeaturePlacement placement)
            {
                yield return new WorldRealizationEdit(
                    "edit/" + placement.NodeId,
                    placement.NodeId,
                    "place-feature",
                    placement.Placement.FeatureId.ToString(),
                    10,
                    placement.Placement.Anchor);
            }
        }

        private sealed class ObservePlanPass : IWorldGenerationPass
        {
            public int Order => 0;
            public WorldPlanRuntime Received { get; private set; }

            public void Execute(WorldGenerationContext context)
            {
                Received = context.WorldPlan;
            }
        }

        private static WorldPlanGraphDefinition CreatePlan()
        {
            var roomType = new WorldPlanNodeTypeDefinition(
                "room",
                "Room",
                "Dungeon",
                6,
                6,
                1,
                new WorldPlanPortDefinition[0]);

            return new WorldPlanGraphDefinition(
                new[] { roomType },
                new[]
                {
                    new WorldPlanNodeDefinition("room_a", "room", "Room A")
                },
                Array.Empty<WorldPlanConnectionDefinition>());
        }

        [Test]
        public void RuntimeBuildsPlanAndLayoutWithoutFeatureBindings()
        {
            WorldPlanRuntime runtime = new WorldPlanRuntimeBuilder().Build(1234, CreatePlan());

            Assert.IsTrue(runtime.Succeeded);
            Assert.IsNotNull(runtime.Plan);
            Assert.IsNotNull(runtime.Layout);
            Assert.AreEqual(1, runtime.Layout.Nodes.Count);
            Assert.AreEqual(0, runtime.Realization.Count);
        }

        [Test]
        public void RuntimeLowersFeaturesAndIndexesRealizationByConfiguredChunkSize()
        {
            var feature = new FeatureDefinition(7, 2, 2);
            var settings = new WorldPlanRuntimeSettings(
                chunkSize: 16,
                featureResolver: new FeatureResolver(feature),
                realizationSource: new RealizationSource(),
                loweringSettings: new WorldPlanFeatureLoweringSettings(16, 0, 1));

            WorldPlanRuntime runtime = new WorldPlanRuntimeBuilder().Build(1234, CreatePlan(), settings);

            Assert.IsTrue(runtime.Succeeded);
            Assert.AreEqual(16, runtime.ChunkSize);
            Assert.AreEqual(1, runtime.LoweringResult.Placements.Count);
            Assert.AreEqual(1, runtime.Realization.Count);

            var edits = new List<WorldRealizationEdit>();
            runtime.CollectChunkRealizationEdits(new ChunkCoord(0, 0), edits);
            Assert.AreEqual(1, edits.Count);
            Assert.AreEqual("place-feature", edits[0].Kind);
            Assert.AreEqual(7, int.Parse(edits[0].Value));
        }

        [Test]
        public void MismatchedLoweringChunkSizeIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new WorldPlanRuntimeSettings(
                chunkSize: 16,
                loweringSettings: new WorldPlanFeatureLoweringSettings(64)));
        }

        [Test]
        public void GeneratorPassesTheSameWorldPlanRuntimeIntoChunkContext()
        {
            WorldPlanRuntime runtime = new WorldPlanRuntimeBuilder().Build(
                1234,
                CreatePlan(),
                new WorldPlanRuntimeSettings(chunkSize: 4));
            var observer = new ObservePlanPass();
            var pipeline = new WorldGenerationPipeline().Add(observer);
            var generator = new ProceduralWorldGenerator(
                1234,
                new WorldGenerationSettings { chunkSize = 4 },
                pipeline,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                runtime);

            generator.GenerateChunk(new ChunkCoord(0, 0));

            Assert.AreSame(runtime, generator.WorldPlan);
            Assert.AreSame(runtime, observer.Received);
        }

        [Test]
        public void GeneratorRejectsWorldPlanWithDifferentChunkSize()
        {
            WorldPlanRuntime runtime = new WorldPlanRuntimeBuilder().Build(
                1234,
                CreatePlan(),
                new WorldPlanRuntimeSettings(chunkSize: 16));

            Assert.Throws<ArgumentException>(() => new ProceduralWorldGenerator(
                1234,
                new WorldGenerationSettings { chunkSize = 4 },
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                runtime));
        }

        [Test]
        public void PlanChunkHelperReportsUnsupportedGeneratorsWithoutThrowing()
        {
            var generated = new StubGenerator();
            var output = new List<WorldRealizationEdit>();

            Assert.IsFalse(WorldPlanChunkGeneration.TryCollectRealizationEdits(
                generated,
                new ChunkCoord(0, 0),
                output));
            Assert.AreEqual(0, output.Count);
        }

        private sealed class StubGenerator : IWorldChunkGenerator
        {
            public GeneratedChunk GenerateChunk(ChunkCoord coordinate)
            {
                return new GeneratedChunk(coordinate, 1);
            }
        }
    }
}
