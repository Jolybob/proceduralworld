using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public enum WorldSceneOrientationMode
    {
        Fixed,
        Rotate90,
        Mirror,
        RotateAndMirror
    }

    /// <summary>Immutable authored metadata for a reusable world scene or dungeon piece.</summary>
    public sealed class WorldSceneDefinition : IWorldFeaturePlacementDefinition
    {
        private readonly List<string> tags;

        public string Id { get; }
        public int FeatureId { get; }
        public int Width { get; }
        public int Height { get; }
        public float SpawnChance { get; }
        public int MaxPerChunk { get; }
        public int MinimumDistanceFromOrigin { get; }
        public int MaxWorldInstances { get; }
        public int Weight { get; }
        public bool Enabled { get; }
        public bool Unique { get; }
        public WorldSceneOrientationMode Orientation { get; }
        public IReadOnlyList<string> Tags => tags;

        public WorldSceneDefinition(
            string id,
            int featureId,
            int width,
            int height,
            float spawnChance = 1f,
            int maxPerChunk = 1,
            int minimumDistanceFromOrigin = 0,
            int maxWorldInstances = 0,
            int weight = 1,
            bool enabled = true,
            bool unique = false,
            WorldSceneOrientationMode orientation = WorldSceneOrientationMode.Fixed,
            IEnumerable<string> tags = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Scene ID must not be empty.", nameof(id));
            if (featureId < 0) throw new ArgumentOutOfRangeException(nameof(featureId));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (spawnChance < 0f || spawnChance > 1f) throw new ArgumentOutOfRangeException(nameof(spawnChance));
            if (maxPerChunk <= 0) throw new ArgumentOutOfRangeException(nameof(maxPerChunk));
            if (minimumDistanceFromOrigin < 0) throw new ArgumentOutOfRangeException(nameof(minimumDistanceFromOrigin));
            if (maxWorldInstances < 0) throw new ArgumentOutOfRangeException(nameof(maxWorldInstances));
            if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));

            Id = id;
            FeatureId = featureId;
            Width = width;
            Height = height;
            SpawnChance = spawnChance;
            MaxPerChunk = maxPerChunk;
            MinimumDistanceFromOrigin = minimumDistanceFromOrigin;
            MaxWorldInstances = maxWorldInstances;
            Weight = weight;
            Enabled = enabled;
            Unique = unique;
            Orientation = orientation;
            this.tags = CanonicalizeTags(tags);
        }

        private static List<string> CanonicalizeTags(IEnumerable<string> values)
        {
            var result = new List<string>();
            if (values != null)
                foreach (string value in values)
                    if (!string.IsNullOrWhiteSpace(value) && !result.Contains(value)) result.Add(value);
            result.Sort(StringComparer.Ordinal);
            return result;
        }
    }

    /// <summary>Immutable catalog of deterministic scene definitions.</summary>
    public sealed class WorldSceneCatalog
    {
        private readonly List<WorldSceneDefinition> scenes;
        private readonly Dictionary<string, WorldSceneDefinition> byId;

        public IReadOnlyList<WorldSceneDefinition> Scenes => scenes;

        public WorldSceneCatalog(IEnumerable<WorldSceneDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            scenes = new List<WorldSceneDefinition>();
            byId = new Dictionary<string, WorldSceneDefinition>(StringComparer.Ordinal);
            foreach (WorldSceneDefinition definition in definitions)
            {
                if (definition == null) continue;
                if (byId.ContainsKey(definition.Id)) throw new ArgumentException("Duplicate world scene ID: " + definition.Id, nameof(definitions));
                byId.Add(definition.Id, definition);
                scenes.Add(definition);
            }
            scenes.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        }

        public bool TryGet(string id, out WorldSceneDefinition definition) => byId.TryGetValue(id, out definition);
    }

    public sealed class WorldSceneSelectionSettings
    {
        public string Salt { get; }
        public IReadOnlyList<string> Tags { get; }
        public ISet<string> ConsumedUniqueSceneIds { get; }

        public WorldSceneSelectionSettings(string salt = "world-scenes", IEnumerable<string> tags = null, ISet<string> consumedUniqueSceneIds = null)
        {
            Salt = salt ?? string.Empty;
            var canonicalTags = new List<string>();
            if (tags != null)
                foreach (string tag in tags)
                    if (!string.IsNullOrWhiteSpace(tag) && !canonicalTags.Contains(tag)) canonicalTags.Add(tag);
            canonicalTags.Sort(StringComparer.Ordinal);
            Tags = canonicalTags;
            ConsumedUniqueSceneIds = consumedUniqueSceneIds ?? new HashSet<string>(StringComparer.Ordinal);
        }
    }

    /// <summary>Deterministically chooses a scene from a catalog before world-space placement.</summary>
    public sealed class WorldSceneSelector
    {
        public WorldSceneDefinition Select(
            int seed,
            WorldSceneCatalog catalog,
            WorldSceneSelectionSettings settings = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            settings = settings ?? new WorldSceneSelectionSettings();

            var eligible = new List<WorldSceneDefinition>();
            for (int i = 0; i < catalog.Scenes.Count; i++)
            {
                WorldSceneDefinition scene = catalog.Scenes[i];
                if (!scene.Enabled || scene.Weight <= 0) continue;
                if (!MatchesTags(scene.Tags, settings.Tags)) continue;
                if (scene.Unique && settings.ConsumedUniqueSceneIds.Contains(scene.Id)) continue;
                eligible.Add(scene);
            }

            if (eligible.Count == 0) return null;
            ulong total = 0UL;
            for (int i = 0; i < eligible.Count; i++) total += (ulong)eligible[i].Weight;
            ulong draw = StableHash(seed, settings.Salt) % total;
            for (int i = 0; i < eligible.Count; i++)
            {
                ulong weight = (ulong)eligible[i].Weight;
                if (draw < weight) return eligible[i];
                draw -= weight;
            }
            return eligible[eligible.Count - 1];
        }

        public WorldSceneDefinition SelectForRegion(int seed, WorldSceneCatalog catalog, IEnumerable<string> regionTags, string salt = "world-scenes")
        {
            return Select(seed, catalog, new WorldSceneSelectionSettings(salt, regionTags));
        }

        private static bool MatchesTags(IReadOnlyList<string> required, IReadOnlyList<string> available)
        {
            for (int i = 0; i < required.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < available.Count; j++)
                    if (string.Equals(required[i], available[j], StringComparison.Ordinal)) { found = true; break; }
                if (!found) return false;
            }
            return true;
        }

        private static ulong StableHash(int seed, string salt)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                hash ^= (uint)seed; hash *= 1099511628211UL;
                for (int i = 0; i < salt.Length; i++) { hash ^= salt[i]; hash *= 1099511628211UL; }
                hash ^= (uint)(seed >> 16); hash *= 1099511628211UL;
                return hash;
            }
        }
    }
}
