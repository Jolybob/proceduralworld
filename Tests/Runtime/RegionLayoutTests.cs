using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class RegionLayoutTests
    {
        [Test]
        public void RadialSectorResolver_ImplementsWorldSpaceLayout()
        {
            var catalog = new MacroRegionCatalog(new[]
            {
                new MacroRegionDefinition(
                    new RegionId(7),
                    10f,
                    100f,
                    0f,
                    1.0f,
                    priority: 5)
            });
            var resolver = new RadialSectorRegionResolver(
                catalog,
                seed: 1234,
                fallbackRegion: new RegionId(0),
                rotateBySeed: false);

            RegionId layoutResult = resolver.Resolve(new WorldPosition(50, 0));
            RegionId resolverResult = resolver.Resolve(default(EnvironmentSample), 50, 0);

            Assert.AreEqual(new RegionId(7), layoutResult);
            Assert.AreEqual(layoutResult, resolverResult);
        }

        [Test]
        public void RegionLayoutResolver_DelegatesWorldCoordinates()
        {
            var catalog = new MacroRegionCatalog(new[]
            {
                new MacroRegionDefinition(
                    new RegionId(3),
                    0f,
                    100f,
                    0f,
                    6.283185f,
                    priority: 1)
            });
            var layout = new RadialSectorRegionResolver(
                catalog,
                seed: 999,
                fallbackRegion: new RegionId(0),
                rotateBySeed: false);
            var resolver = new RegionLayoutResolver(layout, new RegionId(0));

            Assert.AreEqual(new RegionId(3), resolver.Resolve(default(EnvironmentSample), 25, 0));
            Assert.AreEqual(new RegionId(0), resolver.Resolve(default(EnvironmentSample)));
        }

        [Test]
        public void Generator_AcceptsCustomRegionResolverWithoutReplacingPipeline()
        {
            var regions = new RegionCatalog(new[]
            {
                new RegionDefinition(new RegionId(7), "Test Region", new TerrainId(1))
            });
            var terrains = new TerrainCatalog(new[]
            {
                new TerrainDefinition(new TerrainId(1), "Test Terrain", WorldTile.Deep)
            });
            var resolver = new RegionLayoutResolver(
                new ConstantRegionLayout(new RegionId(7)),
                new RegionId(0));
            var settings = new WorldGenerationSettings
            {
                chunkSize = 4,
                macroRegionsEnabled = false,
                cavesEnabled = false,
                liquidsEnabled = false,
                chasmsEnabled = false,
                resourcesEnabled = false,
                structuresEnabled = false
            };

            var generator = new ProceduralWorldGenerator(
                42,
                settings,
                null,
                null,
                null,
                regions,
                terrains,
                null,
                null,
                null,
                null,
                resolver);

            GeneratedChunk chunk = generator.GenerateChunk(new ChunkCoord(0, 0));
            GeneratedCell cell = chunk.GetCell(0, 0);

            Assert.AreEqual(new RegionId(7), cell.Region);
            Assert.AreEqual(new TerrainId(1), cell.Terrain);
            Assert.AreEqual(WorldTile.Deep, cell.Tile);
        }

        private sealed class ConstantRegionLayout : IRegionLayout
        {
            private readonly RegionId region;

            public ConstantRegionLayout(RegionId region)
            {
                this.region = region;
            }

            public RegionId Resolve(WorldPosition position)
            {
                return region;
            }
        }
    }
}
