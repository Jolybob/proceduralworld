using NUnit.Framework;

namespace Jolybob.ProceduralWorld.Tests
{
    public class MacroRegionResolverTests
    {
        [Test]
        public void FullRingRegionMatchesAnyAngleWithinBand()
        {
            var regions = new MacroRegionCatalog(new[]
            {
                MacroRegionDefinition.FullRing(new RegionId(7), 0f, 100f)
            });
            var resolver = new RadialSectorRegionResolver(regions, 1234, new RegionId(99));

            Assert.AreEqual(new RegionId(7), resolver.Resolve(new WorldPosition(50, 0), default(EnvironmentSample)));
            Assert.AreEqual(new RegionId(7), resolver.Resolve(new WorldPosition(0, 50), default(EnvironmentSample)));
            Assert.AreEqual(new RegionId(7), resolver.Resolve(new WorldPosition(-50, 0), default(EnvironmentSample)));
            Assert.AreEqual(new RegionId(7), resolver.Resolve(new WorldPosition(0, -50), default(EnvironmentSample)));
        }

        [Test]
        public void NonOverlappingSectorsProduceDifferentRegions()
        {
            var regions = new MacroRegionCatalog(new[]
            {
                new MacroRegionDefinition(new RegionId(1), 10f, 100f, 0f, 1.2f),
                new MacroRegionDefinition(new RegionId(2), 10f, 100f, 1.57079632679f, 1.2f),
                new MacroRegionDefinition(new RegionId(3), 10f, 100f, 3.14159265359f, 1.2f),
                new MacroRegionDefinition(new RegionId(4), 10f, 100f, 4.71238898038f, 1.2f)
            });
            var resolver = new RadialSectorRegionResolver(regions, 0, new RegionId(99), seedRotationRadians: 0f);

            Assert.AreEqual(new RegionId(1), resolver.Resolve(new WorldPosition(50, 0), default(EnvironmentSample)));
            Assert.AreEqual(new RegionId(2), resolver.Resolve(new WorldPosition(0, 50), default(EnvironmentSample)));
            Assert.AreEqual(new RegionId(3), resolver.Resolve(new WorldPosition(-50, 0), default(EnvironmentSample)));
            Assert.AreEqual(new RegionId(4), resolver.Resolve(new WorldPosition(0, -50), default(EnvironmentSample)));
        }

        [Test]
        public void ResolverFallsBackOutsideConfiguredRegions()
        {
            var regions = new MacroRegionCatalog(new[]
            {
                MacroRegionDefinition.FullRing(new RegionId(7), 100f, 200f)
            });
            var resolver = new RadialSectorRegionResolver(regions, 1234, new RegionId(99), seedRotationRadians: 0f);

            Assert.AreEqual(new RegionId(99), resolver.Resolve(new WorldPosition(50, 0), default(EnvironmentSample)));
            Assert.AreEqual(new RegionId(99), resolver.Resolve(new WorldPosition(250, 0), default(EnvironmentSample)));
        }

        [Test]
        public void SeedRotationIsDeterministic()
        {
            var regions = new MacroRegionCatalog(new[]
            {
                new MacroRegionDefinition(new RegionId(1), 10f, 100f, 0f, 1.2f),
                new MacroRegionDefinition(new RegionId(2), 10f, 100f, 3.14159265359f, 1.2f)
            });
            var a = new RadialSectorRegionResolver(regions, 51746, new RegionId(99));
            var b = new RadialSectorRegionResolver(regions, 51746, new RegionId(99));

            WorldPosition[] points =
            {
                new WorldPosition(40, 0),
                new WorldPosition(0, 40),
                new WorldPosition(-40, 0),
                new WorldPosition(0, -40),
                new WorldPosition(60, 60)
            };

            for (int i = 0; i < points.Length; i++)
                Assert.AreEqual(
                    a.Resolve(points[i], default(EnvironmentSample)),
                    b.Resolve(points[i], default(EnvironmentSample)));
        }

        [Test]
        public void RadialWarpCanMoveBoundaryWithoutChangingCatalog()
        {
            var regions = new MacroRegionCatalog(new[]
            {
                MacroRegionDefinition.FullRing(new RegionId(1), 0f, 50f, boundaryWarp: 15f, boundaryNoiseScale: 0.02f),
                MacroRegionDefinition.FullRing(new RegionId(2), 50f, 100f, boundaryWarp: 15f, boundaryNoiseScale: 0.02f)
            });
            var resolver = new RadialSectorRegionResolver(regions, 9001, new RegionId(99), seedRotationRadians: 0f);

            RegionId atOrigin = resolver.Resolve(new WorldPosition(0, 0), default(EnvironmentSample));
            RegionId farAway = resolver.Resolve(new WorldPosition(90, 0), default(EnvironmentSample));

            Assert.AreEqual(new RegionId(1), atOrigin);
            Assert.AreEqual(new RegionId(2), farAway);
        }
    }
}
