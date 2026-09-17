using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Jolybob.ProceduralWorld.Tilemap;

namespace Jolybob.ProceduralWorld.Tests
{
    public class ResourceStructureVisualLayerTests
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
        public void ResourceLayerResolvesOnlyFlaggedResource()
        {
            var expected = CreateTile("Resource");
            var layer = new ResourceWorldCellVisualLayer(
                new Dictionary<ResourceId, TileBase> { { new ResourceId(7), expected } });
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            cell.SetResource(new ResourceId(7));

            TileBase tile;
            Assert.IsTrue(layer.TryResolve(cell, out tile));
            Assert.AreSame(expected, tile);
        }

        [Test]
        public void ResourceLayerFallsThroughForUnmappedResource()
        {
            var layer = new ResourceWorldCellVisualLayer(
                new Dictionary<ResourceId, TileBase>());
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            cell.SetResource(new ResourceId(7));

            TileBase tile;
            Assert.IsFalse(layer.TryResolve(cell, out tile));
            Assert.IsNull(tile);
        }

        [Test]
        public void StructureLayerResolvesOnlyFlaggedStructure()
        {
            var expected = CreateTile("Structure");
            var layer = new StructureWorldCellVisualLayer(
                new Dictionary<StructureId, TileBase> { { new StructureId(9), expected } });
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            cell.SetStructure(new StructureId(9));

            TileBase tile;
            Assert.IsTrue(layer.TryResolve(cell, out tile));
            Assert.AreSame(expected, tile);
        }

        [Test]
        public void StructureLayerFallsThroughForUnmappedStructure()
        {
            var layer = new StructureWorldCellVisualLayer(
                new Dictionary<StructureId, TileBase>());
            var cell = new GeneratedCell(WorldTile.Deep, 0);
            cell.SetStructure(new StructureId(9));

            TileBase tile;
            Assert.IsFalse(layer.TryResolve(cell, out tile));
            Assert.IsNull(tile);
        }

        [Test]
        public void ResourceAndStructureLayersHaveDeterministicDefaultOrder()
        {
            var resource = new ResourceWorldCellVisualLayer(new Dictionary<ResourceId, TileBase>());
            var structure = new StructureWorldCellVisualLayer(new Dictionary<StructureId, TileBase>());

            Assert.Less(resource.Order, structure.Order);
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
