using System.Collections.Generic;
using NUnit.Framework;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPlanSubgraphCompilerTests
    {
        [Test]
        public void ExpandsTemplateAndRewiresExposedPorts()
        {
            WorldPlanGraphDefinition template = CreateRoomTemplate("RoomTemplate", "room", "flowOut", WorldPlanPortDirection.Output);
            var exposed = new[]
            {
                new WorldPlanSubgraphTemplateDefinition(
                    "RoomTemplate",
                    "Room Template",
                    template,
                    new[]
                    {
                        new WorldPlanSubgraphPortDefinition("out", "Out", "room", "flowOut")
                    })
            };

            WorldPlanNodeTypeDefinition sinkType = CreateType(
                "Sink",
                new WorldPlanPortDefinition("in", "In", WorldPlanPortDirection.Input, "flow", true, false));
            WorldPlanGraphDefinition host = new WorldPlanGraphDefinition(
                new[] { sinkType },
                new[]
                {
                    new WorldPlanNodeDefinition("dungeon", "__subgraph_instance__", "Dungeon", "RoomTemplate"),
                    new WorldPlanNodeDefinition("sink", "Sink", "Sink")
                },
                new[]
                {
                    new WorldPlanConnectionDefinition("connect", "dungeon", "out", "sink", "in", WorldPlanConnectionKind.Required)
                },
                exposed);

            WorldPlanSubgraphCompiler compiler = new WorldPlanSubgraphCompiler();
            WorldPlanSubgraphExpansionResult expansion = compiler.Expand(host);

            Assert.IsTrue(expansion.Succeeded);
            Assert.AreEqual(2, expansion.ExpandedDefinition.Nodes.Count);
            Assert.AreEqual(1, expansion.ExpandedDefinition.Connections.Count);
            Assert.IsNotNull(FindNode(expansion.ExpandedDefinition, "dungeon/room"));
            Assert.IsNotNull(FindNode(expansion.ExpandedDefinition, "sink"));
            Assert.AreEqual("dungeon/room", expansion.ExpandedDefinition.Connections[0].SourceNodeId);
            Assert.AreEqual("flowOut", expansion.ExpandedDefinition.Connections[0].SourcePortId);

            WorldPlanCompilationResult compilation = compiler.Compile(123, host);
            Assert.IsTrue(compilation.Succeeded);
            Assert.AreEqual(2, compilation.Plan.Nodes.Count);
            Assert.AreEqual(1, compilation.Plan.Connections.Count);
            Assert.AreEqual(123, compilation.Plan.Seed);
        }

        [Test]
        public void ExpansionIsIndependentOfInputOrder()
        {
            WorldPlanNodeTypeDefinition sourceType = CreateType(
                "Source",
                new WorldPlanPortDefinition("out", "Out", WorldPlanPortDirection.Output, "flow", false, false));
            WorldPlanNodeTypeDefinition sinkType = CreateType(
                "Sink",
                new WorldPlanPortDefinition("in", "In", WorldPlanPortDirection.Input, "flow", false, false));
            WorldPlanSubgraphTemplateDefinition template = new WorldPlanSubgraphTemplateDefinition(
                "Pair",
                "Pair",
                new WorldPlanGraphDefinition(
                    new[] { sourceType, sinkType },
                    new[]
                    {
                        new WorldPlanNodeDefinition("sink", "Sink", "Sink"),
                        new WorldPlanNodeDefinition("source", "Source", "Source")
                    },
                    new[]
                    {
                        new WorldPlanConnectionDefinition("pairFlow", "source", "out", "sink", "in", WorldPlanConnectionKind.Required)
                    }));

            var first = new WorldPlanGraphDefinition(
                new[] { CreateType("Host", new WorldPlanPortDefinition("in", "In", WorldPlanPortDirection.Input, "flow", false, true)) },
                new[]
                {
                    new WorldPlanNodeDefinition("z", "Host", "Z", "Pair"),
                    new WorldPlanNodeDefinition("a", "Host", "A", "Pair")
                },
                new WorldPlanConnectionDefinition[0],
                new[] { template });

            var second = new WorldPlanGraphDefinition(
                new[] { CreateType("Host", new WorldPlanPortDefinition("in", "In", WorldPlanPortDirection.Input, "flow", false, true)) },
                new[]
                {
                    new WorldPlanNodeDefinition("a", "Host", "A", "Pair"),
                    new WorldPlanNodeDefinition("z", "Host", "Z", "Pair")
                },
                new WorldPlanConnectionDefinition[0],
                new[] { template });

            WorldPlanSubgraphCompiler compiler = new WorldPlanSubgraphCompiler();
            WorldPlanGraphDefinition a = compiler.Expand(first).ExpandedDefinition;
            WorldPlanGraphDefinition b = compiler.Expand(second).ExpandedDefinition;

            Assert.AreEqual(a.NodeTypes.Count, b.NodeTypes.Count);
            Assert.AreEqual(a.Nodes.Count, b.Nodes.Count);
            Assert.AreEqual(a.Connections.Count, b.Connections.Count);
            for (int i = 0; i < a.NodeTypes.Count; i++)
                Assert.AreEqual(a.NodeTypes[i].Id, b.NodeTypes[i].Id);
            for (int i = 0; i < a.Nodes.Count; i++)
            {
                Assert.AreEqual(a.Nodes[i].Id, b.Nodes[i].Id);
                Assert.AreEqual(a.Nodes[i].TypeId, b.Nodes[i].TypeId);
            }
            for (int i = 0; i < a.Connections.Count; i++)
                Assert.AreEqual(a.Connections[i].Id, b.Connections[i].Id);
        }

        [Test]
        public void NestedTemplatesAreFlattenedWithScopedIds()
        {
            WorldPlanNodeTypeDefinition roomType = CreateType(
                "Room",
                new WorldPlanPortDefinition("out", "Out", WorldPlanPortDirection.Output, "flow", false, false));
            WorldPlanSubgraphTemplateDefinition child = new WorldPlanSubgraphTemplateDefinition(
                "Child",
                "Child",
                new WorldPlanGraphDefinition(new[] { roomType }, new[] { new WorldPlanNodeDefinition("room", "Room", "Room") }, new WorldPlanConnectionDefinition[0]),
                new[] { new WorldPlanSubgraphPortDefinition("out", "Out", "room", "out") });
            WorldPlanSubgraphTemplateDefinition parent = new WorldPlanSubgraphTemplateDefinition(
                "Parent",
                "Parent",
                new WorldPlanGraphDefinition(
                    new[] { roomType },
                    new[] { new WorldPlanNodeDefinition("child", "__subgraph_instance__", "Child", "Child") },
                    new WorldPlanConnectionDefinition[0],
                    new[] { child }),
                new[] { new WorldPlanSubgraphPortDefinition("out", "Out", "child", "out") });

            WorldPlanGraphDefinition host = new WorldPlanGraphDefinition(
                new WorldPlanNodeTypeDefinition[0],
                new[] { new WorldPlanNodeDefinition("instance", "__subgraph_instance__", "Instance", "Parent") },
                new WorldPlanConnectionDefinition[0],
                new[] { parent });

            WorldPlanSubgraphExpansionResult expansion = new WorldPlanSubgraphCompiler().Expand(host);
            Assert.IsTrue(expansion.Succeeded);
            Assert.AreEqual(1, expansion.ExpandedDefinition.Nodes.Count);
            Assert.AreEqual("instance/child/room", expansion.ExpandedDefinition.Nodes[0].Id);
        }

        [Test]
        public void TemplateCycleIsRejected()
        {
            WorldPlanGraphDefinition graph = new WorldPlanGraphDefinition(
                new WorldPlanNodeTypeDefinition[0],
                new[] { new WorldPlanNodeDefinition("self", "__subgraph_instance__", "Self", "Self") },
                new WorldPlanConnectionDefinition[0]);
            WorldPlanSubgraphTemplateDefinition self = new WorldPlanSubgraphTemplateDefinition(
                "Self",
                "Self",
                graph);
            WorldPlanGraphDefinition host = new WorldPlanGraphDefinition(
                new WorldPlanNodeTypeDefinition[0],
                new[] { new WorldPlanNodeDefinition("instance", "__subgraph_instance__", "Instance", "Self") },
                new WorldPlanConnectionDefinition[0],
                new[] { self });

            WorldPlanValidationResult validation = new WorldPlanSubgraphCompiler().Validate(host);

            Assert.IsFalse(validation.IsValid);
            Assert.IsTrue(ContainsIssue(validation, "TemplateCycle"));
        }

        [Test]
        public void MissingExposedPortIsRejected()
        {
            WorldPlanGraphDefinition templateGraph = CreateRoomTemplate("Template", "room", "out", WorldPlanPortDirection.Output);
            WorldPlanSubgraphTemplateDefinition template = new WorldPlanSubgraphTemplateDefinition(
                "Template",
                "Template",
                templateGraph,
                new[] { new WorldPlanSubgraphPortDefinition("out", "Out", "room", "out") });
            WorldPlanGraphDefinition host = new WorldPlanGraphDefinition(
                new WorldPlanNodeTypeDefinition[0],
                new[] { new WorldPlanNodeDefinition("instance", "__subgraph_instance__", "Instance", "Template") },
                new[]
                {
                    new WorldPlanConnectionDefinition("bad", "instance", "missing", "instance", "missing", WorldPlanConnectionKind.Required)
                },
                new[] { template });

            WorldPlanValidationResult validation = new WorldPlanSubgraphCompiler().Validate(host);

            Assert.IsFalse(validation.IsValid);
            Assert.IsTrue(ContainsIssue(validation, "MissingExposedPort"));
        }

        private static WorldPlanGraphDefinition CreateRoomTemplate(
            string unusedTemplateId,
            string roomId,
            string portId,
            WorldPlanPortDirection direction)
        {
            return new WorldPlanGraphDefinition(
                new[]
                {
                    CreateType(
                        "Room",
                        new WorldPlanPortDefinition(portId, "Flow", direction, "flow", false, false))
                },
                new[] { new WorldPlanNodeDefinition(roomId, "Room", "Room") },
                new WorldPlanConnectionDefinition[0]);
        }

        private static WorldPlanNodeTypeDefinition CreateType(string id, params WorldPlanPortDefinition[] ports)
        {
            return new WorldPlanNodeTypeDefinition(id, id, "Test", 1, 1, 0, ports);
        }

        private static WorldPlanNode FindNode(WorldPlanGraphDefinition definition, string id)
        {
            for (int i = 0; i < definition.Nodes.Count; i++)
            {
                if (definition.Nodes[i].Id == id)
                    return null;
            }

            return null;
        }

        private static bool ContainsIssue(WorldPlanValidationResult validation, string code)
        {
            for (int i = 0; i < validation.Issues.Count; i++)
            {
                if (validation.Issues[i].Code == code)
                    return true;
            }

            return false;
        }
    }
}
