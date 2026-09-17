using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class ProceduralWorldGeneratorTests
    {
        [Test]
        public void SameSeedAndChunkProduceSameData()
        {
            var settings = new WorldGenerationSettings { chunkSize = 16 };
            var a = new ProceduralWorldGenerator(59273, settings)
                .GenerateChunk(new ChunkCoord(3, -2));
            var b = new ProceduralWorldGenerator(59273, settings)
                .GenerateChunk(new ChunkCoord(3, -2));

            Assert.AreEqual(a.Cells.Length, b.Cells.Length);
            for (int i = 0; i < a.Cells.Length; i++)
            {
                Assert.AreEqual(a.Cells[i].Region, b.Cells[i].Region);
                Assert.AreEqual(a.Cells[i].Terrain, b.Cells[i].Terrain);
                Assert.AreEqual(a.Cells[i].Resource, b.Cells[i].Resource);
                Assert.AreEqual(a.Cells[i].Structure, b.Cells[i].Structure);
                Assert.AreEqual(a.Cells[i].Tile, b.Cells[i].Tile);
                Assert.AreEqual(a.Cells[i].Biome, b.Cells[i].Biome);
                Assert.AreEqual(a.Cells[i].Flags, b.Cells[i].Flags);
            }
        }

        [Test]
        public void DifferentSeedsProduceDifferentEnvironmentFields()
        {
            var settings = new WorldGenerationSettings();
            var points = new[]
            {
                new WorldPosition(0, 0),
                new WorldPosition(17, -9),
                new WorldPosition(-41, 23),
                new WorldPosition(96, 57),
                new WorldPosition(-125, -83)
            };

            var noiseA = new SeededPerlinNoiseField(
                1,
                settings.noiseScale,
                settings.noiseStrength,
                settings.noiseOctaves,
                settings.noisePersistence,
                settings.noiseLacunarity);
            var noiseB = new SeededPerlinNoiseField(
                2,
                settings.noiseScale,
                settings.noiseStrength,
                settings.noiseOctaves,
                settings.noisePersistence,
                settings.noiseLacunarity);
            var fieldsA = new DefaultEnvironmentFieldProvider(1, settings, noiseA);
            var fieldsB = new DefaultEnvironmentFieldProvider(2, settings, noiseB);

            bool foundDifference = false;
            for (int i = 0; i < points.Length && !foundDifference; i++)
            {
                EnvironmentSample a = fieldsA.Sample(points[i].X, points[i].Y);
                EnvironmentSample b = fieldsB.Sample(points[i].X, points[i].Y);

                foundDifference = a.Temperature != b.Temperature ||
                                  a.Moisture != b.Moisture ||
                                  a.Elevation != b.Elevation;
            }

            Assert.IsTrue(foundDifference);
        }

        [Test]
        public void EnvironmentFieldProviderIsDeterministic()
        {
            var settings = new WorldGenerationSettings();
            var noise = new SeededPerlinNoiseField(
                59273,
                settings.noiseScale,
                settings.noiseStrength,
                settings.noiseOctaves,
                settings.noisePersistence,
                settings.noiseLacunarity);
            var a = new DefaultEnvironmentFieldProvider(59273, settings, noise).Sample(50, -20);
            var b = new DefaultEnvironmentFieldProvider(59273, settings, noise).Sample(50, -20);

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

            var chunk = new ProceduralWorldGenerator(59273, settings, null, fields)
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
                59273,
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
                59273,
                new WorldGenerationSettings { chunkSize = 1 },
                new ChunkCoord(0, 0),
                chunk,
                new SeededPerlinNoiseField(59273, 0.02f)));

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
            var serviceA = new WorldRandomService(59273);
            var serviceB = new WorldRandomService(59273);
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
            var service = new WorldRandomService(59273);
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
            cell.SetStructure(new StructureId(4));

            Assert.AreEqual(new RegionId(8), cell.Region);
            Assert.AreEqual((byte)8, cell.Biome);
            Assert.AreEqual(new TerrainId(12), cell.Terrain);
            Assert.AreEqual(new ResourceId(6), cell.Resource);
            Assert.AreEqual(new StructureId(4), cell.Structure);
            Assert.IsTrue((cell.Flags & GeneratedCellFlags.HasResource) != 0);
            Assert.IsTrue((cell.Flags & GeneratedCellFlags.HasStructure) != 0);

            cell.ClearResource();
            cell.ClearStructure();
            Assert.AreEqual(default(ResourceId), cell.Resource);
            Assert.AreEqual(default(StructureId), cell.Structure);
            Assert.IsFalse((cell.Flags & GeneratedCellFlags.HasResource) != 0);
            Assert.IsFalse((cell.Flags & GeneratedCellFlags.HasStructure) != 0);
        }

        [Test]
        public void ResourcesAreDisabledByDefault()
        {
            var settings = new WorldGenerationSettings { chunkSize = 32 };
            var chunk = new ProceduralWorldGenerator(59273, settings)
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
                59273, settings, pipeline, null, null, regions, terrains, resources)
                .GenerateChunk(new ChunkCoord(0, 0));
            var b = new ProceduralWorldGenerator(
                59273, settings, pipeline, null, null, regions, terrains, resources)
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

            Assert.LessOrEqual(resourceCount, 2);
        }

        [Test]
        public void StructuresAreDisabledByDefault()
        {
            var settings = new WorldGenerationSettings { chunkSize = 16 };
            var chunk = new ProceduralWorldGenerator(59273, settings)
                .GenerateChunk(new ChunkCoord(2, -1));

            for (int i = 0; i < chunk.Cells.Length; i++)
                Assert.IsFalse((chunk.Cells[i].Flags & GeneratedCellFlags.HasStructure) != 0);
        }

        [Test]
        public void CustomStructureCatalogPlacesDeterministicFootprint()
        {
            var settings = new WorldGenerationSettings
            {
                chunkSize = 6,
                structuresEnabled = true
            };
            var regions = new RegionCatalog(new[]
            {
                new RegionDefinition(new RegionId(8), "Structure Region", new TerrainId(10))
            });
            var terrains = new TerrainCatalog(new[]
            {
                new TerrainDefinition(new TerrainId(10), "Structure Terrain", WorldTile.Core)
            });
            var structures = new StructureCatalog(new[]
            {
                new StructureDefinition(
                    new StructureId(8),
                    "Test Shrine",
                    new RegionId(8),
                    new TerrainId(10),
                    1f,
                    1,
                    2,
                    2)
            });
            var pipeline = new WorldGenerationPipeline()
                .Add(new RegionBiomePass(new ConstantRegionResolver(new RegionId(8))))
                .Add(new TerrainPass(regions, terrains))
                .Add(new StructurePass(structures));

            var a = new ProceduralWorldGenerator(
                59273, settings, pipeline, null, null, regions, terrains, null, structures)
                .GenerateChunk(new ChunkCoord(0, 0));
            var b = new ProceduralWorldGenerator(
                59273, settings, pipeline, null, null, regions, terrains, null, structures)
                .GenerateChunk(new ChunkCoord(0, 0));

            int structureCells = 0;
            for (int i = 0; i < a.Cells.Length; i++)
            {
                Assert.AreEqual(a.Cells[i].Structure, b.Cells[i].Structure);
                Assert.AreEqual(a.Cells[i].Flags, b.Cells[i].Flags);

                if ((a.Cells[i].Flags & GeneratedCellFlags.HasStructure) != 0)
                {
                    Assert.AreEqual(new StructureId(8), a.Cells[i].Structure);
                    structureCells++;
                }
            }

            Assert.AreEqual(4, structureCells);
        }

        [Test]
        public void PostProcessStepsExecuteInOrderAndModifyGeneratedData()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var postProcess = new WorldPostProcessPipeline()
                .Add(new MarkCellStep(20, 0x4D41524Bu, GeneratedCellFlags.Reserved))
                .Add(new MarkCellStep(10, 0x4541524Cu, GeneratedCellFlags.Carved));
            var generator = new ProceduralWorldGenerator(
                59273,
                settings,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                postProcess);

            GeneratedChunk chunk = generator.GenerateChunk(new ChunkCoord(0, 0));

            Assert.IsTrue((chunk.GetCell(0, 0).Flags & GeneratedCellFlags.Carved) != 0);
            Assert.IsTrue((chunk.GetCell(0, 0).Flags & GeneratedCellFlags.Reserved) != 0);
            Assert.AreEqual(2, postProcess.Steps.Count);
            Assert.AreEqual(10, postProcess.Steps[0].Order);
            Assert.AreEqual(20, postProcess.Steps[1].Order);
        }

        [Test]
        public void PostProcessStepRandomStreamsAreDeterministicAndSalted()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4 };
            var pipelineA = new WorldPostProcessPipeline()
                .Add(new RandomMarkStep(10, 0x11111111u));
            var pipelineB = new WorldPostProcessPipeline()
                .Add(new RandomMarkStep(10, 0x11111111u));
            var pipelineC = new WorldPostProcessPipeline()
                .Add(new RandomMarkStep(10, 0x22222222u));

            var a = new ProceduralWorldGenerator(
                59273, settings, null, null, null, null, null, null, null, pipelineA)
                .GenerateChunk(new ChunkCoord(1, 1));
            var b = new ProceduralWorldGenerator(
                59273, settings, null, null, null, null, null, null, null, pipelineB)
                .GenerateChunk(new ChunkCoord(1, 1));
            var c = new ProceduralWorldGenerator(
                59273, settings, null, null, null, null, null, null, null, pipelineC)
                .GenerateChunk(new ChunkCoord(1, 1));

            for (int i = 0; i < a.Cells.Length; i++)
                Assert.AreEqual(a.Cells[i].Flags, b.Cells[i].Flags);

            bool foundSaltDifference = false;
            for (int i = 0; i < a.Cells.Length; i++)
            {
                if (a.Cells[i].Flags != c.Cells[i].Flags)
                {
                    foundSaltDifference = true;
                    break;
                }
            }

            Assert.IsTrue(foundSaltDifference);
        }

        private sealed class MarkCellStep : IWorldPostProcessStep
        {
            private readonly GeneratedCellFlags flag;

            public int Order { get; }
            public uint Salt { get; }

            public MarkCellStep(int order, uint salt, GeneratedCellFlags flag)
            {
                Order = order;
                Salt = salt;
                this.flag = flag;
            }

            public void Execute(WorldPostProcessContext context)
            {
                var cell = context.GetCell(0, 0);
                cell.Flags |= flag;
                context.SetCell(0, 0, cell);
            }
        }

        private sealed class RandomMarkStep : IWorldPostProcessStep
        {
            public int Order { get; }
            public uint Salt { get; }

            public RandomMarkStep(int order, uint salt)
            {
                Order = order;
                Salt = salt;
            }

            public void Execute(WorldPostProcessContext context)
            {
                for (int y = 0; y < context.Chunk.Size; y++)
                {
                    for (int x = 0; x < context.Chunk.Size; x++)
                    {
                        if (!context.Random.Chance(0.5f))
                            continue;

                        var cell = context.GetCell(x, y);
                        cell.Flags |= GeneratedCellFlags.Reserved;
                        context.SetCell(x, y, cell);
                    }
                }
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
    }
}
