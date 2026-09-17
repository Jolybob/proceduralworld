using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class LiquidTopologyTests
    {
        private sealed class ConstantEnvironmentProvider : IEnvironmentFieldProvider
        {
            private readonly EnvironmentSample sample;

            public ConstantEnvironmentProvider(float temperature, float moisture, float elevation)
            {
                sample = new EnvironmentSample(temperature, moisture, elevation, 0f);
            }

            public EnvironmentSample Sample(int worldX, int worldY) => sample;
        }

        [Test]
        public void LiquidTopologyIsDisabledByDefault()
        {
            var settings = new WorldGenerationSettings { chunkSize = 4, liquidsEnabled = false };
            var chunk = CreateChunk(settings.chunkSize);
            var context = CreateContext(settings, chunk, new ConstantEnvironmentProvider(1f, 1f, 0f));

            new LiquidTopologyPass(settings).Execute(context);

            for (int i = 0; i < chunk.Cells.Length; i++)
                Assert.AreEqual(CellTopology.Solid, chunk.Cells[i].Topology);
        }

        [Test]
        public void LowWetCellsBecomeWater()
        {
            var settings = new WorldGenerationSettings
            {
                chunkSize = 4,
                liquidsEnabled = true,
                waterElevationThreshold = 0.4f,
                waterMoistureThreshold = 0.6f,
                lavaElevationThreshold = 0.8f,
                lavaHeatThreshold = 0.7f
            };
            var chunk = CreateChunk(settings.chunkSize);
            var context = CreateContext(settings, chunk, new ConstantEnvironmentProvider(0.2f, 0.9f, 0.3f));

            new LiquidTopologyPass(settings).Execute(context);

            for (int i = 0; i < chunk.Cells.Length; i++)
                Assert.AreEqual(CellTopology.Water, chunk.Cells[i].Topology);
        }

        [Test]
        public void HotHighCellsBecomeLava()
        {
            var settings = new WorldGenerationSettings
            {
                chunkSize = 4,
                liquidsEnabled = true,
                lavaElevationThreshold = 0.8f,
                lavaHeatThreshold = 0.7f
            };
            var chunk = CreateChunk(settings.chunkSize);
            var context = CreateContext(settings, chunk, new ConstantEnvironmentProvider(0.95f, 0.1f, 0.9f));

            new LiquidTopologyPass(settings).Execute(context);

            for (int i = 0; i < chunk.Cells.Length; i++)
                Assert.AreEqual(CellTopology.Lava, chunk.Cells[i].Topology);
        }

        [Test]
        public void ExistingNonSolidTopologyIsPreserved()
        {
            var settings = new WorldGenerationSettings
            {
                chunkSize = 2,
                liquidsEnabled = true,
                waterElevationThreshold = 0.9f,
                waterMoistureThreshold = 0.1f
            };
            var chunk = CreateChunk(settings.chunkSize);
            var water = new GeneratedCell(WorldTile.Deep, 1);
            water.SetTopology(CellTopology.Water, WorldTile.Deep);
            chunk.SetCell(0, 0, water);
            var context = CreateContext(settings, chunk, new ConstantEnvironmentProvider(0f, 1f, 0f));

            new LiquidTopologyPass(settings).Execute(context);

            Assert.AreEqual(CellTopology.Water, chunk.GetCell(0, 0).Topology);
            Assert.AreEqual(CellTopology.Water, chunk.GetCell(1, 1).Topology);
        }

        [Test]
        public void LiquidTopologyIsDeterministicAcrossChunks()
        {
            var settings = new WorldGenerationSettings { chunkSize = 8, liquidsEnabled = true };
            var generator = new ProceduralWorldGenerator(90417, settings);

            GeneratedChunk first = generator.GenerateChunk(new ChunkCoord(3, -2));
            GeneratedChunk second = generator.GenerateChunk(new ChunkCoord(3, -2));

            Assert.AreEqual(first.Cells.Length, second.Cells.Length);
            for (int i = 0; i < first.Cells.Length; i++)
            {
                Assert.AreEqual(first.Cells[i].Topology, second.Cells[i].Topology);
                Assert.AreEqual(first.Cells[i].Tile, second.Cells[i].Tile);
            }
        }

        private static GeneratedChunk CreateChunk(int size)
        {
            var chunk = new GeneratedChunk(new ChunkCoord(0, 0), size);
            for (int i = 0; i < chunk.Cells.Length; i++)
                chunk.Cells[i] = new GeneratedCell(WorldTile.Deep, 1);
            return chunk;
        }

        private static WorldGenerationContext CreateContext(
            WorldGenerationSettings settings,
            GeneratedChunk chunk,
            IEnvironmentFieldProvider environment)
        {
            var noise = new SeededPerlinNoiseField(1234, 0.02f);
            return new WorldGenerationContext(
                1234,
                settings,
                chunk.Coordinate,
                chunk,
                noise,
                environment,
                new DefaultCaveFieldProvider(1234, settings),
                new WorldRandomService(1234),
                ResourceCatalog.CreateDefault(),
                StructureCatalog.CreateDefault());
        }
    }
}
