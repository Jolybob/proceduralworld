using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldPresentationDirtySetTests
    {
        [Test]
        public void MarkCoalescesRepeatedPositionAndPreservesFirstBefore()
        {
            var dirty = new WorldPresentationDirtySet();
            var position = new WorldPosition(4, -2);
            var first = new GeneratedCell(WorldTile.Deep, 0);
            var middle = new GeneratedCell(WorldTile.Empty, 0);
            var latest = new GeneratedCell(WorldTile.Deep, 0);

            dirty.Mark(new WorldCellChange(position, first, middle, WorldEditOperationKind.SetTile));
            dirty.Mark(new WorldCellChange(position, middle, latest, WorldEditOperationKind.SetTile));

            WorldChangeBatch batch = dirty.Drain();

            Assert.AreEqual(1, batch.Count);
            Assert.AreEqual(first, batch.Changes[0].Before);
            Assert.AreEqual(latest, batch.Changes[0].After);
            Assert.AreEqual(WorldEditOperationKind.SetTile, batch.Changes[0].Operation);
        }

        [Test]
        public void DrainPreservesFirstSeenPositionOrder()
        {
            var dirty = new WorldPresentationDirtySet();
            var firstPosition = new WorldPosition(1, 1);
            var secondPosition = new WorldPosition(2, 2);
            var thirdPosition = new WorldPosition(3, 3);
            var cell = new GeneratedCell(WorldTile.Deep, 0);

            dirty.Mark(new WorldCellChange(firstPosition, cell, cell, WorldEditOperationKind.SetCell));
            dirty.Mark(new WorldCellChange(secondPosition, cell, cell, WorldEditOperationKind.SetCell));
            dirty.Mark(new WorldCellChange(firstPosition, cell, cell, WorldEditOperationKind.SetTile));
            dirty.Mark(new WorldCellChange(thirdPosition, cell, cell, WorldEditOperationKind.SetCell));

            WorldChangeBatch batch = dirty.Drain();

            Assert.AreEqual(3, batch.Count);
            Assert.AreEqual(firstPosition, batch.Changes[0].Position);
            Assert.AreEqual(secondPosition, batch.Changes[1].Position);
            Assert.AreEqual(thirdPosition, batch.Changes[2].Position);
            Assert.AreEqual(WorldEditOperationKind.SetTile, batch.Changes[0].Operation);
        }

        [Test]
        public void DrainClearsDirtySet()
        {
            var dirty = new WorldPresentationDirtySet();
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            dirty.Mark(new WorldCellChange(
                new WorldPosition(8, 9),
                cell,
                cell,
                WorldEditOperationKind.SetCell));

            WorldChangeBatch batch = dirty.Drain();

            Assert.AreEqual(1, batch.Count);
            Assert.AreEqual(0, dirty.Count);
            Assert.AreEqual(0, dirty.Drain().Count);
        }

        [Test]
        public void MarkBatchCoalescesAllChangesByPosition()
        {
            var dirty = new WorldPresentationDirtySet();
            var position = new WorldPosition(6, 7);
            var otherPosition = new WorldPosition(9, 10);
            var first = new GeneratedCell(WorldTile.Deep, 0);
            var latest = new GeneratedCell(WorldTile.Empty, 0);
            var other = new GeneratedCell(WorldTile.Deep, 0);

            dirty.MarkBatch(new WorldChangeBatch(new[]
            {
                new WorldCellChange(position, first, latest, WorldEditOperationKind.SetTile),
                new WorldCellChange(otherPosition, first, other, WorldEditOperationKind.SetTile),
                new WorldCellChange(position, latest, other, WorldEditOperationKind.SetTile)
            }));

            Assert.AreEqual(2, dirty.Count);
            WorldChangeBatch batch = dirty.Drain();
            Assert.AreEqual(2, batch.Count);
            Assert.AreEqual(position, batch.Changes[0].Position);
            Assert.AreEqual(first, batch.Changes[0].Before);
            Assert.AreEqual(other, batch.Changes[0].After);
            Assert.AreEqual(otherPosition, batch.Changes[1].Position);
        }

        [Test]
        public void MarkBatchRejectsNull()
        {
            var dirty = new WorldPresentationDirtySet();
            Assert.Throws<System.ArgumentNullException>(() => dirty.MarkBatch(null));
        }
    }
}
