using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPlanTests
    {
        [Test]
        public void Compiler_OrdersNodesAndConnectionsDeterministically()
        {
            WorldPlanNodeTypeDefinition type = CreateRoomType();
            var first = new WorldPlanGraphDefinition(
                new[] { type },
                new[]
                {
                    new WorldPlanNodeDefinition("b", "room", "B"),
                    new WorldPlanNodeDefinition("a", "room", "A")
                },
                new[]
                {
                    new WorldPlanConnectionDefinition("c2", "a", "out", "b", "in", WorldPlanConnectionKind.Required),
                    new WorldPlanConnectionDefinition("c1", "b", "out", "a", "in", WorldPlanConnectionKind.Optional)
                });

            var second = new WorldPlanGraphDefinition(
                new[] { type },
                new[]
                {
                    new WorldPlanNodeDefinition("a", "room", "A"),
                    new WorldPlanNodeDefinition("b", "room", "B")
                },
                new[]
                {
                    new WorldPlanConnectionDefinition("c1", "b", "out", "a", "in", WorldPlanConnectionKind.Optional),
                    new WorldPlanConnectionDefinition("c2", "a", "out", "b", "in", WorldPlanConnectionKind.Required)
                });

            WorldPlanCompiler compiler = new WorldPlanCompiler();
            WorldPlanCompilationResult firstResult = compiler.Compile(1234, first);
            WorldPlanCompilationResult secondResult = compiler.Compile(1234, second);

            Assert.IsTrue(firstResult.Succeeded);
            Assert.IsTrue(secondResult.Succeeded);
            Assert.AreEqual(firstResult.Plan.Nodes.Count, secondResult.Plan.Nodes.Count);
            Assert.AreEqual(firstResult.Plan.Connections.Count, secondResult.Plan.Connections.Count);

            for (int i = 0; i < firstResult.Plan.Nodes.Count; i++)
                Assert.AreEqual(firstResult.Plan.Nodes[i].Id, secondResult.Plan.Nodes[i].Id);

            for (int i = 0; i < firstResult.Plan.Connections.Count; i++)
            {
                Assert.AreEqual(firstResult.Plan.Connections[i].Id, secondResult.Plan.Connections[i].Id);
                Assert.AreEqual(firstResult.Plan.Connections[i].Kind, secondResult.Plan.Connections[i].Kind);
            }
        }

        [Test]
        public void Validator_RejectsUnknownNodeType()
        {
            var definition = new WorldPlanGraphDefinition(
                new List<WorldPlanNodeTypeDefinition>(),
                new[] { new WorldPlanNodeDefinition("node", "missing", "Node") },
                new List<WorldPlanConnectionDefinition>());

            WorldPlanValidationResult result = new WorldPlanCompiler().Validate(definition);

            Assert.IsFalse(result.IsValid);
            AssertHasCode(result, "UnknownNodeType");
        }

        [Test]
        public void Validator_RejectsIncompatibleSemanticPortTypes()
        {
            WorldPlanNodeTypeDefinition sourceType = new WorldPlanNodeTypeDefinition(
                "source",
                "Source",
                "Test",
                1,
                1,
                0,
                new[]
                {
                    new WorldPlanPortDefinition("out", "Out", WorldPlanPortDirection.Output, "road", false, true)
                });

            WorldPlanNodeTypeDefinition targetType = new WorldPlanNodeTypeDefinition(
                "target",
                "Target",
                "Test",
                1,
                1,
                0,
                new[]
                {
                    new WorldPlanPortDefinition("in", "In", WorldPlanPortDirection.Input, "water", false, true)
                });

            var definition = new WorldPlanGraphDefinition(
                new[] { sourceType, targetType },
                new[]
                {
                    new WorldPlanNodeDefinition("a", "source", "A"),
                    new WorldPlanNodeDefinition("b", "target", "B")
                },
                new[]
                {
                    new WorldPlanConnectionDefinition("edge", "a", "out", "b", "in", WorldPlanConnectionKind.Required)
                });

            WorldPlanValidationResult result = new WorldPlanCompiler().Validate(definition);

            Assert.IsFalse(result.IsValid);
            AssertHasCode(result, "IncompatiblePortTypes");
        }

        [Test]
        public void Validator_RejectsUnconnectedRequiredPort()
        {
            WorldPlanNodeTypeDefinition type = new WorldPlanNodeTypeDefinition(
                "room",
                "Room",
                "Test",
                1,
                1,
                0,
                new[]
                {
                    new WorldPlanPortDefinition("entry", "Entry", WorldPlanPortDirection.Input, "door", true, false)
                });

            var definition = new WorldPlanGraphDefinition(
                new[] { type },
                new[] { new WorldPlanNodeDefinition("room-a", "room", "Room A") },
                new List<WorldPlanConnectionDefinition>());

            WorldPlanValidationResult result = new WorldPlanCompiler().Validate(definition);

            Assert.IsFalse(result.IsValid);
            AssertHasCode(result, "RequiredPortUnconnected");
        }

        [Test]
        public void Compiler_PreservesNodeProperties()
        {
            WorldPlanNodeTypeDefinition type = CreateRoomType();
            var definition = new WorldPlanGraphDefinition(
                new[] { type },
                new[]
                {
                    new WorldPlanNodeDefinition(
                        "room-a",
                        "room",
                        "Room A",
                        new[] { new WorldPlanProperty("biome", "crystal") })
                },
                new List<WorldPlanConnectionDefinition>());

            WorldPlanCompilationResult result = new WorldPlanCompiler().Compile(7, definition);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, result.Plan.Nodes.Count);
            Assert.AreEqual("biome", result.Plan.Nodes[0].Properties[0].Key);
            Assert.AreEqual("crystal", result.Plan.Nodes[0].Properties[0].Value);
        }

        private static WorldPlanNodeTypeDefinition CreateRoomType()
        {
            return new WorldPlanNodeTypeDefinition(
                "room",
                "Room",
                "Dungeon",
                5,
                5,
                1,
                new[]
                {
                    new WorldPlanPortDefinition("in", "In", WorldPlanPortDirection.Input, "door", false, true),
                    new WorldPlanPortDefinition("out", "Out", WorldPlanPortDirection.Output, "door", false, true)
                },
                new[] { "biome", "role" });
        }

        private static void AssertHasCode(WorldPlanValidationResult result, string code)
        {
            for (int i = 0; i < result.Issues.Count; i++)
            {
                if (result.Issues[i].Code == code)
                    return;
            }

            Assert.Fail("Expected validation issue code '" + code + "'.");
        }
    }
}
