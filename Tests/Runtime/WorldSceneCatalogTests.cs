using System.Collections.Generic;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldSceneCatalogTests
    {
        private static WorldSceneDefinition Scene(string id, int weight = 1, bool unique = false, IEnumerable<string> tags = null)
        {
            return new WorldSceneDefinition(id, id.GetHashCode() & int.MaxValue, 4, 4, 1f, 1, 0, 0, weight, true, unique, WorldSceneOrientationMode.RotateAndMirror, tags);
        }

        [Test]
        public void CatalogCanonicalizesSceneOrderAndRejectsDuplicateIds()
        {
            var catalog = new WorldSceneCatalog(new[] { Scene("b"), Scene("a") });
            Assert.AreEqual("a", catalog.Scenes[0].Id);
            Assert.AreEqual("b", catalog.Scenes[1].Id);
            Assert.Throws<System.ArgumentException>(() => new WorldSceneCatalog(new[] { Scene("a"), Scene("a") }));
        }

        [Test]
        public void SelectionIsStableWhenCatalogInputOrderChanges()
        {
            var a = Scene("a", 3);
            var b = Scene("b", 7);
            var selector = new WorldSceneSelector();
            WorldSceneDefinition first = selector.Select(1234, new WorldSceneCatalog(new[] { a, b }));
            WorldSceneDefinition second = selector.Select(1234, new WorldSceneCatalog(new[] { b, a }));
            Assert.IsNotNull(first);
            Assert.AreEqual(first.Id, second.Id);
        }

        [Test]
        public void RequiredTagsAndUniqueConsumptionFilterScenePool()
        {
            var common = Scene("common", 100);
            var unique = Scene("temple", 1, true, new[] { "desert" });
            var catalog = new WorldSceneCatalog(new[] { common, unique });
            var consumed = new HashSet<string> { "temple" };
            WorldSceneDefinition selected = new WorldSceneSelector().Select(
                99,
                catalog,
                new WorldSceneSelectionSettings(tags: new[] { "desert" }, consumedUniqueSceneIds: consumed));
            Assert.IsNull(selected);
        }

        [Test]
        public void TagFilteringSelectsOnlyMatchingBiomePool()
        {
            var forest = Scene("forest", 1, false, new[] { "forest" });
            var desert = Scene("desert", 1, false, new[] { "desert" });
            WorldSceneDefinition selected = new WorldSceneSelector().SelectForRegion(
                5,
                new WorldSceneCatalog(new[] { forest, desert }),
                new[] { "desert" });
            Assert.AreEqual("desert", selected.Id);
        }

        [Test]
        public void EmptyEligiblePoolReturnsNull()
        {
            WorldSceneDefinition selected = new WorldSceneSelector().Select(
                5,
                new WorldSceneCatalog(new[] { Scene("disabled", 0) }));
            Assert.IsNull(selected);
        }
    }
}
