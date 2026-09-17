using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public enum WorldEditOperationKind : byte
    {
        SetCell = 0,
        SetTile = 1,
        SetResource = 2,
        ClearResource = 3,
        SetStructure = 4,
        ClearStructure = 5
    }

    public readonly struct WorldCellChange : IEquatable<WorldCellChange>
    {
        public readonly WorldPosition Position;
        public readonly GeneratedCell Before;
        public readonly GeneratedCell After;
        public readonly WorldEditOperationKind Operation;

        public WorldCellChange(
            WorldPosition position,
            GeneratedCell before,
            GeneratedCell after,
            WorldEditOperationKind operation)
        {
            Position = position;
            Before = before;
            After = after;
            Operation = operation;
        }

        public bool Equals(WorldCellChange other)
        {
            return Position == other.Position &&
                   WorldPersistenceUtility.AreEqual(Before, other.Before) &&
                   WorldPersistenceUtility.AreEqual(After, other.After) &&
                   Operation == other.Operation;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldCellChange other && Equals(other);
        }

        public override int GetHashCode()
        {
            int beforeHash = WorldPersistenceUtility.GetCellHashCode(Before);
            int afterHash = WorldPersistenceUtility.GetCellHashCode(After);
            int hash = HashCode.Combine(Position, beforeHash);
            hash = HashCode.Combine(hash, afterHash);
            return HashCode.Combine(hash, Operation);
        }
    }

    public interface IWorldChangeJournal
    {
        IReadOnlyList<WorldCellChange> Changes { get; }
        void Record(WorldCellChange change);
        void Clear();
    }

    public sealed class InMemoryWorldChangeJournal : IWorldChangeJournal
    {
        private readonly List<WorldCellChange> changes = new List<WorldCellChange>();

        public IReadOnlyList<WorldCellChange> Changes => changes;

        public void Record(WorldCellChange change)
        {
            changes.Add(change);
        }

        public void Clear()
        {
            changes.Clear();
        }
    }
}
