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
                if (a.Cells[i].Tile != b.Cells[i].Tile || a.Cells[i].Biome != b.Cells[i].Biome)
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
    }
}
