using System.Collections.Generic;
using NUnit.Framework;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldRealizationTests
    {
        [Test]
        public void BatchIsDeterministicRegardlessOfInputOrder()
        {
            var a = new WorldRealizationBatch(new[]
            {
                new WorldRealizationEdit("b", "node:b", "Tile", "Stone", 0, new WorldPosition(2, 1)),
                new WorldRealizationEdit("a", "node:a", "Tile", "Dirt", 0, new WorldPosition(-1, 0))
            });
            var b = new WorldRealizationBatch(new[]
            {
                new WorldRealizationEdit("a", "node:a", "Tile", "Dirt", 0, new WorldPosition(-1, 0)),
                new WorldRealizationEdit("b", "node:b", "Tile", "Stone", 0, new WorldPosition(2, 1))
            });

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) Assert.AreEqual(a.Edits[i], b.Edits[i]);
        }

        [Test]
        public void MapIndexesNegativeAndCrossChunkPositions()
        {
            var map = new WorldRealizationMap(4);
            var edit = new WorldRealizationEdit("tile", "room", "Tile", "Stone", 0, new WorldPosition(-1, -1));
            Assert.IsTrue(map.TryAdd(edit, out WorldRealizationConflict ignored));

            var results = new List<WorldRealizationEdit>();
            map.CollectChunk(new ChunkCoord(-1, -1), results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(edit, results[0]);
        }

        [Test]
        public void SamePositionAndKindConflictIsRejectedButDifferentKindsCanCoexist()
        {
            var map = new WorldRealizationMap(8);
            var tile = new WorldRealizationEdit("tile", "room", "Tile", "Stone", 0, new WorldPosition(2, 2));
            var otherTile = new WorldRealizationEdit("other", "other-room", "Tile", "Dirt", 0, new WorldPosition(2, 2));
            var objectEdit = new WorldRealizationEdit("object", "room", "Object", "Chest", 0, new WorldPosition(2, 2));

            Assert.IsTrue(map.TryAdd(tile, out WorldRealizationConflict ignored));
            Assert.IsFalse(map.TryAdd(otherTile, out WorldRealizationConflict conflict));
            Assert.AreEqual("tile", conflict.Existing.Id);
            Assert.IsTrue(map.TryAdd(objectEdit, out WorldRealizationConflict ignoredObject));
        }

        [Test]
        public void FeatureRealizerPreservesSourceTraceabilityAndCanonicalOrder()
        {
            var placements = new List<WorldPlanFeaturePlacement>
            {
                new WorldPlanFeaturePlacement("node:b", new WorldFeaturePlacement("Room", new WorldPosition(2, 2), new ChunkCoord(0, 0), 2, 2)),
                new WorldPlanFeaturePlacement("node:a", new WorldFeaturePlacement("Room", new WorldPosition(0, 0), new ChunkCoord(0, 0), 2, 2))
            };
            var lowering = new WorldPlanFeatureLoweringResultForTest(placements).Result;
            var batch = new WorldPlanRealizer().Realize(lowering, new TestSource());

            Assert.AreEqual(2, batch.Count);
            Assert.AreEqual(new WorldPosition(0, 0), batch.Edits[0].Position);
            Assert.AreEqual("node:a", batch.Edits[0].SourceId);
            Assert.AreEqual("node:b", batch.Edits[1].SourceId);
        }

        private sealed class TestSource : IWorldPlanRealizationSource
        {
            public IEnumerable<WorldRealizationEdit> CreateEdits(WorldPlanFeaturePlacement placement)
            {
                yield return new WorldRealizationEdit(
                    placement.NodeId + ":tile",
                    placement.NodeId,
                    "Tile",
                    placement.Placement.FeatureId,
                    0,
                    placement.Placement.Anchor);
            }
        }

        private sealed class WorldPlanFeatureLoweringResultForTest
        {
            public WorldPlanFeatureLoweringResult Result { get; }
            public WorldPlanFeatureLoweringResultForTest(IReadOnlyList<WorldPlanFeaturePlacement> placements)
            {
                Result = Create(placements);
            }

            private static WorldPlanFeatureLoweringResult Create(IReadOnlyList<WorldPlanFeaturePlacement> placements)
            {
                return (WorldPlanFeatureLoweringResult)typeof(WorldPlanFeatureLoweringResult)
                    .GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(List<WorldPlanFeaturePlacement>), typeof(List<WorldPlanValidationIssue>) }, null)
                    .Invoke(new object[] { new List<WorldPlanFeaturePlacement>(placements), new List<WorldPlanValidationIssue>() });
            }
        }
    }
}
