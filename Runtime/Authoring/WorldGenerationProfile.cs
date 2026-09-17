using System;
using UnityEngine;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Unity-authored, project-facing definition of a procedural world.
    /// The asset contains only serializable configuration; runtime catalogs remain immutable.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WorldGenerationProfile",
        menuName = "Procedural World/World Generation Profile")]
    public sealed class WorldGenerationProfile : ScriptableObject
    {
        [Header("Generation")]
        [SerializeField] private WorldGenerationSettings settings = new WorldGenerationSettings();

        [Header("Macro Regions")]
        [SerializeField] private bool useMacroRegions;
        [SerializeField] private bool rotateMacroLayoutBySeed = true;
        [SerializeField] private float macroSeedRotationRadians;
        [SerializeField] private byte macroFallbackRegionId;
        [SerializeField] private MacroRegionEntry[] macroRegions = Array.Empty<MacroRegionEntry>();

        [Header("Regions")]
        [SerializeField] private RegionEntry[] regions = Array.Empty<RegionEntry>();

        [Header("Terrain")]
        [SerializeField] private TerrainEntry[] terrains = Array.Empty<TerrainEntry>();

        [Header("Resources")]
        [SerializeField] private ResourceEntry[] resources = Array.Empty<ResourceEntry>();

        [Header("Structures")]
        [SerializeField] private StructureEntry[] structures = Array.Empty<StructureEntry>();

        public WorldGenerationSettings Settings => settings;
        public bool UseMacroRegions => useMacroRegions && macroRegions != null && macroRegions.Length > 0;
        public bool RotateMacroLayoutBySeed => rotateMacroLayoutBySeed;
        public float MacroSeedRotationRadians => macroSeedRotationRadians;
        public byte MacroFallbackRegionId => macroFallbackRegionId;
        public MacroRegionEntry[] MacroRegions => macroRegions;
        public RegionEntry[] Regions => regions;
        public TerrainEntry[] Terrains => terrains;
        public ResourceEntry[] Resources => resources;
        public StructureEntry[] Structures => structures;

        public void SetMacroRegions(params MacroRegionEntry[] value)
        {
            macroRegions = value ?? Array.Empty<MacroRegionEntry>();
            useMacroRegions = macroRegions.Length > 0;
        }

        public void SetRegions(params RegionEntry[] value) => regions = value ?? Array.Empty<RegionEntry>();
        public void SetTerrains(params TerrainEntry[] value) => terrains = value ?? Array.Empty<TerrainEntry>();
        public void SetResources(params ResourceEntry[] value) => resources = value ?? Array.Empty<ResourceEntry>();
        public void SetStructures(params StructureEntry[] value) => structures = value ?? Array.Empty<StructureEntry>();
        public void EnableMacroRegions(bool enabled) => useMacroRegions = enabled;

        /// <summary>
        /// Builds the runtime generator from this asset without exposing mutable authoring state
        /// to the generation pipeline.
        /// </summary>
        public ProceduralWorldGenerator CreateGenerator(int seed)
        {
            RegionCatalog regionCatalog = BuildRegions();
            TerrainCatalog terrainCatalog = BuildTerrains();
            ResourceCatalog resourceCatalog = BuildResources();
            StructureCatalog structureCatalog = BuildStructures();

            IRegionResolver resolver;
            if (UseMacroRegions)
            {
                RegionId fallback = new RegionId(macroFallbackRegionId);
                if (!regionCatalog.TryGet(fallback, out _))
                    throw new InvalidOperationException(
                        $"Macro fallback region ID {macroFallbackRegionId} is not defined in the region catalog.");

                resolver = new RadialSectorRegionResolver(
                    BuildMacroRegions(),
                    seed,
                    fallback,
                    seedRotationRadians: macroSeedRotationRadians,
                    rotateBySeed: rotateMacroLayoutBySeed);
            }
            else
            {
                resolver = new ThresholdRegionResolver();
            }

            var pipeline = new WorldGenerationPipeline()
                .Add(new RegionBiomePass(resolver))
                .Add(new TerrainPass(regionCatalog, terrainCatalog))
                .Add(new CavePass(settings))
                .Add(new ChasmPass(settings))
                .Add(new ResourcePass(resourceCatalog))
                .Add(new StructurePass(structureCatalog));

            return new ProceduralWorldGenerator(
                seed,
                settings,
                pipeline,
                null,
                null,
                regionCatalog,
                terrainCatalog,
                resourceCatalog,
                structureCatalog,
                new WorldPostProcessPipeline());
        }

        public bool TryValidate(out string error)
        {
            try
            {
                BuildRegions();
                BuildTerrains();
                BuildResources();
                BuildStructures();
                if (UseMacroRegions)
                {
                    RegionCatalog catalog = BuildRegions();
                    if (!catalog.TryGet(new RegionId(macroFallbackRegionId), out _))
                        throw new InvalidOperationException(
                            $"Macro fallback region ID {macroFallbackRegionId} is not defined in the region catalog.");
                    BuildMacroRegions();
                }

                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private RegionCatalog BuildRegions()
        {
            if (regions == null || regions.Length == 0)
                return RegionCatalog.CreateDefault();

            var definitions = new RegionDefinition[regions.Length];
            for (int i = 0; i < regions.Length; i++)
            {
                RegionEntry entry = regions[i];
                if (entry == null)
                    throw new InvalidOperationException($"Regions[{i}] is null.");

                definitions[i] = new RegionDefinition(
                    new RegionId(entry.id),
                    entry.name,
                    new TerrainId(entry.defaultTerrain),
                    entry.description);
            }

            return new RegionCatalog(definitions);
        }

        private TerrainCatalog BuildTerrains()
        {
            if (terrains == null || terrains.Length == 0)
                return TerrainCatalog.CreateDefault();

            var definitions = new TerrainDefinition[terrains.Length];
            for (int i = 0; i < terrains.Length; i++)
            {
                TerrainEntry entry = terrains[i];
                if (entry == null)
                    throw new InvalidOperationException($"Terrains[{i}] is null.");

                definitions[i] = new TerrainDefinition(
                    new TerrainId(entry.id),
                    entry.name,
                    entry.tile,
                    entry.description);
            }

            return new TerrainCatalog(definitions);
        }

        private ResourceCatalog BuildResources()
        {
            if (resources == null || resources.Length == 0)
                return ResourceCatalog.CreateDefault();

            var definitions = new ResourceDefinition[resources.Length];
            for (int i = 0; i < resources.Length; i++)
            {
                ResourceEntry entry = resources[i];
                if (entry == null)
                    throw new InvalidOperationException($"Resources[{i}] is null.");

                definitions[i] = new ResourceDefinition(
                    new ResourceId(entry.id),
                    entry.name,
                    new RegionId(entry.region),
                    new TerrainId(entry.terrain),
                    entry.spawnChance,
                    entry.maxPerChunk,
                    entry.minimumDistanceFromOrigin);
            }

            return new ResourceCatalog(definitions);
        }

        private StructureCatalog BuildStructures()
        {
            if (structures == null || structures.Length == 0)
                return StructureCatalog.CreateDefault();

            var definitions = new StructureDefinition[structures.Length];
            for (int i = 0; i < structures.Length; i++)
            {
                StructureEntry entry = structures[i];
                if (entry == null)
                    throw new InvalidOperationException($"Structures[{i}] is null.");

                definitions[i] = new StructureDefinition(
                    new StructureId(entry.id),
                    entry.name,
                    new RegionId(entry.region),
                    new TerrainId(entry.terrain),
                    entry.spawnChance,
                    entry.maxPerChunk,
                    entry.width,
                    entry.height,
                    entry.minimumDistanceFromOrigin);
            }

            return new StructureCatalog(definitions);
        }

        private MacroRegionCatalog BuildMacroRegions()
        {
            if (macroRegions == null || macroRegions.Length == 0)
                throw new InvalidOperationException("Macro regions are enabled but no macro-region entries are configured.");

            var definitions = new MacroRegionDefinition[macroRegions.Length];
            for (int i = 0; i < macroRegions.Length; i++)
            {
                MacroRegionEntry entry = macroRegions[i];
                if (entry == null)
                    throw new InvalidOperationException($"MacroRegions[{i}] is null.");

                definitions[i] = new MacroRegionDefinition(
                    new RegionId(entry.id),
                    entry.minRadius,
                    entry.maxRadius,
                    entry.centerAngle,
                    entry.angularWidth,
                    entry.boundaryWarp,
                    entry.boundaryNoiseScale,
                    entry.angularWarp,
                    entry.priority);
            }

            return new MacroRegionCatalog(definitions);
        }

        [Serializable]
        public sealed class MacroRegionEntry
        {
            public byte id;
            [Min(0f)] public float minRadius;
            [Min(0f)] public float maxRadius = 100f;
            public float centerAngle;
            [Min(0f)] public float angularWidth = Mathf.PI * 2f;
            [Min(0f)] public float boundaryWarp;
            [Min(0.0001f)] public float boundaryNoiseScale = 0.01f;
            [Min(0f)] public float angularWarp;
            public int priority;
        }

        [Serializable]
        public sealed class RegionEntry
        {
            public byte id;
            public string name = "Region";
            public string description;
            public byte defaultTerrain;
        }

        [Serializable]
        public sealed class TerrainEntry
        {
            public byte id;
            public string name = "Terrain";
            public string description;
            public WorldTile tile = WorldTile.Mid;
        }

        [Serializable]
        public sealed class ResourceEntry
        {
            public byte id;
            public string name = "Resource";
            public byte region;
            public byte terrain;
            [Range(0f, 1f)] public float spawnChance = 0.01f;
            [Min(1)] public byte maxPerChunk = 1;
            [Min(0)] public int minimumDistanceFromOrigin;
        }

        [Serializable]
        public sealed class StructureEntry
        {
            public byte id;
            public string name = "Structure";
            public byte region;
            public byte terrain;
            [Range(0f, 1f)] public float spawnChance = 0.01f;
            [Min(1)] public byte maxPerChunk = 1;
            [Min(1)] public int width = 1;
            [Min(1)] public int height = 1;
            [Min(0)] public int minimumDistanceFromOrigin;
        }
    }
}
