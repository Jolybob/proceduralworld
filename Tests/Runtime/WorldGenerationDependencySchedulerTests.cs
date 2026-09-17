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
        public void SameChunkCanCarryPlanRealizationAndMaterializationDependenciesAcrossBudgets()
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

            WorldGenerationDependencySchedule first = graph.Dequeue(1);
            Assert.IsTrue(first.Succeeded);
            Assert.AreEqual(plan, new WorldGenerationWorkKey(first.Items[0].Chunk, first.Items[0].Kind));
            Assert.AreEqual(WorldGenerationWorkStatus.Running, graph.GetStatus(plan));

            WorldGenerationDependencySchedule blocked = graph.BuildSchedule();
            Assert.AreEqual(0, blocked.Count);
            Assert.IsTrue(blocked.Succeeded);

            Assert.IsTrue(graph.Complete(plan));
            WorldGenerationDependencySchedule second = graph.Dequeue(1);
            Assert.AreEqual(realization, new WorldGenerationWorkKey(second.Items[0].Chunk, second.Items[0].Kind));
            Assert.IsTrue(graph.Complete(realization));

            WorldGenerationDependencySchedule third = graph.Dequeue(1);
            Assert.AreEqual(materialization, new WorldGenerationWorkKey(third.Items[0].Chunk, third.Items[0].Kind));
        }

        [Test]
        public void CrossChunkDependencyIsHonoredBeforeDependentWork()
        {
            var graph = new WorldGenerationWorkGraph();
            var source = new WorldGenerationWorkKey(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan);
            var dependent = new WorldGenerationWorkKey(new ChunkCoord(10, 10), WorldGenerationWorkKind.Materialization);
            graph.AddDependency(source, dependent);

            WorldGenerationDependencySchedule first = graph.Dequeue(1);
            Assert.IsTrue(first.Succeeded);
            Assert.AreEqual(source, new WorldGenerationWorkKey(first.Items[0].Chunk, first.Items[0].Kind));
            Assert.IsTrue(graph.Complete(source));

            WorldGenerationDependencySchedule second = graph.Dequeue(1);
            Assert.AreEqual(dependent, new WorldGenerationWorkKey(second.Items[0].Chunk, second.Items[0].Kind));
        }

        [Test]
        public void CycleProducesStructuredDiagnosticWithoutClaimingWork()
        {
            var graph = new WorldGenerationWorkGraph();
            var a = new WorldGenerationWorkKey(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan);
            var b = new WorldGenerationWorkKey(new ChunkCoord(1, 0), WorldGenerationWorkKind.Realization);
            graph.AddDependency(a, b);
            graph.AddDependency(b, a);

            WorldGenerationDependencySchedule schedule = graph.Dequeue(10);
            Assert.IsFalse(schedule.Succeeded);
            Assert.AreEqual("DependencyCycle", schedule.Issues[0].Code);
            Assert.AreEqual(WorldGenerationWorkStatus.Pending, graph.GetStatus(a));
            Assert.AreEqual(WorldGenerationWorkStatus.Pending, graph.GetStatus(b));
        }

        [Test]
        public void RunningPrerequisiteBlocksDependentsWithoutReportingCycle()
        {
            var graph = new WorldGenerationWorkGraph();
            var source = new WorldGenerationWorkKey(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan);
            var dependent = new WorldGenerationWorkKey(new ChunkCoord(1, 0), WorldGenerationWorkKind.Realization);
            graph.AddDependency(source, dependent);
            Assert.AreEqual(1, graph.Dequeue(1).Count);

            WorldGenerationDependencySchedule blocked = graph.BuildSchedule();
            Assert.IsTrue(blocked.Succeeded);
            Assert.AreEqual(0, blocked.Count);
        }

        [Test]
        public void FailedWorkBlocksDependentsUntilRetriedAndCompleted()
        {
            var graph = new WorldGenerationWorkGraph();
            var source = new WorldGenerationWorkKey(new ChunkCoord(0, 0), WorldGenerationWorkKind.Plan);
            var dependent = new WorldGenerationWorkKey(new ChunkCoord(0, 0), WorldGenerationWorkKind.Realization);
            graph.AddDependency(source, dependent);

            Assert.AreEqual(1, graph.Dequeue(1).Count);
            Assert.IsTrue(graph.Fail(source));
            WorldGenerationDependencySchedule blocked = graph.BuildSchedule();
            Assert.IsFalse(blocked.Succeeded);
            Assert.AreEqual("FailedDependency", blocked.Issues[0].Code);

            Assert.IsTrue(graph.Retry(source));
            Assert.AreEqual(1, graph.Dequeue(1).Count);
            Assert.IsTrue(graph.Complete(source));
            Assert.AreEqual(1, graph.Dequeue(1).Count);
        }
    }
}
