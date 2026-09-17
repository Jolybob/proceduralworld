using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class WorldFeaturePlacementTests
    {
        [Test]
        public void Planner_CreatesWorldSpacePlacementAcrossChunkBoundary()
        {
            var definition = new TestFeatureDefinition(
                featureId: 17,
                width: 5,
                height: 3,
                spawnChance: 1f,
                maxPerChunk: 1);
            var planner = new WorldFeaturePlacementPlanner(1234);

            WorldFeaturePlacement placement = planner.CreatePlacement(
                new ChunkCoord(0, 0),
                definition,
                anchorX: 63,
                anchorY: 2);

            Assert.AreEqual(17, placement.FeatureId);
            Assert.AreEqual(new WorldPosition(63, 2), placement.Anchor);
            Assert.AreEqual(new ChunkCoord(0, 0), placement.OwnerChunk);
            Assert.AreEqual(63, placement.MinX);
            Assert.AreEqual(67, placement.MaxX);
            Assert.IsTrue(placement.Intersects(new ChunkCoord(1, 0), 64));
            Assert.IsTrue(placement.Contains(new WorldPosition(67, 4)));
        }

        [Test]
        public void Planner_IsDeterministicForSameSeedAndOwnerChunk()
        {
            var definition = new TestFeatureDefinition(
                featureId: 9,
                width: 3,
                height: 3,
                spawnChance: 0.5f,
                maxPerChunk: 3);
            var first = new WorldFeaturePlacementSet();
            var second = new WorldFeaturePlacementSet();
            var planner = new WorldFeaturePlacementPlanner(9876);

            planner.CollectOwnerChunk(
                new ChunkCoord(-2, 3),
                definition,
                WorldRandomDomain.Structures,
                64,
                first);
            planner.CollectOwnerChunk(
                new ChunkCoord(-2, 3),
                definition,
                WorldRandomDomain.Structures,
                64,
                second);

            Assert.AreEqual(first.Count, second.Count);
            Assert.AreEqual(first.Placements.Count, second.Placements.Count);
            for (int i = 0; i < first.Placements.Count; i++)
                Assert.AreEqual(first.Placements[i], second.Placements[i]);
        }

        [Test]
        public void Placement_UsesFloorChunkSemanticsForNegativeCoordinates()
        {
            var placement = new WorldFeaturePlacement(
                featureId: 4,
                anchor: new WorldPosition(-65, -65),
                ownerChunk: new ChunkCoord(-2, -2),
                width: 5,
                height: 5);

            Assert.IsTrue(placement.Intersects(new ChunkCoord(-2, -2), 64));
            Assert.IsTrue(placement.Intersects(new ChunkCoord(-1, -1), 64));
            Assert.IsFalse(placement.Intersects(new ChunkCoord(0, 0), 64));
        }

        [Test]
        public void PlacementSet_DeduplicatesWorldFeatureIdentity()
        {
            var first = new WorldFeaturePlacement(3, new WorldPosition(10, 12), new ChunkCoord(0, 0), 2, 2);
            var duplicate = new WorldFeaturePlacement(3, new WorldPosition(10, 12), new ChunkCoord(0, 0), 2, 2);
            var differentFeature = new WorldFeaturePlacement(4, new WorldPosition(10, 12), new ChunkCoord(0, 0), 2, 2);
            var set = new WorldFeaturePlacementSet();

            Assert.IsTrue(set.Add(first));
            Assert.IsFalse(set.Add(duplicate));
            Assert.IsTrue(set.Add(differentFeature));
            Assert.AreEqual(2, set.Count);
        }

        private sealed class TestFeatureDefinition : IWorldFeaturePlacementDefinition
        {
            public int FeatureId { get; }
            public int Width { get; }
            public int Height { get; }
            public float SpawnChance { get; }
            public int MaxPerChunk { get; }
            public int MinimumDistanceFromOrigin { get; }

            public TestFeatureDefinition(
                int featureId,
                int width,
                int height,
                float spawnChance,
                int maxPerChunk,
                int minimumDistanceFromOrigin = 0)
            {
                FeatureId = featureId;
                Width = width;
                Height = height;
                SpawnChance = spawnChance;
                MaxPerChunk = maxPerChunk;
                MinimumDistanceFromOrigin = minimumDistanceFromOrigin;
            }
        }
    }
}
