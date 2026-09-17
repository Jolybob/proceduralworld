using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jolybob.ProceduralWorld.Authoring
{
    /// <summary>
    /// Unity-authored world definition that assembles the framework's data-only catalogs,
    /// macro layout, and generation settings into a deterministic chunk generator.
    /// The asset contains no Tilemap or presentation dependency.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ProceduralWorldDefinition",
        menuName = "Procedural World/World Definition",
        order = 0)]
    public sealed class ProceduralWorldDefinitionAsset : ScriptableObject
    {
        public enum RegionLayoutMode
        {
            Threshold,
            RadialSectors
        }

        [Serializable]
        private sealed class RegionProfileEntry
        {
            [Min(0)] public int id;
            public string name = "Region";
            public string description = string.Empty;
            [Min(0)] public int defaultTerrainId;
        }

        [Serializable]
        private sealed class TerrainProfileEntry
        {
            [Min(0)] public int id;
            public string name = "Terrain";
            public WorldTile tile = WorldTile.Mid;
            public string description = string.Empty;
        }

        [Serializable]
        private sealed class MacroRegionEntry
        {
            [Min(0)] public int regionId;
            [Min(0f)] public float minRadius = 48f;
            [Min(0.001f)] public float maxRadius = 210f;
            public float centerAngleDegrees;
            [Range(0.001f, 360f)] public float angularWidthDegrees = 90f;
            [Min(0f)] public float boundaryWarp = 14f;
            [Min(0.001f)] public float boundaryNoiseScale = 0.012f;
            [Min(0f)] public float angularWarpRadians = 0.18f;
            public int priority = 10;
        }

        [Header("World")]
        [SerializeField] private int seed = 161729;
        [SerializeField] private WorldGenerationSettings settings = new WorldGenerationSettings();

        [Header("Regions")]
        [SerializeField] private RegionLayoutMode regionLayout = RegionLayoutMode.RadialSectors;
        [SerializeField, Min(0)] private int fallbackRegionId;
        [SerializeField] private List<RegionProfileEntry> regionProfiles = new List<RegionProfileEntry>();
        [SerializeField] private List<MacroRegionEntry> macroRegions = new List<MacroRegionEntry>();

        [Header("Terrain")]
        [SerializeField] private List<TerrainProfileEntry> terrainProfiles = new List<TerrainProfileEntry>();

        public int Seed => seed;

        public WorldGenerationSettings Settings => settings;

        public RegionLayoutMode LayoutMode => regionLayout;

        /// <summary>
        /// Creates a generator using the authored catalogs and region layout.
        /// Resource, structure, topology, and post-process catalogs retain their framework defaults
        /// until a future authoring layer supplies them explicitly.
        /// </summary>
        public ProceduralWorldGenerator CreateGenerator()
        {
            RegionCatalog regions = BuildRegionCatalog();
            TerrainCatalog terrains = BuildTerrainCatalog();
            IRegionResolver resolver = BuildRegionResolver();

            return new ProceduralWorldGenerator(
                seed,
                settings ?? new WorldGenerationSettings(),
                null,
                null,
                null,
                regions,
                terrains,
                null,
                null,
                null,
                null,
                resolver);
        }

        private RegionCatalog BuildRegionCatalog()
        {
            if (regionProfiles == null || regionProfiles.Count == 0)
                return RegionCatalog.CreateDefault();

            var definitions = new List<RegionDefinition>(regionProfiles.Count);
            for (int i = 0; i < regionProfiles.Count; i++)
            {
                RegionProfileEntry entry = regionProfiles[i];
                if (entry == null)
                    continue;

                definitions.Add(new RegionDefinition(
                    new RegionId(ToByte(entry.id, "region id")),
                    entry.name,
                    new TerrainId(ToByte(entry.defaultTerrainId, "default terrain id")),
                    entry.description));
            }

            return definitions.Count > 0
                ? new RegionCatalog(definitions)
                : RegionCatalog.CreateDefault();
        }

        private TerrainCatalog BuildTerrainCatalog()
        {
            if (terrainProfiles == null || terrainProfiles.Count == 0)
                return TerrainCatalog.CreateDefault();

            var definitions = new List<TerrainDefinition>(terrainProfiles.Count);
            for (int i = 0; i < terrainProfiles.Count; i++)
            {
                TerrainProfileEntry entry = terrainProfiles[i];
                if (entry == null)
                    continue;

                definitions.Add(new TerrainDefinition(
                    new TerrainId(ToByte(entry.id, "terrain id")),
                    entry.name,
                    entry.tile,
                    entry.description));
            }

            return definitions.Count > 0
                ? new TerrainCatalog(definitions)
                : TerrainCatalog.CreateDefault();
        }

        private IRegionResolver BuildRegionResolver()
        {
            if (regionLayout == RegionLayoutMode.Threshold)
                return new ThresholdRegionResolver();

            MacroRegionCatalog catalog = BuildMacroRegionCatalog();
            var radialLayout = new RadialSectorRegionResolver(
                catalog,
                seed,
                new RegionId(ToByte(fallbackRegionId, "fallback region id")));

            return new RegionLayoutResolver(
                radialLayout,
                new RegionId(ToByte(fallbackRegionId, "fallback region id")));
        }

        private MacroRegionCatalog BuildMacroRegionCatalog()
        {
            if (macroRegions == null || macroRegions.Count == 0)
                return MacroRegionCatalog.CreateDefault();

            var definitions = new List<MacroRegionDefinition>(macroRegions.Count);
            for (int i = 0; i < macroRegions.Count; i++)
            {
                MacroRegionEntry entry = macroRegions[i];
                if (entry == null)
                    continue;

                definitions.Add(new MacroRegionDefinition(
                    new RegionId(ToByte(entry.regionId, "macro region id")),
                    entry.minRadius,
                    entry.maxRadius,
                    entry.centerAngleDegrees * Mathf.Deg2Rad,
                    Mathf.Clamp(entry.angularWidthDegrees, 0.001f, 360f) * Mathf.Deg2Rad,
                    entry.boundaryWarp,
                    entry.boundaryNoiseScale,
                    entry.angularWarpRadians,
                    entry.priority));
            }

            return definitions.Count > 0
                ? new MacroRegionCatalog(definitions)
                : MacroRegionCatalog.CreateDefault();
        }

        private static byte ToByte(int value, string label)
        {
            if (value < byte.MinValue || value > byte.MaxValue)
                throw new ArgumentOutOfRangeException(label, value, "World definition IDs must fit in an unsigned byte.");
            return (byte)value;
        }
    }
}
