using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class ProceduralWorldGeneratorTests
    {
        [Test]
        public void SameSeedAndChunkProduceSameData()
        {
            var settings = new WorldGenerationSettings { chunkSize = 16 };
            var a = new ProceduralWorldGenerator(12345, settings)
                .GenerateChunk(new ChunkCoord(3, -2));
            var b = new ProceduralWorldGenerator(12345, settings)
                .GenerateChunk(new ChunkCoord(3, -2));

            Assert.AreEqual(a.Cells.Length, b.Cells.Length);
            for (int i = 0; i < a.Cells.Length; i++)
            {
                Assert.AreEqual(a.Cells[i].Tile, b.Cells[i].Tile);
                Assert.AreEqual(a.Cells[i].Biome, b.Cells[i].Biome);
                Assert.AreEqual(a.Cells[i].Flags, b.Cells[i].Flags);
            }
        }

        [Test]
        public void DifferentSeedsUsuallyProduceDifferentData()
        {
            var settings = new WorldGenerationSettings { chunkSize = 16 };
            var a = new ProceduralWorldGenerator(1, settings)
                .GenerateChunk(new ChunkCoord(0, 0));
            var b = new ProceduralWorldGenerator(2, settings)
                .GenerateChunk(new ChunkCoord(0, 0));

            bool foundDifference = false;
            for (int i = 0; i < a.Cells.Length; i++)
            {
                if (a.Cells[i].Tile != b.Cells[i].Tile ||
                    a.Cells[i].Biome != b.Cells[i].Biome ||
                    a.Cells[i].Flags != b.Cells[i].Flags)
                {
                    foundDifference = true;
                    break;
                }
            }

            Assert.IsTrue(foundDifference);
        }

        [Test]
        public void EnvironmentFieldProviderIsDeterministic()
        {
            var settings = new WorldGenerationSettings();
            var noise = new SeededPerlinNoiseField(
                123,
                settings.noiseScale,
                settings.noiseStrength,
                settings.noiseOctaves,
                settings.noisePersistence,
                settings.noiseLacunarity);
            var a = new DefaultEnvironmentFieldProvider(123, settings, noise).Sample(50, -20);
            var b = new DefaultEnvironmentFieldProvider(123, settings, noise).Sample(50, -20);

            Assert.AreEqual(a.Temperature, b.Temperature);
            Assert.AreEqual(a.Moisture, b.Moisture);
            Assert.AreEqual(a.Elevation, b.Elevation);
            Assert.AreEqual(a.Distance, b.Distance);
        }

        [Test]
        public void CustomEnvironmentFieldProviderCanDriveGeneration()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var fields = new ConstantEnvironmentFieldProvider(
                new EnvironmentSample(0.9f, 0.9f, 0.8f, 0.5f));

            var chunk = new ProceduralWorldGenerator(123, settings, null, fields)
                .GenerateChunk(new ChunkCoord(0, 0));

            for (int i = 0; i < chunk.Cells.Length; i++)
                Assert.AreEqual((byte)3, chunk.Cells[i].Biome);
        }

        [Test]
        public void CustomCatalogsDriveTerrainWithoutChangingGeneratedRegionIds()
        {
            var settings = new WorldGenerationSettings { chunkSize = 2 };
            var regions = new RegionCatalog(new[]
            {
                new RegionDefinition(
                    new RegionId(7),
                    "Test Region",
                    new TerrainId(9))
            });
            var terrains = new TerrainCatalog(new[]
            {
                new TerrainDefinition(
                    new TerrainId(9),
                    "Test Terrain",
                    WorldTile.Core)
            });
            var resolver = new ConstantRegionResolver(new RegionId(7));
            var pipeline = new WorldGenerationPipeline()
                .Add(new RegionBiomePass(resolver))
                .Add(new TerrainPass(regions, terrains));

            var chunk = new ProceduralWorldGenerator(
                123,
                settings,
                pipeline,
                new ConstantEnvironmentFieldProvider(new EnvironmentSample(0.5f, 0.5f, 0.5f, 0.5f)),
                regions,
                terrains)
                .GenerateChunk(new ChunkCoord(0, 0));

            for (int i = 0; i < chunk.Cells.Length; i++)
            {
                Assert.AreEqual((byte)7, chunk.Cells[i].Biome);
                Assert.AreEqual(WorldTile.Core, chunk.Cells[i].Tile);
            }
        }

        [Test]
        public void RegionCatalogRejectsDuplicateIds()
        {
            Assert.Throws<System.ArgumentException>(() => new RegionCatalog(new[]
            {
                new RegionDefinition(new RegionId(1), "A", new TerrainId(1)),
                new RegionDefinition(new RegionId(1), "B", new TerrainId(1))
            }));
        }

        [Test]
        public void CavePassIsDisabledByDefault()
        {
            var settings = new WorldGenerationSettings { chunkSize = 16 };
            var chunk = new ProceduralWorldGenerator(123, settings)
                .GenerateChunk(new ChunkCoord(2, 2));

            for (int i = 0; i < chunk.Cells.Length; i++)
            {
                Assert.AreEqual(GeneratedCellFlags.None, chunk.Cells[i].Flags);
                Assert.AreNotEqual(WorldTile.Empty, chunk.Cells[i].Tile);
            }
        }

        [Test]
        public void CavePassCanCarveUsingInjectedField()
        {
            var settings = new WorldGenerationSettings
            {
                chunkSize = 4,
                cavesEnabled = true,
                caveThreshold = 0.5f,
                caveMinimumDistance = 0f
            };

            var caveField = new ConstantCaveFieldProvider(1f);
            var generator = new ProceduralWorldGenerator(
                123,
                settings,
                null,
                null,
                caveField,
                null,
                null);

            var chunk = generator.GenerateChunk(new ChunkCoord(0, 0));

            for (int i = 0; i < chunk.Cells.Length; i++)
            {
                Assert.AreEqual(WorldTile.Empty, chunk.Cells[i].Tile);
                Assert.IsTrue((chunk.Cells[i].Flags & GeneratedCellFlags.Carved) != 0);
            }
        }

        private sealed class ConstantEnvironmentFieldProvider : IEnvironmentFieldProvider
        {
            private readonly EnvironmentSample sample;

            public ConstantEnvironmentFieldProvider(EnvironmentSample sample)
            {
                this.sample = sample;
            }

            public EnvironmentSample Sample(int worldX, int worldY)
            {
                return sample;
            }
        }

        private sealed class ConstantRegionResolver : IRegionResolver
        {
            private readonly RegionId region;

            public ConstantRegionResolver(RegionId region)
            {
                this.region = region;
            }

            public RegionId Resolve(EnvironmentSample sample)
            {
                return region;
            }
        }

        private sealed class ConstantCaveFieldProvider : ICaveFieldProvider
        {
            private readonly float value;

            public ConstantCaveFieldProvider(float value)
            {
                this.value = value;
            }

            public float Sample(int worldX, int worldY)
            {
                return value;
            }
        }
    }
}
