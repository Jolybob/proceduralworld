using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public interface IWorldPlanFeatureResolver
    {
        IWorldFeaturePlacementDefinition Resolve(WorldPlanNode node);
    }

    public readonly struct WorldPlanPlacementContext
    {
        public WorldPlanNode Node { get; }
        public WorldPlanNodeLayout Layout { get; }
        public IWorldFeaturePlacementDefinition Feature { get; }
        public WorldPosition Anchor { get; }
        public WorldPosition Max { get; }

        public WorldPlanPlacementContext(WorldPlanNode node, WorldPlanNodeLayout layout, IWorldFeaturePlacementDefinition feature, WorldPosition anchor)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Feature = feature ?? throw new ArgumentNullException(nameof(feature));
            Layout = layout;
            Anchor = anchor;
            long maxX = (long)anchor.X + feature.Width - 1L;
            long maxY = (long)anchor.Y + feature.Height - 1L;
            if (maxX > int.MaxValue || maxX < int.MinValue || maxY > int.MaxValue || maxY < int.MinValue)
                throw new OverflowException("Planned feature footprint exceeds the supported world coordinate range.");
            Max = new WorldPosition((int)maxX, (int)maxY);
        }
    }

    /// <summary>Pure policy boundary for terrain, cave, water, reservation, or other world-truth checks.</summary>
    public interface IWorldPlanPlacementFeasibility
    {
        bool CanPlace(WorldPlanPlacementContext context);
    }

    public readonly struct WorldPlanFeaturePlacement : IEquatable<WorldPlanFeaturePlacement>
    {
        public string NodeId { get; }
        public WorldFeaturePlacement Placement { get; }

        public WorldPlanFeaturePlacement(string nodeId, WorldFeaturePlacement placement)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("Node ID must not be empty.", nameof(nodeId));
            NodeId = nodeId;
            Placement = placement;
        }

        public bool Equals(WorldPlanFeaturePlacement other) => string.Equals(NodeId, other.NodeId, StringComparison.Ordinal) && Placement.Equals(other.Placement);
        public override bool Equals(object obj) => obj is WorldPlanFeaturePlacement other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(NodeId, Placement);
    }

    public sealed class WorldPlanFeatureLoweringSettings
    {
        public int ChunkSize { get; }
        public int SearchRadius { get; }
        public int CandidateStep { get; }

        public WorldPlanFeatureLoweringSettings(int chunkSize = 64, int searchRadius = 8, int candidateStep = 1)
        {
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
            if (searchRadius < 0) throw new ArgumentOutOfRangeException(nameof(searchRadius));
            if (candidateStep <= 0) throw new ArgumentOutOfRangeException(nameof(candidateStep));
            ChunkSize = chunkSize;
            SearchRadius = searchRadius;
            CandidateStep = candidateStep;
        }

        public static WorldPlanFeatureLoweringSettings Default => new WorldPlanFeatureLoweringSettings();
    }

    public sealed class WorldPlanFeatureLoweringResult
    {
        public IReadOnlyList<WorldPlanFeaturePlacement> Placements { get; }
        public IReadOnlyList<WorldPlanValidationIssue> Issues { get; }
        public bool Succeeded
        {
            get
            {
                for (int i = 0; i < Issues.Count; i++)
                    if (Issues[i].Severity == WorldPlanValidationSeverity.Error) return false;
                return true;
            }
        }

        internal WorldPlanFeatureLoweringResult(List<WorldPlanFeaturePlacement> placements, List<WorldPlanValidationIssue> issues)
        {
            Placements = placements;
            Issues = issues;
        }
    }

    /// <summary>
    /// Lowers semantic plan nodes into the existing world-space feature kernel. If a feasibility
    /// policy is supplied, candidates are searched deterministically around the preferred anchor.
    /// </summary>
    public sealed class WorldPlanFeatureLowerer
    {
        public WorldPlanFeatureLoweringResult Lower(WorldPlan plan, WorldPlanLayout layout, IWorldPlanFeatureResolver resolver, IWorldPlanPlacementFeasibility feasibility = null, WorldPlanFeatureLoweringSettings settings = null)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            settings = settings ?? WorldPlanFeatureLoweringSettings.Default;

            var placements = new List<WorldPlanFeaturePlacement>();
            var issues = new List<WorldPlanValidationIssue>();
            var nodes = new List<WorldPlanNode>(plan.Nodes);
            nodes.Sort(CompareNodes);

            for (int i = 0; i < nodes.Count; i++)
            {
                WorldPlanNode node = nodes[i];
                if (node == null || !layout.TryGetNode(node.Id, out WorldPlanNodeLayout nodeLayout)) continue;
                IWorldFeaturePlacementDefinition feature = resolver.Resolve(node);
                if (feature == null)
                {
                    issues.Add(new WorldPlanValidationIssue(WorldPlanValidationSeverity.Error, "MissingFeatureBinding", "No world feature definition was resolved for plan node '" + node.Id + "'.", node.Id));
                    continue;
                }

                WorldPosition preferred = new WorldPosition(
                    checked(nodeLayout.Position.X + Math.Max(0, (nodeLayout.Width - feature.Width) / 2)),
                    checked(nodeLayout.Position.Y + Math.Max(0, (nodeLayout.Height - feature.Height) / 2)));

                if (!TryFindAnchor(node, nodeLayout, feature, preferred, feasibility, settings, out WorldPosition anchor))
                {
                    issues.Add(new WorldPlanValidationIssue(WorldPlanValidationSeverity.Error, "NoFeasibleFeaturePlacement", "No feasible world-space anchor was found for plan node '" + node.Id + "' within the configured lowering search radius.", node.Id));
                    continue;
                }

                var placement = new WorldFeaturePlacement(feature.FeatureId, anchor, new ChunkCoord(FloorDiv(anchor.X, settings.ChunkSize), FloorDiv(anchor.Y, settings.ChunkSize)), feature.Width, feature.Height);
                placements.Add(new WorldPlanFeaturePlacement(node.Id, placement));
            }

            placements.Sort(ComparePlacements);
            return new WorldPlanFeatureLoweringResult(placements, issues);
        }

        private static bool TryFindAnchor(WorldPlanNode node, WorldPlanNodeLayout layout, IWorldFeaturePlacementDefinition feature, WorldPosition preferred, IWorldPlanPlacementFeasibility feasibility, WorldPlanFeatureLoweringSettings settings, out WorldPosition anchor)
        {
            if (feasibility == null) { anchor = preferred; return true; }
            for (int distance = 0; distance <= settings.SearchRadius; distance += settings.CandidateStep)
            {
                for (int dx = -distance; dx <= distance; dx += settings.CandidateStep)
                {
                    int remaining = distance - Math.Abs(dx);
                    if (remaining == 0)
                    {
                        if (TryCandidate(node, layout, feature, preferred, dx, 0, feasibility, out anchor)) return true;
                    }
                    else
                    {
                        if (TryCandidate(node, layout, feature, preferred, dx, -remaining, feasibility, out anchor)) return true;
                        if (TryCandidate(node, layout, feature, preferred, dx, remaining, feasibility, out anchor)) return true;
                    }
                }
            }
            anchor = default(WorldPosition);
            return false;
        }

        private static bool TryCandidate(WorldPlanNode node, WorldPlanNodeLayout layout, IWorldFeaturePlacementDefinition feature, WorldPosition preferred, int dx, int dy, IWorldPlanPlacementFeasibility feasibility, out WorldPosition anchor)
        {
            long x = (long)preferred.X + dx;
            long y = (long)preferred.Y + dy;
            if (x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue) { anchor = default(WorldPosition); return false; }
            anchor = new WorldPosition((int)x, (int)y);
            return feasibility.CanPlace(new WorldPlanPlacementContext(node, layout, feature, anchor));
        }

        private static int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            if (remainder != 0 && ((remainder < 0) != (divisor < 0))) quotient--;
            return quotient;
        }

        private static int CompareNodes(WorldPlanNode left, WorldPlanNode right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return string.CompareOrdinal(left.Id, right.Id);
        }

        private static int ComparePlacements(WorldPlanFeaturePlacement left, WorldPlanFeaturePlacement right) => string.CompareOrdinal(left.NodeId, right.NodeId);
    }
}
