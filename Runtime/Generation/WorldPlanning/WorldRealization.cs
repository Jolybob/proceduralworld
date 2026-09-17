using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>A deterministic world-space operation ready for chunk materialization.</summary>
    public readonly struct WorldRealizationEdit : IEquatable<WorldRealizationEdit>
    {
        public string Id { get; }
        public string SourceId { get; }
        public string Kind { get; }
        public string Value { get; }
        public int Priority { get; }
        public WorldPosition Position { get; }

        public WorldRealizationEdit(string id, string sourceId, string kind, string value, int priority, WorldPosition position)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Realization edit ID must not be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Realization source ID must not be empty.", nameof(sourceId));
            if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Realization edit kind must not be empty.", nameof(kind));
            Id = id;
            SourceId = sourceId;
            Kind = kind;
            Value = value ?? string.Empty;
            Priority = priority;
            Position = position;
        }

        public bool Equals(WorldRealizationEdit other) =>
            string.Equals(Id, other.Id, StringComparison.Ordinal) &&
            string.Equals(SourceId, other.SourceId, StringComparison.Ordinal) &&
            string.Equals(Kind, other.Kind, StringComparison.Ordinal) &&
            string.Equals(Value, other.Value, StringComparison.Ordinal) &&
            Priority == other.Priority && Position == other.Position;

        public override bool Equals(object obj) => obj is WorldRealizationEdit other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, SourceId, Kind, Value, Priority, Position);
    }

    public readonly struct WorldRealizationConflict : IEquatable<WorldRealizationConflict>
    {
        public WorldRealizationEdit Existing { get; }
        public WorldRealizationEdit Requested { get; }

        public WorldRealizationConflict(WorldRealizationEdit existing, WorldRealizationEdit requested)
        {
            Existing = existing;
            Requested = requested;
        }

        public bool Equals(WorldRealizationConflict other) => Existing.Equals(other.Existing) && Requested.Equals(other.Requested);
        public override bool Equals(object obj) => obj is WorldRealizationConflict other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Existing, Requested);
    }

    /// <summary>Immutable-in-practice deterministic collection of world-space realization edits.</summary>
    public sealed class WorldRealizationBatch
    {
        private readonly List<WorldRealizationEdit> edits;

        public IReadOnlyList<WorldRealizationEdit> Edits => edits;
        public int Count => edits.Count;

        public WorldRealizationBatch(IEnumerable<WorldRealizationEdit> edits)
        {
            if (edits == null) throw new ArgumentNullException(nameof(edits));
            this.edits = new List<WorldRealizationEdit>(edits);
            this.edits.Sort(CompareEdits);
            for (int i = 1; i < this.edits.Count; i++)
                if (string.Equals(this.edits[i - 1].Id, this.edits[i].Id, StringComparison.Ordinal))
                    throw new ArgumentException("Realization edit IDs must be unique.", nameof(edits));
        }

        public static WorldRealizationBatch Empty => new WorldRealizationBatch(Array.Empty<WorldRealizationEdit>());

        private static int CompareEdits(WorldRealizationEdit left, WorldRealizationEdit right)
        {
            int c = left.Position.X.CompareTo(right.Position.X);
            if (c != 0) return c;
            c = left.Position.Y.CompareTo(right.Position.Y);
            if (c != 0) return c;
            c = string.CompareOrdinal(left.Kind, right.Kind);
            if (c != 0) return c;
            c = right.Priority.CompareTo(left.Priority);
            if (c != 0) return c;
            c = string.CompareOrdinal(left.SourceId, right.SourceId);
            if (c != 0) return c;
            return string.CompareOrdinal(left.Id, right.Id);
        }
    }

    /// <summary>
    /// World-space realization store. World coordinates are authoritative; chunk indexing is only
    /// an acceleration structure used by streaming/materialization adapters.
    /// </summary>
    public sealed class WorldRealizationMap
    {
        private readonly int chunkSize;
        private readonly Dictionary<ChunkCoord, List<WorldRealizationEdit>> byChunk = new Dictionary<ChunkCoord, List<WorldRealizationEdit>>();
        private readonly Dictionary<string, WorldRealizationEdit> byId = new Dictionary<string, WorldRealizationEdit>(StringComparer.Ordinal);

        public WorldRealizationMap(int chunkSize = 64)
        {
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
            this.chunkSize = chunkSize;
        }

        public int ChunkSize => chunkSize;
        public int Count => byId.Count;

        public bool TryAdd(WorldRealizationEdit edit, out WorldRealizationConflict conflict)
        {
            if (byId.TryGetValue(edit.Id, out WorldRealizationEdit existingById))
            {
                conflict = new WorldRealizationConflict(existingById, edit);
                return false;
            }

            var candidates = new List<WorldRealizationEdit>();
            Collect(edit.Position, edit.Kind, candidates);
            for (int i = 0; i < candidates.Count; i++)
            {
                WorldRealizationEdit existing = candidates[i];
                if (existing.Position == edit.Position && string.Equals(existing.Kind, edit.Kind, StringComparison.Ordinal))
                {
                    conflict = new WorldRealizationConflict(existing, edit);
                    return false;
                }
            }

            byId.Add(edit.Id, edit);
            ChunkCoord chunk = ToChunk(edit.Position);
            if (!byChunk.TryGetValue(chunk, out List<WorldRealizationEdit> edits))
            {
                edits = new List<WorldRealizationEdit>();
                byChunk.Add(chunk, edits);
            }
            edits.Add(edit);
            conflict = default(WorldRealizationConflict);
            return true;
        }

        public bool Remove(string editId)
        {
            if (string.IsNullOrWhiteSpace(editId) || !byId.TryGetValue(editId, out WorldRealizationEdit edit)) return false;
            byId.Remove(editId);
            ChunkCoord chunk = ToChunk(edit.Position);
            if (byChunk.TryGetValue(chunk, out List<WorldRealizationEdit> edits))
            {
                edits.RemoveAll(value => string.Equals(value.Id, editId, StringComparison.Ordinal));
                if (edits.Count == 0) byChunk.Remove(chunk);
            }
            return true;
        }

        public bool TryGet(string editId, out WorldRealizationEdit edit) => byId.TryGetValue(editId, out edit);

        public void Collect(WorldPosition position, List<WorldRealizationEdit> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (!byChunk.TryGetValue(ToChunk(position), out List<WorldRealizationEdit> edits)) return;
            for (int i = 0; i < edits.Count; i++)
                if (edits[i].Position == position) output.Add(edits[i]);
            output.Sort(CompareEdits);
        }

        public void Collect(WorldPosition position, string kind, List<WorldRealizationEdit> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (!byChunk.TryGetValue(ToChunk(position), out List<WorldRealizationEdit> edits)) return;
            for (int i = 0; i < edits.Count; i++)
                if (edits[i].Position == position && string.Equals(edits[i].Kind, kind, StringComparison.Ordinal)) output.Add(edits[i]);
            output.Sort(CompareEdits);
        }

        public void CollectChunk(ChunkCoord chunk, List<WorldRealizationEdit> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (!byChunk.TryGetValue(chunk, out List<WorldRealizationEdit> edits)) return;
            output.AddRange(edits);
            output.Sort(CompareEdits);
        }

        public void CollectIntersecting(WorldPosition min, WorldPosition max, List<WorldRealizationEdit> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (max.X < min.X || max.Y < min.Y) return;
            int minX = FloorDiv(min.X, chunkSize), maxX = FloorDiv(max.X, chunkSize);
            int minY = FloorDiv(min.Y, chunkSize), maxY = FloorDiv(max.Y, chunkSize);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (long x = minX; x <= (long)maxX; x++)
                for (long y = minY; y <= (long)maxY; y++)
                {
                    if (!byChunk.TryGetValue(new ChunkCoord((int)x, (int)y), out List<WorldRealizationEdit> edits)) continue;
                    for (int i = 0; i < edits.Count; i++)
                    {
                        WorldRealizationEdit edit = edits[i];
                        if (edit.Position.X >= min.X && edit.Position.X <= max.X && edit.Position.Y >= min.Y && edit.Position.Y <= max.Y && seen.Add(edit.Id)) output.Add(edit);
                    }
                }
            output.Sort(CompareEdits);
        }

        public void Clear()
        {
            byChunk.Clear();
            byId.Clear();
        }

        private ChunkCoord ToChunk(WorldPosition position) => new ChunkCoord(FloorDiv(position.X, chunkSize), FloorDiv(position.Y, chunkSize));

        private static int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            if (remainder != 0 && ((remainder < 0) != (divisor < 0))) quotient--;
            return quotient;
        }

        private static int CompareEdits(WorldRealizationEdit left, WorldRealizationEdit right)
        {
            int c = left.Position.X.CompareTo(right.Position.X);
            if (c != 0) return c;
            c = left.Position.Y.CompareTo(right.Position.Y);
            if (c != 0) return c;
            c = string.CompareOrdinal(left.Kind, right.Kind);
            if (c != 0) return c;
            c = right.Priority.CompareTo(left.Priority);
            if (c != 0) return c;
            c = string.CompareOrdinal(left.SourceId, right.SourceId);
            if (c != 0) return c;
            return string.CompareOrdinal(left.Id, right.Id);
        }
    }

    public interface IWorldPlanRealizationSource
    {
        IEnumerable<WorldRealizationEdit> CreateEdits(WorldPlanFeaturePlacement placement);
    }

    /// <summary>Converts lowered feature placements into a deterministic world-space edit batch.</summary>
    public sealed class WorldPlanRealizer
    {
        public WorldRealizationBatch Realize(WorldPlanFeatureLoweringResult lowering, IWorldPlanRealizationSource source)
        {
            if (lowering == null) throw new ArgumentNullException(nameof(lowering));
            if (source == null) throw new ArgumentNullException(nameof(source));
            var edits = new List<WorldRealizationEdit>();
            var placements = new List<WorldPlanFeaturePlacement>(lowering.Placements);
            placements.Sort((a, b) => string.CompareOrdinal(a.NodeId, b.NodeId));
            for (int i = 0; i < placements.Count; i++)
            {
                IEnumerable<WorldRealizationEdit> generated = source.CreateEdits(placements[i]);
                if (generated == null) continue;
                foreach (WorldRealizationEdit edit in generated) edits.Add(edit);
            }
            return new WorldRealizationBatch(edits);
        }
    }
}
