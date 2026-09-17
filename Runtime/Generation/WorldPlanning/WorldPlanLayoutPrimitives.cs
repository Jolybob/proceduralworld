using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public enum WorldPlanLayoutPortSide
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public sealed class WorldPlanLayoutSettings
    {
        public int NodeSpacing { get; }
        public int ComponentSpacing { get; }

        public WorldPlanLayoutSettings(int nodeSpacing = 2, int componentSpacing = 8)
        {
            if (nodeSpacing < 0)
                throw new ArgumentOutOfRangeException(nameof(nodeSpacing));
            if (componentSpacing < 0)
                throw new ArgumentOutOfRangeException(nameof(componentSpacing));

            NodeSpacing = nodeSpacing;
            ComponentSpacing = componentSpacing;
        }

        public static WorldPlanLayoutSettings Default => new WorldPlanLayoutSettings();
    }

    public readonly struct WorldPlanLayoutPort : IEquatable<WorldPlanLayoutPort>
    {
        public string NodeId { get; }
        public string PortId { get; }
        public WorldPosition Position { get; }
        public WorldPlanLayoutPortSide Side { get; }

        public WorldPlanLayoutPort(string nodeId, string portId, WorldPosition position, WorldPlanLayoutPortSide side)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException("Node ID must not be empty.", nameof(nodeId));
            if (string.IsNullOrWhiteSpace(portId))
                throw new ArgumentException("Port ID must not be empty.", nameof(portId));

            NodeId = nodeId;
            PortId = portId;
            Position = position;
            Side = side;
        }

        public bool Equals(WorldPlanLayoutPort other)
        {
            return string.Equals(NodeId, other.NodeId, StringComparison.Ordinal)
                && string.Equals(PortId, other.PortId, StringComparison.Ordinal)
                && Position == other.Position
                && Side == other.Side;
        }

        public override bool Equals(object obj) => obj is WorldPlanLayoutPort other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(NodeId, PortId, Position, Side);
    }

    public readonly struct WorldPlanNodeLayout : IEquatable<WorldPlanNodeLayout>
    {
        public string NodeId { get; }
        public WorldPosition Position { get; }
        public int Width { get; }
        public int Height { get; }
        public int Clearance { get; }

        public int MinX => Position.X;
        public int MinY => Position.Y;
        public int MaxX => checked(Position.X + Width - 1);
        public int MaxY => checked(Position.Y + Height - 1);
        public int OccupiedMinX => checked(MinX - Clearance);
        public int OccupiedMinY => checked(MinY - Clearance);
        public int OccupiedMaxX => checked(MaxX + Clearance);
        public int OccupiedMaxY => checked(MaxY + Clearance);

        public WorldPlanNodeLayout(string nodeId, WorldPosition position, int width, int height, int clearance)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException("Node ID must not be empty.", nameof(nodeId));
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (clearance < 0)
                throw new ArgumentOutOfRangeException(nameof(clearance));

            long maxX = (long)position.X + width - 1L;
            long maxY = (long)position.Y + height - 1L;
            long occupiedMinX = (long)position.X - clearance;
            long occupiedMinY = (long)position.Y - clearance;
            long occupiedMaxX = maxX + clearance;
            long occupiedMaxY = maxY + clearance;
            if (maxX > int.MaxValue || maxX < int.MinValue
                || maxY > int.MaxValue || maxY < int.MinValue
                || occupiedMinX > int.MaxValue || occupiedMinX < int.MinValue
                || occupiedMinY > int.MaxValue || occupiedMinY < int.MinValue
                || occupiedMaxX > int.MaxValue || occupiedMaxX < int.MinValue
                || occupiedMaxY > int.MaxValue || occupiedMaxY < int.MinValue)
            {
                throw new OverflowException("World-plan node layout exceeds the supported world coordinate range.");
            }

            NodeId = nodeId;
            Position = position;
            Width = width;
            Height = height;
            Clearance = clearance;
        }

        public bool Intersects(WorldPlanNodeLayout other, bool includeClearance = true)
        {
            int leftA = includeClearance ? OccupiedMinX : MinX;
            int rightA = includeClearance ? OccupiedMaxX : MaxX;
            int bottomA = includeClearance ? OccupiedMinY : MinY;
            int topA = includeClearance ? OccupiedMaxY : MaxY;
            int leftB = includeClearance ? other.OccupiedMinX : other.MinX;
            int rightB = includeClearance ? other.OccupiedMaxX : other.MaxX;
            int bottomB = includeClearance ? other.OccupiedMinY : other.MinY;
            int topB = includeClearance ? other.OccupiedMaxY : other.MaxY;

            return leftA <= rightB && rightA >= leftB
                && bottomA <= topB && topA >= bottomB;
        }

        public bool Equals(WorldPlanNodeLayout other)
        {
            return string.Equals(NodeId, other.NodeId, StringComparison.Ordinal)
                && Position == other.Position
                && Width == other.Width
                && Height == other.Height
                && Clearance == other.Clearance;
        }

        public override bool Equals(object obj) => obj is WorldPlanNodeLayout other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(NodeId, Position, Width, Height, Clearance);
    }

    public sealed class WorldPlanLayout
    {
        private readonly List<WorldPlanNodeLayout> nodes;
        private readonly List<WorldPlanLayoutPort> ports;
        private readonly Dictionary<string, WorldPlanNodeLayout> nodeById;
        private readonly Dictionary<string, WorldPlanLayoutPort> portByKey;

        public IReadOnlyList<WorldPlanNodeLayout> Nodes => nodes;
        public IReadOnlyList<WorldPlanLayoutPort> Ports => ports;

        internal WorldPlanLayout(List<WorldPlanNodeLayout> nodes, List<WorldPlanLayoutPort> ports)
        {
            this.nodes = nodes;
            this.ports = ports;
            nodeById = new Dictionary<string, WorldPlanNodeLayout>(StringComparer.Ordinal);
            portByKey = new Dictionary<string, WorldPlanLayoutPort>(StringComparer.Ordinal);

            for (int i = 0; i < nodes.Count; i++)
                nodeById[nodes[i].NodeId] = nodes[i];
            for (int i = 0; i < ports.Count; i++)
                portByKey[MakePortKey(ports[i].NodeId, ports[i].PortId)] = ports[i];
        }

        public bool TryGetNode(string nodeId, out WorldPlanNodeLayout node)
        {
            return nodeById.TryGetValue(nodeId, out node);
        }

        public bool TryGetPort(string nodeId, string portId, out WorldPlanLayoutPort port)
        {
            return portByKey.TryGetValue(MakePortKey(nodeId, portId), out port);
        }

        public static string MakePortKey(string nodeId, string portId)
        {
            return nodeId + "::" + portId;
        }
    }

    public sealed class WorldPlanLayoutResult
    {
        public WorldPlanLayout Layout { get; }
        public IReadOnlyList<WorldPlanValidationIssue> Issues { get; }
        public bool Succeeded
        {
            get
            {
                if (Layout == null || Issues == null)
                    return false;

                for (int i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i].Severity == WorldPlanValidationSeverity.Error)
                        return false;
                }

                return true;
            }
        }

        internal WorldPlanLayoutResult(WorldPlanLayout layout, List<WorldPlanValidationIssue> issues)
        {
            Layout = layout;
            Issues = issues;
        }
    }
}
