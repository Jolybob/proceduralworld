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
        public void MacroProfileSettingsCanBeChangedWithoutChangingRuntimeContracts()
        {
            WorldGenerationProfile profile = ScriptableObject.CreateInstance<WorldGenerationProfile>();
            try
            {
                profile.Settings.chunkSize = 8;
                profile.Settings.chasmsEnabled = false;

                WorldGenerationProfile.MacroRegionEntry entry = new WorldGenerationProfile.MacroRegionEntry
                {
                    id = 0,
                    minRadius = 0f,
                    maxRadius = 100f,
                    centerAngle = 0f,
                    angularWidth = Mathf.PI * 2f
                };

                profile.MacroRegions[0] = entry;
                Assert.IsFalse(profile.UseMacroRegions);
                Assert.IsTrue(profile.TryValidate(out string error), error);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
