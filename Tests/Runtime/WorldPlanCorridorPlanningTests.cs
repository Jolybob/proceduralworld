using NUnit.Framework;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPlanCorridorPlanningTests
    {
        [Test]
        public void CorridorIsDeterministicRegardlessOfConnectionOrder()
        {
            WorldPlanNodeTypeDefinition type = Type();
            WorldPlanGraphDefinition first = Graph(type, false);
            WorldPlanGraphDefinition second = Graph(type, true);
            WorldPlan a = new WorldPlanCompiler().Compile(1, first).Plan;
            WorldPlan b = new WorldPlanCompiler().Compile(1, second).Plan;
            WorldPlanLayoutSolver solver = new WorldPlanLayoutSolver();
            WorldPlanLayout la = solver.Solve(a).Layout;
            WorldPlanLayout lb = solver.Solve(b).Layout;
            WorldPlanCorridorResult ra = new WorldPlanCorridorPlanner().Plan(a, la);
            WorldPlanCorridorResult rb = new WorldPlanCorridorPlanner().Plan(b, lb);
            Assert.IsTrue(ra.Succeeded);
            Assert.AreEqual(ra.Corridors.Count, rb.Corridors.Count);
            for (int i = 0; i < ra.Corridors.Count; i++) Assert.AreEqual(ra.Corridors[i], rb.Corridors[i]);
        }

        [Test]
        public void TraversalPolicyCanBlockAndDetour()
        {
            WorldPlanNodeTypeDefinition type = Type();
            WorldPlanGraphDefinition graph = new WorldPlanGraphDefinition(
                new[] { type },
                new[] { new WorldPlanNodeDefinition("a", "Room", "A"), new WorldPlanNodeDefinition("b", "Room", "B") },
                new[] { new WorldPlanConnectionDefinition("ab", "a", "out", "b", "in", WorldPlanConnectionKind.Required) });
            WorldPlan plan = new WorldPlanCompiler().Compile(2, graph).Plan;
            WorldPlanLayout layout = new WorldPlanLayoutSolver().Solve(plan).Layout;
            WorldPlanCorridorResult result = new WorldPlanCorridorPlanner().Plan(plan, layout, new BlockMiddleTraversal());
            Assert.IsTrue(result.Succeeded);
            Assert.Greater(result.Corridors[0].Cells.Count, 0);
        }

        private sealed class BlockMiddleTraversal : IWorldPlanCorridorTraversal
        {
            public bool CanTraverse(WorldPlanCorridorContext context) => context.Position.X != 3;
            public int GetTraversalCost(WorldPlanCorridorContext context) => 1;
        }

        private static WorldPlanNodeTypeDefinition Type() => new WorldPlanNodeTypeDefinition(
            "Room", "Room", "Test", 2, 2, 0,
            new WorldPlanPortDefinition("in", "In", WorldPlanPortDirection.Input, "flow", false, false),
            new WorldPlanPortDefinition("out", "Out", WorldPlanPortDirection.Output, "flow", false, false));

        private static WorldPlanGraphDefinition Graph(WorldPlanNodeTypeDefinition type, bool reversed)
        {
            var nodes = new[] { new WorldPlanNodeDefinition("a", "Room", "A"), new WorldPlanNodeDefinition("b", "Room", "B"), new WorldPlanNodeDefinition("c", "Room", "C") };
            var ab = new WorldPlanConnectionDefinition("ab", "a", "out", "b", "in", WorldPlanConnectionKind.Required);
            var bc = new WorldPlanConnectionDefinition("bc", "b", "out", "c", "in", WorldPlanConnectionKind.Required);
            return new WorldPlanGraphDefinition(new[] { type }, nodes, reversed ? new[] { bc, ab } : new[] { ab, bc });
        }
    }
}
