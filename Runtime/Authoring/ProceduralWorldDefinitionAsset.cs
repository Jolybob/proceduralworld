using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jolybob.ProceduralWorld.Authoring
{
    /// <summary>
    /// Unity-authored world definition that assembles the framework's data-only catalogs,
    /// macro layout, optional semantic world plan, and generation settings into a deterministic
    /// chunk generator. The asset contains no Tilemap or presentation dependency.
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

        [Serializable]
        private sealed class WorldPlanCandidateEntry
        {
            public string id = "plan";
            public WorldPlanGraphAsset graph;
            [Min(0)] public int weight = 1;
            public bool enabled = true;
            public List<string> requiredTags = new List<string>();
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

        [Header("Semantic World Plan")]
        [SerializeField] private WorldPlanGraphAsset worldPlanGraph;
        [SerializeField] private List<WorldPlanCandidateEntry> worldPlanCandidates = new List<WorldPlanCandidateEntry>();
        [SerializeField] private List<string> worldPlanTags = new List<string>();
        [SerializeField] private string worldPlanSelectionSalt = "world-plan";

        public int Seed => seed;

        public WorldGenerationSettings Settings => settings;

        public RegionLayoutMode LayoutMode => regionLayout;

        public WorldPlanGraphAsset WorldPlanGraph => worldPlanGraph;

        /// <summary>
        /// Creates a generator using the authored catalogs and region layout. When a semantic
        /// world-plan graph is assigned, it is compiled and laid out once at generator creation;
        /// chunk generation receives the same immutable plan runtime through its context.
        /// Feature lowering/materialization still remains an explicit runtime policy boundary.
        /// </summary>
        public ProceduralWorldGenerator CreateGenerator()
        {
            WorldGenerationSettings resolvedSettings = settings ?? new WorldGenerationSettings();
            RegionCatalog regions = BuildRegionCatalog();
            TerrainCatalog terrains = BuildTerrainCatalog();
            IRegionResolver resolver = BuildRegionResolver();
            WorldPlanRuntime worldPlan = BuildWorldPlanRuntime(resolvedSettings.chunkSize);

            return new ProceduralWorldGenerator(
                seed,
                resolvedSettings,
                null,
                null,
                null,
                regions,
                terrains,
                null,
                null,
                null,
                null,
                resolver,
                worldPlan);
        }

        private WorldPlanRuntime BuildWorldPlanRuntime(int chunkSize)
        {
            WorldPlanGraphAsset selected = SelectWorldPlanGraph();
            if (selected == null)
                return null;

            return new WorldPlanRuntimeBuilder().Build(
                seed,
                selected.BuildDefinition(),
                new WorldPlanRuntimeSettings(chunkSize: chunkSize));
        }

        private WorldPlanGraphAsset SelectWorldPlanGraph()
        {
            var candidates = new List<WorldPlanCandidate>();
            if (worldPlanCandidates != null)
            {
                for (int i = 0; i < worldPlanCandidates.Count; i++)
                {
                    WorldPlanCandidateEntry entry = worldPlanCandidates[i];
                    if (entry == null || entry.graph == null)
                        continue;

                    candidates.Add(new WorldPlanCandidate(
                        entry.id,
                        entry.graph.BuildDefinition(),
                        entry.weight,
                        entry.enabled,
                        entry.requiredTags));
                }
            }

            if (candidates.Count == 0)
                return worldPlanGraph;

            WorldPlanSelectionResult result = new WorldPlanSelector().Select(
                seed,
                candidates,
                new WorldPlanSelectionSettings(worldPlanSelectionSalt, worldPlanTags));

            return result.Candidate == null ? null : FindCandidateGraph(result.Candidate.Id);
        }

        private WorldPlanGraphAsset FindCandidateGraph(string id)
        {
            for (int i = 0; i < worldPlanCandidates.Count; i++)
            {
                WorldPlanCandidateEntry entry = worldPlanCandidates[i];
                if (entry != null && entry.graph != null
                    && string.Equals(entry.id, id, StringComparison.Ordinal))
                    return entry.graph;
            }

            return null;
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
