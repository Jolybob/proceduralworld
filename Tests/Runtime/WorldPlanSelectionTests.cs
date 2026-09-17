using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPlanSelectionTests
    {
        private static WorldPlanGraphDefinition Graph(string typeId)
        {
            return new WorldPlanGraphDefinition(
                new[] { new WorldPlanNodeTypeDefinition(typeId, typeId, "Test", 1, 1, 0, null) },
                new[] { new WorldPlanNodeDefinition("root", typeId, "Root") },
                null);
        }

        [Test]
        public void SameSeedAndCandidatesSelectSamePlanRegardlessOfInputOrder()
        {
            var a = new WorldPlanCandidate("a", Graph("a"), 3);
            var b = new WorldPlanCandidate("b", Graph("b"), 7);
            var selector = new WorldPlanSelector();

            WorldPlanSelectionResult first = selector.Select(12345, new[] { a, b });
            WorldPlanSelectionResult second = selector.Select(12345, new[] { b, a });

            Assert.IsTrue(first.Succeeded);
            Assert.AreEqual(first.Candidate.Id, second.Candidate.Id);
        }

        [Test]
        public void RequiredTagsFilterCandidatesBeforeWeightedSelection()
        {
            var a = new WorldPlanCandidate("a", Graph("a"), 1, true, new[] { "snow" });
            var b = new WorldPlanCandidate("b", Graph("b"), 1, true, new[] { "desert" });

            WorldPlanSelectionResult result = new WorldPlanSelector().Select(
                99,
                new[] { a, b },
                new WorldPlanSelectionSettings(tags: new[] { "desert" }));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("b", result.Candidate.Id);
        }

        [Test]
        public void DisabledAndZeroWeightCandidatesAreIgnored()
        {
            var disabled = new WorldPlanCandidate("a", Graph("a"), 100, false);
            var zero = new WorldPlanCandidate("b", Graph("b"), 0);
            var enabled = new WorldPlanCandidate("c", Graph("c"), 1);

            WorldPlanSelectionResult result = new WorldPlanSelector().Select(1, new List<WorldPlanCandidate> { disabled, zero, enabled });

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("c", result.Candidate.Id);
        }

        [Test]
        public void NoEligibleCandidatesReturnsUnsuccessfulResult()
        {
            WorldPlanSelectionResult result = new WorldPlanSelector().Select(
                1,
                new[] { new WorldPlanCandidate("a", Graph("a"), 0) });

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Candidate);
        }
    }
}
