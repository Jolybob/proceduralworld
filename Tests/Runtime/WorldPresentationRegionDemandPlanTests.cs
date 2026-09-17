using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldPresentationRegionDemandPlanTests
    {
        [Test]
        public void PlannerSeparatesLoadsAndUnloadsDeterministically()
        {
            var planner = new WorldPresentationRegionDemandPlanner();
            var plan = planner.CreatePlan(new[] { 2, 5, 9 }, new[] { 9, 4, 4, 7 });

            CollectionAssert.AreEqual(new[] { 7, 4 }, plan.RegionsToLoad);
            CollectionAssert.AreEqual(new[] { 5, 2 }, plan.RegionsToUnload);
        }

        [Test]
        public void PlannerReturnsEmptyPlanWhenDemandMatchesResidency()
        {
            var planner = new WorldPresentationRegionDemandPlanner();
            var plan = planner.CreatePlan(new[] { 2, 5 }, new[] { 5, 2, 2 });

            Assert.AreEqual(0, plan.RegionsToLoad.Count);
            Assert.AreEqual(0, plan.RegionsToUnload.Count);
        }

        [Test]
        public void PlannerDeduplicatesCurrentAndDemandedRegions()
        {
            var planner = new WorldPresentationRegionDemandPlanner();
            var plan = planner.CreatePlan(new[] { 3, 3, 8 }, new[] { 8, 8, 1 });

            CollectionAssert.AreEqual(new[] { 1 }, plan.RegionsToLoad);
            CollectionAssert.AreEqual(new[] { 3 }, plan.RegionsToUnload);
        }
    }
}
