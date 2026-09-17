using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>Candidate semantic world plan with deterministic selection metadata.</summary>
    public sealed class WorldPlanCandidate
    {
        public string Id { get; }
        public WorldPlanGraphDefinition Definition { get; }
        public int Weight { get; }
        public bool Enabled { get; }
        public IReadOnlyList<string> RequiredTags { get; }

        public WorldPlanCandidate(string id, WorldPlanGraphDefinition definition, int weight = 1, bool enabled = true, IEnumerable<string> requiredTags = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Candidate ID must not be empty.", nameof(id));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
            Id = id;
            Definition = definition;
            Weight = weight;
            Enabled = enabled;
            var tags = new List<string>();
            if (requiredTags != null)
                foreach (string tag in requiredTags)
                    if (!string.IsNullOrWhiteSpace(tag) && !tags.Contains(tag)) tags.Add(tag);
            tags.Sort(StringComparer.Ordinal);
            RequiredTags = tags;
        }
    }

    public sealed class WorldPlanSelectionSettings
    {
        public string Salt { get; }
        public IReadOnlyList<string> Tags { get; }

        public WorldPlanSelectionSettings(string salt = "world-plan", IEnumerable<string> tags = null)
        {
            Salt = salt ?? string.Empty;
            var values = new List<string>();
            if (tags != null)
                foreach (string tag in tags)
                    if (!string.IsNullOrWhiteSpace(tag) && !values.Contains(tag)) values.Add(tag);
            values.Sort(StringComparer.Ordinal);
            Tags = values;
        }

        public static WorldPlanSelectionSettings Default => new WorldPlanSelectionSettings();
    }

    public sealed class WorldPlanSelectionResult
    {
        public WorldPlanCandidate Candidate { get; }
        public bool Succeeded => Candidate != null;

        internal WorldPlanSelectionResult(WorldPlanCandidate candidate) { Candidate = candidate; }
    }

    /// <summary>
    /// Selects one semantic world plan without relying on process-local random state.
    /// Candidate ordering is canonicalized by ID and the weighted draw is derived solely from seed and salt.
    /// </summary>
    public sealed class WorldPlanSelector
    {
        public WorldPlanSelectionResult Select(int seed, IEnumerable<WorldPlanCandidate> candidates, WorldPlanSelectionSettings settings = null)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            settings = settings ?? WorldPlanSelectionSettings.Default;

            var eligible = new List<WorldPlanCandidate>();
            foreach (WorldPlanCandidate candidate in candidates)
            {
                if (candidate == null || !candidate.Enabled || candidate.Weight <= 0) continue;
                if (!MatchesTags(candidate.RequiredTags, settings.Tags)) continue;
                eligible.Add(candidate);
            }
            eligible.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            if (eligible.Count == 0) return new WorldPlanSelectionResult(null);

            ulong total = 0UL;
            for (int i = 0; i < eligible.Count; i++) total += (ulong)eligible[i].Weight;
            ulong draw = StableHash(seed, settings.Salt) % total;
            for (int i = 0; i < eligible.Count; i++)
            {
                ulong weight = (ulong)eligible[i].Weight;
                if (draw < weight) return new WorldPlanSelectionResult(eligible[i]);
                draw -= weight;
            }
            return new WorldPlanSelectionResult(eligible[eligible.Count - 1]);
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
