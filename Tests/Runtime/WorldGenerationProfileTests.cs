using NUnit.Framework;
using UnityEngine;

namespace Jolybob.ProceduralWorld.Tests
{
    public class WorldGenerationProfileTests
    {
        [Test]
        public void DefaultProfileBuildsDeterministicGenerator()
        {
            WorldGenerationProfile profile = ScriptableObject.CreateInstance<WorldGenerationProfile>();
            try
            {
                Assert.IsTrue(profile.TryValidate(out string error), error);

                GeneratedChunk first = profile
                    .CreateGenerator(51746)
                    .GenerateChunk(new ChunkCoord(2, -3));
                GeneratedChunk second = profile
                    .CreateGenerator(51746)
                    .GenerateChunk(new ChunkCoord(2, -3));

                Assert.AreEqual(first.Cells.Length, second.Cells.Length);
                for (int i = 0; i < first.Cells.Length; i++)
                {
                    Assert.AreEqual(first.Cells[i].Region, second.Cells[i].Region);
                    Assert.AreEqual(first.Cells[i].Terrain, second.Cells[i].Terrain);
                    Assert.AreEqual(first.Cells[i].Resource, second.Cells[i].Resource);
                    Assert.AreEqual(first.Cells[i].Structure, second.Cells[i].Structure);
                    Assert.AreEqual(first.Cells[i].Topology, second.Cells[i].Topology);
                    Assert.AreEqual(first.Cells[i].Flags, second.Cells[i].Flags);
                }
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MacroProfileBuildsPositionAwareResolver()
        {
            WorldGenerationProfile profile = ScriptableObject.CreateInstance<WorldGenerationProfile>();
            try
            {
                profile.Settings.chunkSize = 8;
                profile.SetRegions(
                    new WorldGenerationProfile.RegionEntry
                    {
                        id = 0,
                        name = "Core",
                        defaultTerrain = 4
                    },
                    new WorldGenerationProfile.RegionEntry
                    {
                        id = 1,
                        name = "Outer",
                        defaultTerrain = 2
                    });
                profile.SetMacroRegions(
                    new WorldGenerationProfile.MacroRegionEntry
                    {
                        id = 0,
                        minRadius = 0f,
                        maxRadius = 20f,
                        centerAngle = 0f,
                        angularWidth = Mathf.PI * 2f
                    },
                    new WorldGenerationProfile.MacroRegionEntry
                    {
                        id = 1,
                        minRadius = 20f,
                        maxRadius = 200f,
                        centerAngle = 0f,
                        angularWidth = Mathf.PI * 2f
                    });

                Assert.AreEqual((byte)0, profile.MacroFallbackRegionId);
                Assert.IsTrue(profile.UseMacroRegions);
                Assert.IsTrue(profile.TryValidate(out string error), error);

                GeneratedChunk chunk = profile.CreateGenerator(51746)
                    .GenerateChunk(new ChunkCoord(4, 0));

                Assert.AreEqual(new RegionId(1), chunk.GetCell(0, 4).Region);
                Assert.AreEqual(new TerrainId(2), chunk.GetCell(0, 4).Terrain);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
