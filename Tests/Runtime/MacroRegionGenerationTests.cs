using System;
using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public sealed class MacroRegionGenerationTests
    {
        [Test]
        public void RadialSectorResolverIsDeterministic()
        {
            var catalog = MacroRegionCatalog.CreateDefault();
            var first = new RadialSectorRegionResolver(catalog, 12345, new RegionId(0));
            var second = new RadialSectorRegionResolver(catalog, 12345, new RegionId(0));

            for (int x = -250; x <= 250; x += 17)
            {
                for (int y = -250; y <= 250; y += 19)
                {
                    Assert.AreEqual(
                        first.Resolve(default(EnvironmentSample), x, y),
                        second.Resolve(default(EnvironmentSample), x, y),
                        $"Resolver mismatch at ({x}, {y}).");
                }
            }
        }

        [Test]
        public void RadialSectorResolverKeepsCoreFallbackOutsideMacroRing()
        {
            var resolver = new RadialSectorRegionResolver(
                MacroRegionCatalog.CreateDefault(),
                1,
                new RegionId(0),
                rotateBySeed: false);

            Assert.AreEqual(new RegionId(0), resolver.Resolve(default(EnvironmentSample), 0, 0));
            Assert.AreEqual(new RegionId(0), resolver.Resolve(default(EnvironmentSample), 500, 0));
        }

        [Test]
        public void RadialSectorResolverUsesDeterministicSectorBoundaries()
        {
            var catalog = new MacroRegionCatalog(new[]
            {
                new MacroRegionDefinition(new RegionId(7), 10f, 100f, 0f, MathF.PI / 2f, priority: 1),
                new MacroRegionDefinition(new RegionId(8), 10f, 100f, MathF.PI, MathF.PI / 2f, priority: 1)
            });
            var resolver = new RadialSectorRegionResolver(catalog, 99, new RegionId(0), rotateBySeed: false);

            Assert.AreEqual(new RegionId(7), resolver.Resolve(default(EnvironmentSample), 50, 0));
            Assert.AreEqual(new RegionId(8), resolver.Resolve(default(EnvironmentSample), -50, 0));
            Assert.AreEqual(new RegionId(0), resolver.Resolve(default(EnvironmentSample), 0, 50));
        }

        [Test]
        public void MacroRegionsCanBeEnabledThroughGeneratorSettings()
        {
            var settings = new WorldGenerationSettings
            {
                chunkSize = 16,
                macroRegionsEnabled = true
            };
            var generator = new ProceduralWorldGenerator(42, settings);

            GeneratedChunk chunk = generator.GenerateChunk(new ChunkCoord(0, 0));

            bool foundMacroRegion = false;
            for (int y = 0; y < chunk.Size; y++)
            {
                for (int x = 0; x < chunk.Size; x++)
                {
                    if (chunk.GetCell(x, y).Region != new RegionId(0))
                    {
                        foundMacroRegion = true;
                        break;
                    }
                }

                if (foundMacroRegion)
                    break;
            }

            Assert.IsTrue(foundMacroRegion);
        }
    }
}
