using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class ProceduralWorldGeneratorTests
    {
        [Test]
        public void SameSeedAndChunkProduceSameData()
        {
            var settings = new WorldGenerationSettings { chunkSize = 16 };
            var a = new ProceduralWorldGenerator(24680, settings)
                .GenerateChunk(new ChunkCoord(3, -2));
            var b = new ProceduralWorldGenerator(24680, settings)
                .GenerateChunk(new ChunkCoord(3, -2));

            Assert.AreEqual(a.Cells.Length, b.Cells.Length);
            for (int i = 0; i < a.Cells.Length; i++)
            {
                Assert.AreEqual(a.Cells[i].Region, b.Cells[i].Region);
                Assert.AreEqual(a.Cells[i].Terrain, b.Cells[i].Terrain);
                Assert.AreEqual(a.Cells[i].Resource, b.Cells[i].Resource);
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
                if (a.Cells[i].Tile != b.Cells[i].Tile || a.Cells[i].Region != b.Cells[i].Region)
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
                new RegionDefinition(new RegionId(7), "Test Region", new TerrainId(9))
            });
            var terrains = new TerrainCatalog(new[]
            {
                new TerrainDefinition(new TerrainId(9), "Test Terrain", WorldTile.Core)
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
                Assert.AreEqual(new RegionId(7), chunk.Cells[i].Region);
                Assert.AreEqual(new TerrainId(9), chunk.Cells[i].Terrain);
                Assert.AreEqual(WorldTile.Core, chunk.Cells[i].Tile);
            }
        }

        [Test]
        public void LegacyBiomeWritesAreAcceptedByTerrainPass()
        {
            var chunk = new GeneratedChunk(new ChunkCoord(0, 0), 1);
            var cell = chunk.GetCell(0, 0);
            cell.Biome = 4;
            chunk.SetCell(0, 0, cell);

            new TerrainPass().Execute(new WorldGenerationContext(
                123,
                new WorldGenerationSettings { chunkSize = 1 },
                new ChunkCoord(0, 0),
                chunk,
                new SeededPerlinNoiseField(123, 0.02f)));

            cell = chunk.GetCell(0, 0);
            Assert.AreEqual(new RegionId(4), cell.Region);
            Assert.AreEqual(new TerrainId(2), cell.Terrain);
            Assert.AreEqual((byte)4, cell.Biome);
            Assert.AreEqual(WorldTile.Mid, cell.Tile);
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
        public void RandomStreamIsDeterministicForSameSeedChunkAndDomain()
        {
            var serviceA = new WorldRandomService(24680);
            var serviceB = new WorldRandomService(24680);
            var chunk = new ChunkCoord(-3, 7);
            var a = serviceA.Create(chunk, WorldRandomDomain.Resources, 1u);
            var b = serviceB.Create(chunk, WorldRandomDomain.Resources, 1u);

            for (int i = 0; i < 32; i++)
            {
                Assert.AreEqual(a.NextUInt(), b.NextUInt());
                Assert.AreEqual(a.NextInt(0, 1000), b.NextInt(0, 1000));
                Assert.AreEqual(a.NextFloat01(), b.NextFloat01());
                Assert.AreEqual(a.Chance(0.35f), b.Chance(0.35f));
            }
        }

        [Test]
        public void RandomResourceStreamsAreIndependent()
        {
            var service = new WorldRandomService(24680);
            var crystal = service.Create(new ChunkCoord(1, 2), WorldRandomDomain.Resources, 1u);
            var ore = service.Create(new ChunkCoord(1, 2), WorldRandomDomain.Resources, 2u);

            bool foundDifference = false;
            for (int i = 0; i < 8; i++)
            {
                if (crystal.NextUInt() != ore.NextUInt())
                {
                    foundDifference = true;
                    break;
                }
            }

            Assert.IsTrue(foundDifference);
        }

        [Test]
        public void GeneratedCellHelperMethodsKeepCompatibilityMirrorsInSync()
        {
            var cell = new GeneratedCell(WorldTile.Deep, 1);
            cell.SetRegion(new RegionId(8));
            cell.SetTerrain(new TerrainId(12), WorldTile.Core);
            cell.SetResource(new ResourceId(6));

            Assert.AreEqual(new RegionId(8), cell.Region);
            Assert.AreEqual((byte)8, cell.Biome);
            Assert.AreEqual(new TerrainId(12), cell.Terrain);
            Assert.AreEqual(new ResourceId(6), cell.Resource);
            Assert.IsTrue((cell.Flags & GeneratedCellFlags.HasResource) != 0);

            cell.ClearResource();
            Assert.AreEqual(default(ResourceId), cell.Resource);
            Assert.IsFalse((cell.Flags & GeneratedCellFlags.HasResource) != 0);
        }

        [Test]
        public void ResourcesAreDisabledByDefault()
        {
            var settings = new WorldGenerationSettings { chunkSize = 32 };
            var chunk = new ProceduralWorldGenerator(24680, settings)
                .GenerateChunk(new ChunkCoord(0, 0));

            for (int i = 0; i < chunk.Cells.Length; i++)
                Assert.IsFalse((chunk.Cells[i].Flags & GeneratedCellFlags.HasResource) != 0);
        }

        [Test]
        public void CustomResourceCatalogCanPlaceDeterministicResources()
        {
            var settings = new WorldGenerationSettings
            {
                chunkSize = 4,
                resourcesEnabled = true
            };
            var regions = new RegionCatalog(new[]
            {
                new RegionDefinition(new RegionId(7), "Resource Region", new TerrainId(9))
            });
            var terrains = new TerrainCatalog(new[]
            {
                new TerrainDefinition(new TerrainId(9), "Resource Terrain", WorldTile.Core)
            });
            var resources = new ResourceCatalog(new[]
            {
                new ResourceDefinition(
                    new ResourceId(7),
                    "Test Resource",
                    new RegionId(7),
                    new TerrainId(9),
                    1f,
                    2)
            });
            var pipeline = new WorldGenerationPipeline()
                .Add(new RegionBiomePass(new ConstantRegionResolver(new RegionId(7))))
                .Add(new TerrainPass(regions, terrains))
                .Add(new ResourcePass(resources));

            var a = new ProceduralWorldGenerator(
                24680, settings, pipeline, null, null, regions, terrains, resources)
                .GenerateChunk(new ChunkCoord(0, 0));
            var b = new ProceduralWorldGenerator(
                24680, settings, pipeline, null, null, regions, terrains, resources)
                .GenerateChunk(new ChunkCoord(0, 0));

            int resourceCount = 0;
            for (int i = 0; i < a.Cells.Length; i++)
            {
                Assert.AreEqual(a.Cells[i].Resource, b.Cells[i].Resource);
                if ((a.Cells[i].Flags & GeneratedCellFlags.HasResource) != 0)
                {
                    Assert.AreEqual(new ResourceId(7), a.Cells[i].Resource);
                    resourceCount++;
                }
            }

            Assert.AreEqual(2, resourceCount);
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
    }
}
