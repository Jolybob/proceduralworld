using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class ResourceDepositTests
    {
        [Test]
        public void ResourceDefinition_DefaultsToSingleCellDeposit()
        {
            var definition = new ResourceDefinition(
                new ResourceId(9),
                "Test Ore",
                new RegionId(1),
                new TerrainId(1),
                1f,
                1);

            Assert.AreEqual((byte)1, definition.DepositSizeMin);
            Assert.AreEqual((byte)1, definition.DepositSizeMax);
            Assert.AreEqual(0f, definition.DepositGrowthChance);
        }

        [Test]
        public void ResourcePass_GrowsDeterministicDeposit()
        {
            var regions = new RegionCatalog(new[]
            {
                new RegionDefinition(new RegionId(1), "Test Region", new TerrainId(1))
            });
            var terrains = new TerrainCatalog(new[]
            {
                new TerrainDefinition(new TerrainId(1), "Test Terrain", WorldTile.Deep)
            });
            var resources = new ResourceCatalog(new[]
            {
                new ResourceDefinition(
                    new ResourceId(9),
                    "Test Ore",
                    new RegionId(1),
                    new TerrainId(1),
                    1f,
                    1,
                    0,
                    5,
                    5,
                    1f)
            });
            var settings = new WorldGenerationSettings
            {
                chunkSize = 8,
                macroRegionsEnabled = false,
                cavesEnabled = false,
                liquidsEnabled = false,
                chasmsEnabled = false,
                resourcesEnabled = true,
                structuresEnabled = false
            };

            var generator = new ProceduralWorldGenerator(
                12345,
                settings,
                null,
                null,
                null,
                regions,
                terrains,
                resources,
                null,
                null,
                null,
                new ConstantRegionLayout(new RegionId(1)));

            GeneratedChunk first = generator.GenerateChunk(new ChunkCoord(3, -2));
            GeneratedChunk second = generator.GenerateChunk(new ChunkCoord(3, -2));

            int firstCount = CountResources(first, new ResourceId(9));
            int secondCount = CountResources(second, new ResourceId(9));

            Assert.AreEqual(5, firstCount);
            Assert.AreEqual(firstCount, secondCount);
        }

        private static int CountResources(GeneratedChunk chunk, ResourceId resource)
        {
            int count = 0;
            for (int y = 0; y < chunk.Size; y++)
            {
                for (int x = 0; x < chunk.Size; x++)
                {
                    if (chunk.GetCell(x, y).Resource == resource)
                        count++;
                }
            }

            return count;
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
