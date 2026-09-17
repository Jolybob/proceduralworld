using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class StructurePlacementTests
    {
        [Test]
        public void Planner_CanProducePlacementThatCrossesChunkBoundary()
        {
            var definition = new StructureDefinition(
                new StructureId(7),
                "Boundary Shrine",
                new RegionId(1),
                new TerrainId(1),
                1f,
                1,
                3,
                3);
            var planner = new StructurePlacementPlanner(1234);

            StructurePlacement placement = planner.CreatePlacement(
                new ChunkCoord(0, 0),
                definition,
                anchorX: 63,
                anchorY: 2);

            Assert.AreEqual(new WorldPosition(63, 2), placement.Anchor);
            Assert.AreEqual(new ChunkCoord(0, 0), placement.OwnerChunk);
            Assert.IsTrue(placement.Intersects(new ChunkCoord(1, 0), 64));
            Assert.IsTrue(placement.Contains(new WorldPosition(64, 2)));
        }

        [Test]
        public void Placement_IntersectsNegativeChunkCoordinates()
        {
            var definition = new StructureDefinition(
                new StructureId(7),
                "Boundary Shrine",
                new RegionId(1),
                new TerrainId(1),
                1f,
                1,
                5,
                5);
            var placement = new StructurePlacement(
                definition,
                new WorldPosition(-65, -65),
                new ChunkCoord(-2, -2));

            Assert.IsTrue(placement.Intersects(new ChunkCoord(-2, -2), 64));
            Assert.IsTrue(placement.Intersects(new ChunkCoord(-1, -1), 64));
            Assert.IsFalse(placement.Intersects(new ChunkCoord(0, 0), 64));
        }

        [Test]
        public void Pass_StampsOnlyTheChunkLocalIntersection()
        {
            var definition = new StructureDefinition(
                new StructureId(7),
                "Boundary Shrine",
                new RegionId(1),
                new TerrainId(1),
                0f,
                1,
                3,
                3);
            var placements = new StructurePlacementSet();
            placements.Add(new StructurePlacement(
                definition,
                new WorldPosition(63, 2),
                new ChunkCoord(0, 0)));

            var regions = new RegionCatalog(new[]
            {
                new RegionDefinition(new RegionId(1), "Test Region", new TerrainId(1))
            });
            var terrains = new TerrainCatalog(new[]
            {
                new TerrainDefinition(new TerrainId(1), "Test Terrain", WorldTile.Deep)
            });
            var structures = new StructureCatalog(new[] { definition });
            var settings = new WorldGenerationSettings
            {
                chunkSize = 64,
                macroRegionsEnabled = false,
                cavesEnabled = false,
                liquidsEnabled = false,
                chasmsEnabled = false,
                resourcesEnabled = false,
                structuresEnabled = true
            };

            var pipeline = new WorldGenerationPipeline()
                .Add(new RegionBiomePass(new ConstantRegionResolver(new RegionId(1))))
                .Add(new TerrainPass(regions, terrains))
                .Add(new StructurePlacementPass(structures, new StaticStructurePlacementSource(placements)));

            var generator = new ProceduralWorldGenerator(
                1234,
                settings,
                pipeline,
                regions: regions,
                terrains: terrains,
                structures: structures);

            GeneratedChunk chunk = generator.GenerateChunk(new ChunkCoord(0, 0));

            Assert.AreEqual(new StructureId(7), chunk.GetCell(63, 2).Structure);
            Assert.AreEqual(new StructureId(7), chunk.GetCell(63, 4).Structure);
            Assert.AreEqual(default(StructureId), chunk.GetCell(62, 2).Structure);
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

        private sealed class StaticStructurePlacementSource : IStructurePlacementSource
        {
            private readonly StructurePlacementSet placements;

            public StaticStructurePlacementSource(StructurePlacementSet placements)
            {
                this.placements = placements;
            }

            public void Collect(WorldGenerationContext context, StructurePlacementSet output)
            {
                foreach (StructurePlacement placement in placements.Placements)
                {
                    if (placement.Intersects(context.ChunkCoordinate, context.Chunk.Size))
                        output.Add(placement);
                }
            }
        }
    }
}
