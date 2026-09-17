using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public readonly struct WorldPlanCorridor : IEquatable<WorldPlanCorridor>
    {
        public string ConnectionId { get; }
        public string SourceNodeId { get; }
        public string TargetNodeId { get; }
        public IReadOnlyList<WorldPosition> Cells { get; }

        public WorldPlanCorridor(string connectionId, string sourceNodeId, string targetNodeId, IReadOnlyList<WorldPosition> cells)
        {
            if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentException("Connection ID must not be empty.", nameof(connectionId));
            if (string.IsNullOrWhiteSpace(sourceNodeId)) throw new ArgumentException("Source node ID must not be empty.", nameof(sourceNodeId));
            if (string.IsNullOrWhiteSpace(targetNodeId)) throw new ArgumentException("Target node ID must not be empty.", nameof(targetNodeId));
            if (cells == null || cells.Count == 0) throw new ArgumentException("Corridor must contain at least one cell.", nameof(cells));
            ConnectionId = connectionId;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
            Cells = cells;
        }

        public bool Equals(WorldPlanCorridor other) => string.Equals(ConnectionId, other.ConnectionId, StringComparison.Ordinal) && string.Equals(SourceNodeId, other.SourceNodeId, StringComparison.Ordinal) && string.Equals(TargetNodeId, other.TargetNodeId, StringComparison.Ordinal) && CellsEqual(Cells, other.Cells);
        public override bool Equals(object obj) => obj is WorldPlanCorridor other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ConnectionId, SourceNodeId, TargetNodeId, Cells.Count);

        private static bool CellsEqual(IReadOnlyList<WorldPosition> a, IReadOnlyList<WorldPosition> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }

    public sealed class WorldPlanCorridorPlanningSettings
    {
        public int SearchMargin { get; }
        public int MaximumExpandedCells { get; }
        public int ChunkSize { get; }

        public WorldPlanCorridorPlanningSettings(int searchMargin = 16, int maximumExpandedCells = 16384, int chunkSize = 64)
        {
            if (searchMargin < 0) throw new ArgumentOutOfRangeException(nameof(searchMargin));
            if (maximumExpandedCells <= 0) throw new ArgumentOutOfRangeException(nameof(maximumExpandedCells));
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
            SearchMargin = searchMargin;
            MaximumExpandedCells = maximumExpandedCells;
            ChunkSize = chunkSize;
        }

        public static WorldPlanCorridorPlanningSettings Default => new WorldPlanCorridorPlanningSettings();
    }

    public readonly struct WorldPlanCorridorContext
    {
        public WorldPlanConnection Connection { get; }
        public WorldPosition Position { get; }
        public WorldPosition Goal { get; }

        public WorldPlanCorridorContext(WorldPlanConnection connection, WorldPosition position, WorldPosition goal)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Position = position;
            Goal = goal;
        }
    }

    public interface IWorldPlanCorridorTraversal
    {
        bool CanTraverse(WorldPlanCorridorContext context);
        int GetTraversalCost(WorldPlanCorridorContext context);
    }

    public sealed class WorldPlanCorridorResult
    {
        public IReadOnlyList<WorldPlanCorridor> Corridors { get; }
        public IReadOnlyList<WorldPlanValidationIssue> Issues { get; }
        public bool Succeeded
        {
            get { for (int i = 0; i < Issues.Count; i++) if (Issues[i].Severity == WorldPlanValidationSeverity.Error) return false; return true; }
        }

        internal WorldPlanCorridorResult(List<WorldPlanCorridor> corridors, List<WorldPlanValidationIssue> issues) { Corridors = corridors; Issues = issues; }
    }

    /// <summary>Deterministic world-space A* realization of compiled plan connections.</summary>
    public sealed class WorldPlanCorridorPlanner
    {
        private readonly struct SearchNode : IComparable<SearchNode>
        {
            public readonly WorldPosition Position;
            public readonly int Cost;
            public readonly int Estimate;
            public SearchNode(WorldPosition position, int cost, int estimate) { Position = position; Cost = cost; Estimate = estimate; }
            public int CompareTo(SearchNode other)
            {
                int c = Estimate.CompareTo(other.Estimate);
                if (c != 0) return c;
                c = Cost.CompareTo(other.Cost);
                if (c != 0) return c;
                c = Position.X.CompareTo(other.Position.X);
                return c != 0 ? c : Position.Y.CompareTo(other.Position.Y);
            }
        }

        public WorldPlanCorridorResult Plan(WorldPlan plan, WorldPlanLayout layout, IWorldPlanCorridorTraversal traversal = null, WorldPlanCorridorPlanningSettings settings = null)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            settings = settings ?? WorldPlanCorridorPlanningSettings.Default;
            var corridors = new List<WorldPlanCorridor>();
            var issues = new List<WorldPlanValidationIssue>();
            var connections = new List<WorldPlanConnection>(plan.Connections);
            connections.Sort(CompareConnections);

            for (int i = 0; i < connections.Count; i++)
            {
                WorldPlanConnection connection = connections[i];
                if (connection == null || connection.SourceNode == null || connection.TargetNode == null) continue;
                if (!layout.TryGetPort(connection.SourceNode.Id, connection.SourcePort.Id, out WorldPlanLayoutPort source) || !layout.TryGetPort(connection.TargetNode.Id, connection.TargetPort.Id, out WorldPlanLayoutPort target))
                {
                    issues.Add(new WorldPlanValidationIssue(WorldPlanValidationSeverity.Error, "MissingCorridorAnchor", "Connection '" + connection.Id + "' has no resolved world-space port anchors.", connection.SourceNode.Id, connection.Id));
                    continue;
                }

                if (!TryFindPath(connection, source.Position, target.Position, traversal, settings, out List<WorldPosition> path))
                {
                    issues.Add(new WorldPlanValidationIssue(WorldPlanValidationSeverity.Error, "NoCorridorPath", "No traversable world-space corridor was found for connection '" + connection.Id + "'.", connection.SourceNode.Id, connection.Id));
                    continue;
                }
                corridors.Add(new WorldPlanCorridor(connection.Id, connection.SourceNode.Id, connection.TargetNode.Id, path));
            }

            return new WorldPlanCorridorResult(corridors, issues);
        }

        private static bool TryFindPath(WorldPlanConnection connection, WorldPosition start, WorldPosition goal, IWorldPlanCorridorTraversal traversal, WorldPlanCorridorPlanningSettings settings, out List<WorldPosition> path)
        {
            path = null;
            if (start == goal) { path = new List<WorldPosition> { start }; return traversal == null || traversal.CanTraverse(new WorldPlanCorridorContext(connection, start, goal)); }
            if (traversal == null) return BuildManhattanPath(start, goal, out path);

            var open = new List<SearchNode>();
            var closed = new HashSet<WorldPosition>();
            var parent = new Dictionary<WorldPosition, WorldPosition>();
            var costs = new Dictionary<WorldPosition, int>();
            int minX = Math.Min(start.X, goal.X) - settings.SearchMargin;
            int maxX = Math.Max(start.X, goal.X) + settings.SearchMargin;
            int minY = Math.Min(start.Y, goal.Y) - settings.SearchMargin;
            int maxY = Math.Max(start.Y, goal.Y) + settings.SearchMargin;
            costs[start] = 0;
            open.Add(new SearchNode(start, 0, Manhattan(start, goal)));
            int expanded = 0;

            while (open.Count > 0 && expanded < settings.MaximumExpandedCells)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++) if (open[i].CompareTo(open[best]) < 0) best = i;
                SearchNode current = open[best]; open.RemoveAt(best);
                if (!closed.Add(current.Position)) continue;
                expanded++;
                if (current.Position == goal) { path = Reconstruct(parent, goal); return true; }

                for (int direction = 0; direction < 4; direction++)
                {
                    int dx = direction == 0 ? 1 : direction == 1 ? -1 : 0;
                    int dy = direction == 2 ? 1 : direction == 3 ? -1 : 0;
                    long nx = (long)current.Position.X + dx, ny = (long)current.Position.Y + dy;
                    if (nx < minX || nx > maxX || ny < minY || ny > maxY || nx < int.MinValue || nx > int.MaxValue || ny < int.MinValue || ny > int.MaxValue) continue;
                    var next = new WorldPosition((int)nx, (int)ny);
                    if (closed.Contains(next)) continue;
                    var context = new WorldPlanCorridorContext(connection, next, goal);
                    if (!traversal.CanTraverse(context)) continue;
                    int step = traversal.GetTraversalCost(context);
                    if (step <= 0) step = 1;
                    int newCost = checked(current.Cost + step);
                    if (!costs.TryGetValue(next, out int oldCost) || newCost < oldCost)
                    {
                        costs[next] = newCost;
                        parent[next] = current.Position;
                        open.Add(new SearchNode(next, newCost, checked(newCost + Manhattan(next, goal))));
                    }
                }
            }
            return false;
        }

        private static bool BuildManhattanPath(WorldPosition start, WorldPosition goal, out List<WorldPosition> path)
        {
            path = new List<WorldPosition>();
            int x = start.X, y = start.Y;
            path.Add(start);
            while (x != goal.X) { x += x < goal.X ? 1 : -1; path.Add(new WorldPosition(x, y)); }
            while (y != goal.Y) { y += y < goal.Y ? 1 : -1; path.Add(new WorldPosition(x, y)); }
            return true;
        }

        private static List<WorldPosition> Reconstruct(Dictionary<WorldPosition, WorldPosition> parent, WorldPosition goal)
        {
            var reversed = new List<WorldPosition> { goal };
            WorldPosition current = goal;
            while (parent.TryGetValue(current, out WorldPosition previous)) { reversed.Add(previous); current = previous; }
            reversed.Reverse(); return reversed;
        }

        private static int Manhattan(WorldPosition a, WorldPosition b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        private static int CompareConnections(WorldPlanConnection a, WorldPlanConnection b)
        {
            if (a == null && b == null) return 0; if (a == null) return 1; if (b == null) return -1;
            int c = string.CompareOrdinal(a.SourceNode.Id, b.SourceNode.Id); if (c != 0) return c;
            c = string.CompareOrdinal(a.TargetNode.Id, b.TargetNode.Id); if (c != 0) return c;
            return string.CompareOrdinal(a.Id, b.Id);
        }
    }
}
