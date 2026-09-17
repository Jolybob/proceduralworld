using NUnit.Framework;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldGenerationDependencySchedulerTests
    {
        [Test]
        public void IndependentWorkIsDeterministicRegardlessOfRequestOrder()
        {
            var a = new WorldGenerationWorkGraph();
            a.Request(new ChunkCoord(2, 0), WorldGenerationWorkKind.Materialization, 1);
            a.Request(new ChunkCoord(-1, 3), WorldGenerationWorkKind.Realization, 2);
            a.Request(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan, 2);

            var b = new WorldGenerationWorkGraph();
            b.Request(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan, 2);
            b.Request(new ChunkCoord(2, 0), WorldGenerationWorkKind.Materialization, 1);
            b.Request(new ChunkCoord(-1, 3), WorldGenerationWorkKind.Realization, 2);

            WorldGenerationDependencySchedule sa = a.BuildSchedule();
            WorldGenerationDependencySchedule sb = b.BuildSchedule();
            Assert.IsTrue(sa.Succeeded);
            Assert.AreEqual(sa.Count, sb.Count);
            for (int i = 0; i < sa.Count; i++) Assert.AreEqual(sa.Items[i], sb.Items[i]);
        }

        [Test]
        public void SameChunkCanCarryPlanRealizationAndMaterializationDependencies()
        {
            var graph = new WorldGenerationWorkGraph();
            var chunk = new ChunkCoord(4, -2);
            var plan = new WorldGenerationWorkKey(chunk, WorldGenerationWorkKind.Plan);
            var realization = new WorldGenerationWorkKey(chunk, WorldGenerationWorkKind.Realization);
            var materialization = new WorldGenerationWorkKey(chunk, WorldGenerationWorkKind.Materialization);

            graph.Request(chunk, WorldGenerationWorkKind.Plan, 10);
            graph.Request(chunk, WorldGenerationWorkKind.Realization, 5);
            graph.Request(chunk, WorldGenerationWorkKind.Materialization, 1);
            graph.AddDependency(plan, realization);
            graph.AddDependency(realization, materialization);

            WorldGenerationDependencySchedule schedule = graph.BuildSchedule();
            Assert.IsTrue(schedule.Succeeded);
            Assert.AreEqual(3, schedule.Count);
            Assert.AreEqual(WorldGenerationWorkKind.Plan, schedule.Items[0].Kind);
            Assert.AreEqual(WorldGenerationWorkKind.Realization, schedule.Items[1].Kind);
            Assert.AreEqual(WorldGenerationWorkKind.Materialization, schedule.Items[2].Kind);
        }

        [Test]
        public void CrossChunkDependencyIsHonoredBeforeDependentWork()
        {
            var graph = new WorldGenerationWorkGraph();
            var source = new WorldGenerationWorkKey(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan);
            var dependent = new WorldGenerationWorkKey(new ChunkCoord(10, 10), WorldGenerationWorkKind.Materialization);
            graph.Request(source.Chunk, source.Kind);
            graph.Request(dependent.Chunk, dependent.Kind);
            graph.AddDependency(source, dependent);

            WorldGenerationDependencySchedule schedule = graph.BuildSchedule();
            Assert.IsTrue(schedule.Succeeded);
            Assert.AreEqual(source, new WorldGenerationWorkKey(schedule.Items[0].Chunk, schedule.Items[0].Kind));
            Assert.AreEqual(dependent, new WorldGenerationWorkKey(schedule.Items[1].Chunk, schedule.Items[1].Kind));
        }

        [Test]
        public void CycleProducesStructuredDiagnosticWithoutDequeueingWork()
        {
            var graph = new WorldGenerationWorkGraph();
            var a = new WorldGenerationWorkKey(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan);
            var b = new WorldGenerationWorkKey(new ChunkCoord(1, 0), WorldGenerationWorkKind.Realization);
            graph.AddDependency(a, b);
            graph.AddDependency(b, a);

            WorldGenerationDependencySchedule schedule = graph.Dequeue(10);
            Assert.IsFalse(schedule.Succeeded);
            Assert.AreEqual("DependencyCycle", schedule.Issues[0].Code);
            Assert.AreEqual(2, graph.Count);
        }

        [Test]
        public void BudgetReturnsOnlyReadyPrefixAndLeavesGraphIntact()
        {
            var graph = new WorldGenerationWorkGraph();
            graph.Request(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan);
            graph.Request(new ChunkCoord(1, 0), WorldGenerationWorkKind.Plan);
            graph.Request(new ChunkCoord(2, 0), WorldGenerationWorkKind.Plan);

            WorldGenerationDependencySchedule schedule = graph.Dequeue(2);
            Assert.IsTrue(schedule.Succeeded);
            Assert.AreEqual(2, schedule.Count);
            Assert.AreEqual(1, graph.Count);
        }
    }
}
