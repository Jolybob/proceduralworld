using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public readonly struct WorldReservation : IEquatable<WorldReservation>
    {
        public string Id { get; }
        public string OwnerId { get; }
        public string Kind { get; }
        public int Priority { get; }
        public WorldPosition Min { get; }
        public WorldPosition Max { get; }

        public WorldReservation(string id, string ownerId, string kind, int priority, WorldPosition min, WorldPosition max)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Reservation ID must not be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Reservation owner ID must not be empty.", nameof(ownerId));
            if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Reservation kind must not be empty.", nameof(kind));
            if (max.X < min.X || max.Y < min.Y) throw new ArgumentException("Reservation bounds must be inclusive and ordered.");
            Id = id;
            OwnerId = ownerId;
            Kind = kind;
            Priority = priority;
            Min = min;
            Max = max;
        }

        public bool Contains(WorldPosition position) => position.X >= Min.X && position.X <= Max.X && position.Y >= Min.Y && position.Y <= Max.Y;
        public bool Intersects(WorldPosition min, WorldPosition max) => min.X <= Max.X && max.X >= Min.X && min.Y <= Max.Y && max.Y >= Min.Y;
        public bool Equals(WorldReservation other) => string.Equals(Id, other.Id, StringComparison.Ordinal) && string.Equals(OwnerId, other.OwnerId, StringComparison.Ordinal) && string.Equals(Kind, other.Kind, StringComparison.Ordinal) && Priority == other.Priority && Min == other.Min && Max == other.Max;
        public override bool Equals(object obj) => obj is WorldReservation other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, OwnerId, Kind, Priority, Min, Max);
    }

    public readonly struct WorldReservationConflict : IEquatable<WorldReservationConflict>
    {
        public WorldReservation Existing { get; }
        public WorldReservation Requested { get; }

        public WorldReservationConflict(WorldReservation existing, WorldReservation requested)
        {
            Existing = existing;
            Requested = requested;
        }

        public bool Equals(WorldReservationConflict other) => Existing.Equals(other.Existing) && Requested.Equals(other.Requested);
        public override bool Equals(object obj) => obj is WorldReservationConflict other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Existing, Requested);
    }

    /// <summary>
    /// Chunk-indexed world-space reservation store. Reservations are authoritative world truth,
    /// not chunk-local state, so cross-chunk footprints remain queryable while chunks stream.
    /// </summary>
    public sealed class WorldReservationMap
    {
        private readonly int chunkSize;
        private readonly Dictionary<ChunkCoord, List<WorldReservation>> byChunk = new Dictionary<ChunkCoord, List<WorldReservation>>();
        private readonly Dictionary<string, WorldReservation> byId = new Dictionary<string, WorldReservation>(StringComparer.Ordinal);

        public WorldReservationMap(int chunkSize = 64)
        {
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
            this.chunkSize = chunkSize;
        }

        public int ChunkSize => chunkSize;
        public int Count => byId.Count;

        public bool TryReserve(WorldReservation reservation, out WorldReservationConflict conflict)
        {
            if (byId.ContainsKey(reservation.Id))
            {
                conflict = new WorldReservationConflict(byId[reservation.Id], reservation);
                return false;
            }

            var candidates = new List<WorldReservation>();
            CollectIntersecting(reservation.Min, reservation.Max, candidates);
            candidates.Sort(CompareReservations);
            for (int i = 0; i < candidates.Count; i++)
            {
                WorldReservation existing = candidates[i];
                if (existing.Intersects(reservation.Min, reservation.Max) && !string.Equals(existing.OwnerId, reservation.OwnerId, StringComparison.Ordinal))
                {
                    conflict = new WorldReservationConflict(existing, reservation);
                    return false;
                }
            }

            byId.Add(reservation.Id, reservation);
            Index(reservation);
            conflict = default(WorldReservationConflict);
            return true;
        }

        public bool Remove(string reservationId)
        {
            if (string.IsNullOrWhiteSpace(reservationId) || !byId.TryGetValue(reservationId, out WorldReservation reservation))
                return false;
            byId.Remove(reservationId);
            Unindex(reservation);
            return true;
        }

        public bool TryGet(string reservationId, out WorldReservation reservation) => byId.TryGetValue(reservationId, out reservation);

        public bool IsReserved(WorldPosition position, string ownerId = null)
        {
            var candidates = new List<WorldReservation>();
            CollectContaining(position, candidates);
            for (int i = 0; i < candidates.Count; i++)
                if (ownerId == null || !string.Equals(candidates[i].OwnerId, ownerId, StringComparison.Ordinal)) return true;
            return false;
        }

        public bool CanReserve(WorldPosition min, WorldPosition max, string ownerId = null)
        {
            var candidates = new List<WorldReservation>();
            CollectIntersecting(min, max, candidates);
            for (int i = 0; i < candidates.Count; i++)
                if (ownerId == null || !string.Equals(candidates[i].OwnerId, ownerId, StringComparison.Ordinal)) return false;
            return true;
        }

        public void CollectContaining(WorldPosition position, List<WorldReservation> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            ChunkCoord chunk = new ChunkCoord(FloorDiv(position.X, chunkSize), FloorDiv(position.Y, chunkSize));
            if (!byChunk.TryGetValue(chunk, out List<WorldReservation> reservations)) return;
            for (int i = 0; i < reservations.Count; i++) if (reservations[i].Contains(position)) output.Add(reservations[i]);
            output.Sort(CompareReservations);
        }

        public void CollectIntersecting(WorldPosition min, WorldPosition max, List<WorldReservation> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (max.X < min.X || max.Y < min.Y) return;
            int minChunkX = FloorDiv(min.X, chunkSize);
            int maxChunkX = FloorDiv(max.X, chunkSize);
            int minChunkY = FloorDiv(min.Y, chunkSize);
            int maxChunkY = FloorDiv(max.Y, chunkSize);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int x = minChunkX; x <= maxChunkX; x++)
            {
                for (int y = minChunkY; y <= maxChunkY; y++)
                {
                    if (!byChunk.TryGetValue(new ChunkCoord(x, y), out List<WorldReservation> reservations)) continue;
                    for (int i = 0; i < reservations.Count; i++)
                    {
                        WorldReservation reservation = reservations[i];
                        if (reservation.Intersects(min, max) && seen.Add(reservation.Id)) output.Add(reservation);
                    }
                }
            }
            output.Sort(CompareReservations);
        }

        public void Clear()
        {
            byChunk.Clear();
            byId.Clear();
        }

        private void Index(WorldReservation reservation)
        {
            int minChunkX = FloorDiv(reservation.Min.X, chunkSize);
            int maxChunkX = FloorDiv(reservation.Max.X, chunkSize);
            int minChunkY = FloorDiv(reservation.Min.Y, chunkSize);
            int maxChunkY = FloorDiv(reservation.Max.Y, chunkSize);
            for (int x = minChunkX; x <= maxChunkX; x++)
                for (int y = minChunkY; y <= maxChunkY; y++)
                {
                    ChunkCoord chunk = new ChunkCoord(x, y);
                    if (!byChunk.TryGetValue(chunk, out List<WorldReservation> reservations))
                    {
                        reservations = new List<WorldReservation>();
                        byChunk.Add(chunk, reservations);
                    }
                    reservations.Add(reservation);
                }
        }

        private void Unindex(WorldReservation reservation)
        {
            int minChunkX = FloorDiv(reservation.Min.X, chunkSize);
            int maxChunkX = FloorDiv(reservation.Max.X, chunkSize);
            int minChunkY = FloorDiv(reservation.Min.Y, chunkSize);
            int maxChunkY = FloorDiv(reservation.Max.Y, chunkSize);
            for (int x = minChunkX; x <= maxChunkX; x++)
                for (int y = minChunkY; y <= maxChunkY; y++)
                {
                    ChunkCoord chunk = new ChunkCoord(x, y);
                    if (!byChunk.TryGetValue(chunk, out List<WorldReservation> reservations)) continue;
                    reservations.RemoveAll(r => string.Equals(r.Id, reservation.Id, StringComparison.Ordinal));
                    if (reservations.Count == 0) byChunk.Remove(chunk);
                }
        }

        private static int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            if (remainder != 0 && ((remainder < 0) != (divisor < 0))) quotient--;
            return quotient;
        }

        private static int CompareReservations(WorldReservation left, WorldReservation right)
        {
            int c = string.CompareOrdinal(left.Id, right.Id);
            if (c != 0) return c;
            c = string.CompareOrdinal(left.OwnerId, right.OwnerId);
            if (c != 0) return c;
            c = left.Priority.CompareTo(right.Priority);
            if (c != 0) return c;
            c = left.Min.X.CompareTo(right.Min.X);
            if (c != 0) return c;
            return left.Min.Y.CompareTo(right.Min.Y);
        }
    }

    public sealed class WorldPlanPlacementReservationPolicy : IWorldPlanPlacementFeasibility
    {
        private readonly WorldReservationMap reservations;
        private readonly string ownerPrefix;
        private readonly string kind;
        private readonly int priority;

        public WorldPlanPlacementReservationPolicy(WorldReservationMap reservations, string ownerPrefix = "plan", string kind = "Feature", int priority = 0)
        {
            this.reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
            if (string.IsNullOrWhiteSpace(ownerPrefix)) throw new ArgumentException("Owner prefix must not be empty.", nameof(ownerPrefix));
            if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Reservation kind must not be empty.", nameof(kind));
            this.ownerPrefix = ownerPrefix;
            this.kind = kind;
            this.priority = priority;
        }

        public bool CanPlace(WorldPlanPlacementContext context)
        {
            string ownerId = ownerPrefix + ":" + context.Node.Id;
            string reservationId = ownerId + ":" + context.Feature.FeatureId;
            var reservation = new WorldReservation(reservationId, ownerId, kind, priority, context.Anchor, context.Max);
            if (!reservations.CanReserve(reservation.Min, reservation.Max, ownerId)) return false;
            return reservations.TryReserve(reservation, out WorldReservationConflict ignored);
        }
    }

    public sealed class WorldPlanCorridorReservationTraversal : IWorldPlanCorridorTraversal
    {
        private readonly WorldReservationMap reservations;
        private readonly string ownerId;
        private readonly bool allowGoal;

        public WorldPlanCorridorReservationTraversal(WorldReservationMap reservations, string ownerId = "corridor", bool allowGoal = true)
        {
            this.reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner ID must not be empty.", nameof(ownerId));
            this.ownerId = ownerId;
            this.allowGoal = allowGoal;
        }

        public bool CanTraverse(WorldPlanCorridorContext context)
        {
            if (allowGoal && context.Position == context.Goal) return true;
            return !reservations.IsReserved(context.Position, ownerId);
        }

        public int GetTraversalCost(WorldPlanCorridorContext context) => 1;
    }

    public static class WorldPlanCorridorReservationWriter
    {
        public static bool TryReserve(WorldReservationMap reservations, WorldPlanCorridor corridor, string ownerId = "corridor", string kind = "Corridor", int priority = 0)
        {
            if (reservations == null) throw new ArgumentNullException(nameof(reservations));
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner ID must not be empty.", nameof(ownerId));
            if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Reservation kind must not be empty.", nameof(kind));
            for (int i = 0; i < corridor.Cells.Count; i++)
            {
                WorldPosition cell = corridor.Cells[i];
                var reservation = new WorldReservation(corridor.ConnectionId + ":" + i, ownerId, kind, priority, cell, cell);
                if (!reservations.TryReserve(reservation, out WorldReservationConflict ignored))
                {
                    for (int r = 0; r < i; r++) reservations.Remove(corridor.ConnectionId + ":" + r);
                    return false;
                }
            }
            return true;
        }
    }
}
