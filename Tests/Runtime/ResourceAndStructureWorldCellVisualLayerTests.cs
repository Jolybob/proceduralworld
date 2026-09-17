using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Jolybob.ProceduralWorld.Tilemap;

namespace Jolybob.ProceduralWorld.Tests
{
    public class ResourceAndStructureWorldCellVisualLayerTests
    {
        private readonly List<Tile> createdTiles = new List<Tile>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdTiles.Count; i++)
                UnityEngine.Object.DestroyImmediate(createdTiles[i]);
            createdTiles.Clear();
        }

        [Test]
        public void ResourceLayerResolvesOnlyResourceCells()
        {
            var resourceTile = CreateTile("Resource");
            var layer = new ResourceWorldCellVisualLayer(
                new Dictionary<ResourceId, TileBase> { { new ResourceId(1), resourceTile } });

            var cell = new GeneratedCell(WorldTile.Deep, 0);
            cell.SetResource(new ResourceId(1));

            TileBase resolved;
            Assert.IsTrue(layer.TryResolve(cell, out resolved));
            Assert.AreSame(resourceTile, resolved);

            var emptyCell = new GeneratedCell(WorldTile.Deep, 0);
            Assert.IsFalse(layer.TryResolve(emptyCell, out resolved));
            Assert.IsNull(resolved);
        }

        [Test]
        public void StructureLayerResolvesOnlyStructureCells()
        {
            var structureTile = CreateTile("Structure");
            var layer = new StructureWorldCellVisualLayer(
                new Dictionary<StructureId, TileBase> { { new StructureId(2), structureTile } });

            var cell = new GeneratedCell(WorldTile.Deep, 0);
            cell.SetStructure(new StructureId(2));

            TileBase resolved;
            Assert.IsTrue(layer.TryResolve(cell, out resolved));
            Assert.AreSame(structureTile, resolved);

            var emptyCell = new GeneratedCell(WorldTile.Deep, 0);
            Assert.IsFalse(layer.TryResolve(emptyCell, out resolved));
            Assert.IsNull(resolved);
        }

        [Test]
        public void UnmappedResourceFallsThrough()
        {
            var layer = new ResourceWorldCellVisualLayer(
                new Dictionary<ResourceId, TileBase> { { new ResourceId(1), CreateTile("Resource") } });
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            cell.SetResource(new ResourceId(2));

            TileBase resolved;
            Assert.IsFalse(layer.TryResolve(cell, out resolved));
            Assert.IsNull(resolved);
        }

        [Test]
        public void ResourceAndStructureOrdersAreDeterministic()
        {
            var resourceLayer = new ResourceWorldCellVisualLayer(new Dictionary<ResourceId, TileBase>());
            var structureLayer = new StructureWorldCellVisualLayer(new Dictionary<StructureId, TileBase>());

            Assert.Less(resourceLayer.Order, structureLayer.Order);
            Assert.AreEqual(200, resourceLayer.Order);
            Assert.AreEqual(300, structureLayer.Order);
        }

        private Tile CreateTile(string name)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            createdTiles.Add(tile);
            return tile;
        }
    }
}
