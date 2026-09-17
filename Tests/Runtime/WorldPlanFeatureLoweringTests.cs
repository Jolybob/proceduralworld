using NUnit.Framework;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPlanFeatureLoweringTests
    {
        private sealed class Feature : IWorldFeaturePlacementDefinition
        {
            public int FeatureId { get; }
            public int Width { get; }
            public int Height { get; }
            public float SpawnChance => 1f;
            public int MaxPerChunk => 1;
            public int MinimumDistanceFromOrigin => 0;

            public Feature(int featureId, int width = 3, int height = 2)
            {
                FeatureId = featureId;
                Width = width;
                Height = height;
            }
        }

        private sealed class Resolver : IWorldPlanFeatureResolver
        {
            private readonly IWorldFeaturePlacementDefinition feature;
            public Resolver(IWorldFeaturePlacementDefinition feature) { this.feature = feature; }
            public IWorldFeaturePlacementDefinition Resolve(WorldPlanNode node) => feature;
        }

        private sealed class ShiftedFeasibility : IWorldPlanPlacementFeasibility
        {
            public bool CanPlace(WorldPlanPlacementContext context) => context.Anchor.X >= 2;
        }

        [Test]
        public void LoweringProducesWorldFeaturePlacementFromLayout()
        {
            WorldPlanNodeTypeDefinition room = new WorldPlanNodeTypeDefinition("Room", "Room", "Test", 6, 4, 0, null);
            WorldPlanGraphDefinition graph = new WorldPlanGraphDefinition(
                new[] { room },
                new[] { new WorldPlanNodeDefinition("room", "Room", "Room") },
                new WorldPlanConnectionDefinition[0]);
            WorldPlan plan = new WorldPlanCompiler().Compile(1, graph).Plan;
            WorldPlanLayoutResult layout = new WorldPlanLayoutSolver().Solve(plan);

            WorldPlanFeatureLoweringResult result = new WorldPlanFeatureLowerer().Lower(
                plan, layout.Layout, new Resolver(new Feature(7)));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, result.Placements.Count);
            Assert.AreEqual("room", result.Placements[0].NodeId);
            Assert.AreEqual(7, result.Placements[0].Placement.FeatureId);
            Assert.AreEqual(0, result.Placements[0].Placement.OwnerChunk.X);
            Assert.AreEqual(0, result.Placements[0].Placement.OwnerChunk.Y);
        }

        [Test]
        public void FeasibilitySearchIsDeterministicAndMovesAnchor()
        {
            WorldPlanNodeTypeDefinition room = new WorldPlanNodeTypeDefinition("Room", "Room", "Test", 6, 4, 0, null);
            WorldPlanGraphDefinition graph = new WorldPlanGraphDefinition(
                new[] { room },
                new[] { new WorldPlanNodeDefinition("room", "Room", "Room") },
                new WorldPlanConnectionDefinition[0]);
            WorldPlan plan = new WorldPlanCompiler().Compile(1, graph).Plan;
            WorldPlanLayoutResult layout = new WorldPlanLayoutSolver().Solve(plan);
            var settings = new WorldPlanFeatureLoweringSettings(searchRadius: 3);
            WorldPlanFeatureLowerer lowerer = new WorldPlanFeatureLowerer();

            WorldPlanFeatureLoweringResult first = lowerer.Lower(plan, layout.Layout, new Resolver(new Feature(7)), new ShiftedFeasibility(), settings);
            WorldPlanFeatureLoweringResult second = lowerer.Lower(plan, layout.Layout, new Resolver(new Feature(7)), new ShiftedFeasibility(), settings);

            Assert.IsTrue(first.Succeeded);
            Assert.AreEqual(first.Placements.Count, second.Placements.Count);
            Assert.AreEqual(first.Placements[0], second.Placements[0]);
            Assert.AreEqual(2, first.Placements[0].Placement.Anchor.X);
        }

        [Test]
        public void LoweringUsesMathematicalFloorForNegativeOwnerChunks()
        {
            WorldPlanNodeTypeDefinition room = new WorldPlanNodeTypeDefinition("Room", "Room", "Test", 1, 1, 0, null);
            WorldPlanGraphDefinition graph = new WorldPlanGraphDefinition(
                new[] { room },
                new[] { new WorldPlanNodeDefinition("room", "Room", "Room") },
                new WorldPlanConnectionDefinition[0]);
            WorldPlan plan = new WorldPlanCompiler().Compile(1, graph).Plan;
            WorldPlanLayoutResult layout = new WorldPlanLayoutSolver().Solve(plan);
            var shifted = new NegativeAnchorFeasibility();

            WorldPlanFeatureLoweringResult result = new WorldPlanFeatureLowerer().Lower(
                plan, layout.Layout, new Resolver(new Feature(3, 1, 1)), shifted,
                new WorldPlanFeatureLoweringSettings(chunkSize: 64, searchRadius: 1));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(-1, result.Placements[0].Placement.OwnerChunk.X);
        }

        private sealed class NegativeAnchorFeasibility : IWorldPlanPlacementFeasibility
        {
            public bool CanPlace(WorldPlanPlacementContext context) => context.Anchor.X < 0;
        }
    }
}
