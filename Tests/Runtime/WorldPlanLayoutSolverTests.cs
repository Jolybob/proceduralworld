using NUnit.Framework;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPlanLayoutSolverTests
    {
        [Test]
        public void LayoutIsDeterministicRegardlessOfInputOrder()
        {
            WorldPlanNodeTypeDefinition room = CreateType(
                "Room",
                5,
                3,
                1,
                new WorldPlanPortDefinition("in", "In", WorldPlanPortDirection.Input, "flow", false, false),
                new WorldPlanPortDefinition("out", "Out", WorldPlanPortDirection.Output, "flow", false, false));

            WorldPlanGraphDefinition first = CreateChainDefinition(
                new[]
                {
                    new WorldPlanNodeDefinition("middle", "Room", "Middle"),
                    new WorldPlanNodeDefinition("end", "Room", "End"),
                    new WorldPlanNodeDefinition("start", "Room", "Start")
                },
                new[]
                {
                    new WorldPlanConnectionDefinition("middleEnd", "middle", "out", "end", "in", WorldPlanConnectionKind.Required),
                    new WorldPlanConnectionDefinition("startMiddle", "start", "out", "middle", "in", WorldPlanConnectionKind.Required)
                },
                room);

            WorldPlanGraphDefinition second = CreateChainDefinition(
                new[]
                {
                    new WorldPlanNodeDefinition("start", "Room", "Start"),
                    new WorldPlanNodeDefinition("end", "Room", "End"),
                    new WorldPlanNodeDefinition("middle", "Room", "Middle")
                },
                new[]
                {
                    new WorldPlanConnectionDefinition("startMiddle", "start", "out", "middle", "in", WorldPlanConnectionKind.Required),
                    new WorldPlanConnectionDefinition("middleEnd", "middle", "out", "end", "in", WorldPlanConnectionKind.Required)
                },
                room);

            WorldPlan firstPlan = new WorldPlanCompiler().Compile(10, first).Plan;
            WorldPlan secondPlan = new WorldPlanCompiler().Compile(10, second).Plan;
            WorldPlanLayoutSolver solver = new WorldPlanLayoutSolver();

            WorldPlanLayoutResult firstLayout = solver.Solve(firstPlan);
            WorldPlanLayoutResult secondLayout = solver.Solve(secondPlan);

            Assert.IsTrue(firstLayout.Succeeded);
            Assert.IsTrue(secondLayout.Succeeded);
            Assert.AreEqual(firstLayout.Layout.Nodes.Count, secondLayout.Layout.Nodes.Count);
            for (int i = 0; i < firstLayout.Layout.Nodes.Count; i++)
                Assert.AreEqual(firstLayout.Layout.Nodes[i], secondLayout.Layout.Nodes[i]);
            for (int i = 0; i < firstLayout.Layout.Ports.Count; i++)
                Assert.AreEqual(firstLayout.Layout.Ports[i], secondLayout.Layout.Ports[i]);
        }

        [Test]
        public void LayoutRespectsMinimumClearance()
        {
            WorldPlanNodeTypeDefinition room = CreateType("Room", 4, 4, 2);
            WorldPlanGraphDefinition graph = new WorldPlanGraphDefinition(
                new[] { room },
                new[]
                {
                    new WorldPlanNodeDefinition("a", "Room", "A"),
                    new WorldPlanNodeDefinition("b", "Room", "B"),
                    new WorldPlanNodeDefinition("c", "Room", "C")
                },
                new WorldPlanConnectionDefinition[0]);

            WorldPlan plan = new WorldPlanCompiler().Compile(1, graph).Plan;
            WorldPlanLayoutResult result = new WorldPlanLayoutSolver().Solve(
                plan,
                new WorldPlanLayoutSettings(nodeSpacing: 1, componentSpacing: 1));

            Assert.IsTrue(result.Succeeded);
            for (int i = 0; i < result.Layout.Nodes.Count; i++)
            {
                for (int j = i + 1; j < result.Layout.Nodes.Count; j++)
                    Assert.IsFalse(result.Layout.Nodes[i].Intersects(result.Layout.Nodes[j], true));
            }
        }

        [Test]
        public void ConnectedNodesAdvanceAcrossDeterministicLayers()
        {
            WorldPlanNodeTypeDefinition room = CreateType("Room", 3, 2, 0,
                new WorldPlanPortDefinition("in", "In", WorldPlanPortDirection.Input, "flow", false, false),
                new WorldPlanPortDefinition("out", "Out", WorldPlanPortDirection.Output, "flow", false, false));
            WorldPlanGraphDefinition graph = new WorldPlanGraphDefinition(
                new[] { room },
                new[]
                {
                    new WorldPlanNodeDefinition("a", "Room", "A"),
                    new WorldPlanNodeDefinition("b", "Room", "B"),
                    new WorldPlanNodeDefinition("c", "Room", "C")
                },
                new[]
                {
                    new WorldPlanConnectionDefinition("ab", "a", "out", "b", "in", WorldPlanConnectionKind.Required),
                    new WorldPlanConnectionDefinition("bc", "b", "out", "c", "in", WorldPlanConnectionKind.Required)
                });

            WorldPlan plan = new WorldPlanCompiler().Compile(1, graph).Plan;
            WorldPlanLayoutResult result = new WorldPlanLayoutSolver().Solve(plan);

            Assert.IsTrue(result.Succeeded);
            WorldPlanNodeLayout a = FindNode(result, "a");
            WorldPlanNodeLayout b = FindNode(result, "b");
            WorldPlanNodeLayout c = FindNode(result, "c");
            Assert.Greater(b.Position.X, a.Position.X);
            Assert.Greater(c.Position.X, b.Position.X);
        }

        [Test]
        public void ConnectedPortAnchorsFollowSourceAndTargetSides()
        {
            WorldPlanNodeTypeDefinition room = CreateType("Room", 4, 4, 0,
                new WorldPlanPortDefinition("in", "In", WorldPlanPortDirection.Input, "flow", false, false),
                new WorldPlanPortDefinition("out", "Out", WorldPlanPortDirection.Output, "flow", false, false));
            WorldPlanGraphDefinition graph = new WorldPlanGraphDefinition(
                new[] { room },
                new[]
                {
                    new WorldPlanNodeDefinition("a", "Room", "A"),
                    new WorldPlanNodeDefinition("b", "Room", "B")
                },
                new[]
                {
                    new WorldPlanConnectionDefinition("ab", "a", "out", "b", "in", WorldPlanConnectionKind.Required)
                });

            WorldPlan plan = new WorldPlanCompiler().Compile(1, graph).Plan;
            WorldPlanLayoutResult result = new WorldPlanLayoutSolver().Solve(plan);

            Assert.IsTrue(result.Layout.TryGetPort("a", "out", out WorldPlanLayoutPort source));
            Assert.IsTrue(result.Layout.TryGetPort("b", "in", out WorldPlanLayoutPort target));
            Assert.AreEqual(WorldPlanLayoutPortSide.Right, source.Side);
            Assert.AreEqual(WorldPlanLayoutPortSide.Left, target.Side);
        }

        private static WorldPlanGraphDefinition CreateChainDefinition(
            WorldPlanNodeDefinition[] nodes,
            WorldPlanConnectionDefinition[] connections,
            WorldPlanNodeTypeDefinition type)
        {
            return new WorldPlanGraphDefinition(new[] { type }, nodes, connections);
        }

        private static WorldPlanNodeTypeDefinition CreateType(
            string id,
            int width,
            int height,
            int clearance,
            params WorldPlanPortDefinition[] ports)
        {
            return new WorldPlanNodeTypeDefinition(id, id, "Test", width, height, clearance, ports);
        }

        private static WorldPlanNodeLayout FindNode(WorldPlanLayoutResult result, string nodeId)
        {
            for (int i = 0; i < result.Layout.Nodes.Count; i++)
            {
                if (result.Layout.Nodes[i].NodeId == nodeId)
                    return result.Layout.Nodes[i];
            }

            Assert.Fail("Missing node layout: " + nodeId);
            return default(WorldPlanNodeLayout);
        }
    }
}
