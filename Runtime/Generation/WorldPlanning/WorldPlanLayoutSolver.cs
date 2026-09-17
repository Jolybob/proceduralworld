using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Deterministically converts a semantic world plan into world-space node footprints and
    /// port anchors. The solver is independent from chunk residency and Unity editor state.
    /// </summary>
    public sealed class WorldPlanLayoutSolver
    {
        private sealed class Component
        {
            public readonly List<List<WorldPlanNode>> Layers = new List<List<WorldPlanNode>>();
            public int Width;
        }

        public WorldPlanLayoutResult Solve(WorldPlan plan, WorldPlanLayoutSettings settings = null)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            settings = settings ?? WorldPlanLayoutSettings.Default;
            var issues = new List<WorldPlanValidationIssue>();
            var orderedNodes = new List<WorldPlanNode>(plan.Nodes);
            orderedNodes.Sort(CompareNodes);

            var nodeById = new Dictionary<string, WorldPlanNode>(StringComparer.Ordinal);
            for (int i = 0; i < orderedNodes.Count; i++)
            {
                WorldPlanNode node = orderedNodes[i];
                if (node == null)
                    continue;
                if (!nodeById.TryAdd(node.Id, node))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "DuplicateLayoutNode",
                        "World plan contains duplicate node ID: " + node.Id,
                        node.Id));
                }
            }

            var adjacency = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (int i = 0; i < orderedNodes.Count; i++)
            {
                WorldPlanNode node = orderedNodes[i];
                if (node != null && !adjacency.ContainsKey(node.Id))
                    adjacency.Add(node.Id, new List<string>());
            }

            var orderedConnections = new List<WorldPlanConnection>(plan.Connections);
            orderedConnections.Sort(CompareConnections);
            for (int i = 0; i < orderedConnections.Count; i++)
            {
                WorldPlanConnection connection = orderedConnections[i];
                if (connection == null || connection.SourceNode == null || connection.TargetNode == null)
                    continue;
                if (!adjacency.ContainsKey(connection.SourceNode.Id) || !adjacency.ContainsKey(connection.TargetNode.Id))
                    continue;

                adjacency[connection.SourceNode.Id].Add(connection.TargetNode.Id);
                if (!string.Equals(connection.SourceNode.Id, connection.TargetNode.Id, StringComparison.Ordinal))
                    adjacency[connection.TargetNode.Id].Add(connection.SourceNode.Id);
            }

            foreach (KeyValuePair<string, List<string>> pair in adjacency)
                pair.Value.Sort(StringComparer.Ordinal);

            var visited = new HashSet<string>(StringComparer.Ordinal);
            var components = new List<Component>();
            for (int i = 0; i < orderedNodes.Count; i++)
            {
                WorldPlanNode root = orderedNodes[i];
                if (root == null || !visited.Add(root.Id))
                    continue;

                var component = new Component();
                var queue = new Queue<WorldPlanNode>();
                var layerById = new Dictionary<string, int>(StringComparer.Ordinal);
                queue.Enqueue(root);
                layerById[root.Id] = 0;

                while (queue.Count > 0)
                {
                    WorldPlanNode current = queue.Dequeue();
                    int layer = layerById[current.Id];
                    while (component.Layers.Count <= layer)
                        component.Layers.Add(new List<WorldPlanNode>());
                    component.Layers[layer].Add(current);

                    List<string> neighbors = adjacency[current.Id];
                    for (int n = 0; n < neighbors.Count; n++)
                    {
                        string neighborId = neighbors[n];
                        if (!visited.Add(neighborId))
                            continue;
                        layerById[neighborId] = layer + 1;
                        queue.Enqueue(nodeById[neighborId]);
                    }
                }

                for (int layer = 0; layer < component.Layers.Count; layer++)
                    component.Layers[layer].Sort(CompareNodes);

                components.Add(component);
            }

            var nodeLayouts = new List<WorldPlanNodeLayout>(orderedNodes.Count);
            long componentCursorX = 0;
            for (int componentIndex = 0; componentIndex < components.Count; componentIndex++)
            {
                Component component = components[componentIndex];
                var layerWidths = new int[component.Layers.Count];
                var layerHeights = new int[component.Layers.Count];
                int componentHeight = 0;

                for (int layer = 0; layer < component.Layers.Count; layer++)
                {
                    int width = 0;
                    int height = 0;
                    List<WorldPlanNode> layerNodes = component.Layers[layer];
                    for (int i = 0; i < layerNodes.Count; i++)
                    {
                        WorldPlanNode node = layerNodes[i];
                        int occupiedWidth = GetOccupiedWidth(node);
                        int occupiedHeight = GetOccupiedHeight(node);
                        width = checked(width + (i == 0 ? 0 : settings.NodeSpacing) + occupiedWidth);
                        height = Math.Max(height, occupiedHeight);
                    }

                    layerWidths[layer] = width;
                    layerHeights[layer] = height;
                    component.Width = Math.Max(component.Width, width);
                }

                for (int layer = 0; layer < layerHeights.Length; layer++)
                {
                    componentHeight = checked(componentHeight + layerHeights[layer]);
                    if (layer + 1 < layerHeights.Length)
                        componentHeight = checked(componentHeight + settings.NodeSpacing);
                }

                int layerY = 0;
                for (int layer = 0; layer < component.Layers.Count; layer++)
                {
                    List<WorldPlanNode> layerNodes = component.Layers[layer];
                    int rowOffsetX = (component.Width - layerWidths[layer]) / 2;
                    int x = checked((int)(componentCursorX + rowOffsetX));
                    for (int i = 0; i < layerNodes.Count; i++)
                    {
                        WorldPlanNode node = layerNodes[i];
                        int clearance = node.Type.MinimumClearance;
                        int width = node.Type.MinimumWidth;
                        int height = node.Type.MinimumHeight;
                        int occupiedWidth = GetOccupiedWidth(node);
                        int occupiedHeight = GetOccupiedHeight(node);
                        int yOffset = (layerHeights[layer] - occupiedHeight) / 2;
                        int nodeX = checked(x + clearance);
                        int nodeY = checked(layerY + yOffset + clearance);

                        nodeLayouts.Add(new WorldPlanNodeLayout(
                            node.Id,
                            new WorldPosition(nodeX, nodeY),
                            width,
                            height,
                            clearance));

                        x = checked(x + occupiedWidth + settings.NodeSpacing);
                    }

                    layerY = checked(layerY + layerHeights[layer] + settings.NodeSpacing);
                }

                componentCursorX = checked(componentCursorX + component.Width + settings.ComponentSpacing);
            }

            nodeLayouts.Sort(CompareLayouts);
            ValidateNoOverlap(nodeLayouts, issues);
            var portLayouts = BuildPortLayouts(orderedNodes, nodeLayouts, orderedConnections);
            return new WorldPlanLayoutResult(new WorldPlanLayout(nodeLayouts, portLayouts), issues);
        }

        private static List<WorldPlanLayoutPort> BuildPortLayouts(
            List<WorldPlanNode> nodes,
            List<WorldPlanNodeLayout> nodeLayouts,
            List<WorldPlanConnection> connections)
        {
            var result = new List<WorldPlanLayoutPort>();
            var nodeLayoutById = new Dictionary<string, WorldPlanNodeLayout>(StringComparer.Ordinal);
            for (int i = 0; i < nodeLayouts.Count; i++)
                nodeLayoutById[nodeLayouts[i].NodeId] = nodeLayouts[i];

            var sideByPort = new Dictionary<string, WorldPlanLayoutPortSide>(StringComparer.Ordinal);
            for (int i = 0; i < connections.Count; i++)
            {
                WorldPlanConnection connection = connections[i];
                if (connection == null)
                    continue;

                string sourceKey = WorldPlanLayout.MakePortKey(connection.SourceNode.Id, connection.SourcePort.Id);
                if (!sideByPort.ContainsKey(sourceKey))
                    sideByPort.Add(sourceKey, WorldPlanLayoutPortSide.Right);

                string targetKey = WorldPlanLayout.MakePortKey(connection.TargetNode.Id, connection.TargetPort.Id);
                if (!sideByPort.ContainsKey(targetKey))
                    sideByPort.Add(targetKey, WorldPlanLayoutPortSide.Left);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                WorldPlanNode node = nodes[i];
                if (node == null || !nodeLayoutById.TryGetValue(node.Id, out WorldPlanNodeLayout layout))
                    continue;

                var inputPorts = new List<WorldPlanPortDefinition>();
                var outputPorts = new List<WorldPlanPortDefinition>();
                var neutralPorts = new List<WorldPlanPortDefinition>();
                for (int p = 0; p < node.Type.Ports.Count; p++)
                {
                    WorldPlanPortDefinition port = node.Type.Ports[p];
                    if (port == null)
                        continue;

                    string key = WorldPlanLayout.MakePortKey(node.Id, port.Id);
                    if (sideByPort.TryGetValue(key, out WorldPlanLayoutPortSide connectedSide))
                    {
                        if (connectedSide == WorldPlanLayoutPortSide.Left)
                            inputPorts.Add(port);
                        else
                            outputPorts.Add(port);
                    }
                    else if (port.Direction == WorldPlanPortDirection.Input)
                    {
                        inputPorts.Add(port);
                    }
                    else if (port.Direction == WorldPlanPortDirection.Output)
                    {
                        outputPorts.Add(port);
                    }
                    else
                    {
                        neutralPorts.Add(port);
                    }
                }

                inputPorts.Sort(ComparePorts);
                outputPorts.Sort(ComparePorts);
                neutralPorts.Sort(ComparePorts);
                AddPorts(layout, inputPorts, WorldPlanLayoutPortSide.Left, result);
                AddPorts(layout, outputPorts, WorldPlanLayoutPortSide.Right, result);
                AddPorts(layout, neutralPorts, WorldPlanLayoutPortSide.Bottom, result);
            }

            result.Sort(ComparePortsLayout);
            return result;
        }

        private static void AddPorts(
            WorldPlanNodeLayout node,
            List<WorldPlanPortDefinition> ports,
            WorldPlanLayoutPortSide side,
            List<WorldPlanLayoutPort> output)
        {
            for (int i = 0; i < ports.Count; i++)
            {
                int span = side == WorldPlanLayoutPortSide.Left || side == WorldPlanLayoutPortSide.Right
                    ? node.Height
                    : node.Width;
                int axis = checked((i + 1) * span / (ports.Count + 1));
                WorldPosition position;
                switch (side)
                {
                    case WorldPlanLayoutPortSide.Left:
                        position = new WorldPosition(node.MinX, checked(node.MinY + axis));
                        break;
                    case WorldPlanLayoutPortSide.Right:
                        position = new WorldPosition(node.MaxX, checked(node.MinY + axis));
                        break;
                    case WorldPlanLayoutPortSide.Top:
                        position = new WorldPosition(checked(node.MinX + axis), node.MaxY);
                        break;
                    default:
                        position = new WorldPosition(checked(node.MinX + axis), node.MinY);
                        break;
                }

                output.Add(new WorldPlanLayoutPort(node.NodeId, ports[i].Id, position, side));
            }
        }

        private static void ValidateNoOverlap(List<WorldPlanNodeLayout> nodes, List<WorldPlanValidationIssue> issues)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    if (!nodes[i].Intersects(nodes[j], true))
                        continue;

                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "LayoutOverlap",
                        "World-plan layout overlaps occupied footprints between '"
                        + nodes[i].NodeId + "' and '" + nodes[j].NodeId + "'.",
                        nodes[i].NodeId));
                }
            }
        }

        private static int GetOccupiedWidth(WorldPlanNode node)
        {
            return checked(node.Type.MinimumWidth + node.Type.MinimumClearance * 2);
        }

        private static int GetOccupiedHeight(WorldPlanNode node)
        {
            return checked(node.Type.MinimumHeight + node.Type.MinimumClearance * 2);
        }

        private static int CompareNodes(WorldPlanNode left, WorldPlanNode right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return string.CompareOrdinal(left.Id, right.Id);
        }

        private static int CompareLayouts(WorldPlanNodeLayout left, WorldPlanNodeLayout right)
        {
            return string.CompareOrdinal(left.NodeId, right.NodeId);
        }

        private static int ComparePorts(WorldPlanPortDefinition left, WorldPlanPortDefinition right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return string.CompareOrdinal(left.Id, right.Id);
        }

        private static int CompareConnections(WorldPlanConnection left, WorldPlanConnection right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            int comparison = string.CompareOrdinal(left.SourceNode.Id, right.SourceNode.Id);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.SourcePort.Id, right.SourcePort.Id);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.TargetNode.Id, right.TargetNode.Id);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.TargetPort.Id, right.TargetPort.Id);
            if (comparison != 0) return comparison;
            return string.CompareOrdinal(left.Id, right.Id);
        }

        private static int ComparePortsLayout(WorldPlanLayoutPort left, WorldPlanLayoutPort right)
        {
            int comparison = string.CompareOrdinal(left.NodeId, right.NodeId);
            if (comparison != 0) return comparison;
            return string.CompareOrdinal(left.PortId, right.PortId);
        }
    }
}
