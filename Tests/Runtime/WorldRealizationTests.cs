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
        public void IntersectingQueryIsDeduplicatedAndCanonical()
        {
            var map = new WorldRealizationMap(4);
            var first = new WorldRealizationEdit("b", "node:b", "Tile", "Stone", 0, new WorldPosition(3, 3));
            var second = new WorldRealizationEdit("a", "node:a", "Object", "Chest", 1, new WorldPosition(4, 4));
            Assert.IsTrue(map.TryAdd(first, out WorldRealizationConflict ignoredFirst));
            Assert.IsTrue(map.TryAdd(second, out WorldRealizationConflict ignoredSecond));

            var results = new List<WorldRealizationEdit>();
            map.CollectIntersecting(new WorldPosition(0, 0), new WorldPosition(7, 7), results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("b", results[0].Id);
            Assert.AreEqual("a", results[1].Id);
        }
    }
}
