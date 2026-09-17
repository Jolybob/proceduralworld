using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public enum WorldPlanValidationSeverity
    {
        Error,
        Warning
    }

    public sealed class WorldPlanValidationIssue
    {
        public WorldPlanValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public string NodeId { get; }
        public string ConnectionId { get; }

        public WorldPlanValidationIssue(
            WorldPlanValidationSeverity severity,
            string code,
            string message,
            string nodeId = null,
            string connectionId = null)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            NodeId = nodeId;
            ConnectionId = connectionId;
        }
    }

    public sealed class WorldPlanValidationResult
    {
        private readonly List<WorldPlanValidationIssue> issues;

        public IReadOnlyList<WorldPlanValidationIssue> Issues => issues;
        public bool IsValid
        {
            get
            {
                for (int i = 0; i < issues.Count; i++)
                {
                    if (issues[i].Severity == WorldPlanValidationSeverity.Error)
                        return false;
                }

                return true;
            }
        }

        internal WorldPlanValidationResult(List<WorldPlanValidationIssue> issues)
        {
            this.issues = issues;
        }
    }

    public sealed class WorldPlanCompilationResult
    {
        public WorldPlan Plan { get; }
        public WorldPlanValidationResult Validation { get; }
        public bool Succeeded => Plan != null && Validation != null && Validation.IsValid;

        internal WorldPlanCompilationResult(WorldPlan plan, WorldPlanValidationResult validation)
        {
            Plan = plan;
            Validation = validation;
        }
    }

    public sealed class WorldPlanCompiler
    {
        public WorldPlanCompilationResult Compile(int seed, WorldPlanGraphDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            WorldPlanValidationResult validation = Validate(definition);
            if (!validation.IsValid)
                return new WorldPlanCompilationResult(null, validation);

            var orderedTypes = new List<WorldPlanNodeTypeDefinition>(definition.NodeTypes);
            orderedTypes.Sort(CompareTypes);

            var orderedNodes = new List<WorldPlanNodeDefinition>(definition.Nodes);
            orderedNodes.Sort(CompareNodes);

            var orderedConnections = new List<WorldPlanConnectionDefinition>(definition.Connections);
            orderedConnections.Sort(CompareConnections);

            var typeById = new Dictionary<string, WorldPlanNodeTypeDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < orderedTypes.Count; i++)
                typeById.Add(orderedTypes[i].Id, orderedTypes[i]);

            var runtimeNodes = new List<WorldPlanNode>(orderedNodes.Count);
            var nodeById = new Dictionary<string, WorldPlanNode>(StringComparer.Ordinal);
            for (int i = 0; i < orderedNodes.Count; i++)
            {
                WorldPlanNodeDefinition definitionNode = orderedNodes[i];
                WorldPlanNode runtimeNode = new WorldPlanNode(
                    i,
                    definitionNode,
                    typeById[definitionNode.TypeId]);
                runtimeNodes.Add(runtimeNode);
                nodeById.Add(runtimeNode.Id, runtimeNode);
            }

            var runtimeConnections = new List<WorldPlanConnection>(orderedConnections.Count);
            for (int i = 0; i < orderedConnections.Count; i++)
            {
                WorldPlanConnectionDefinition connection = orderedConnections[i];
                WorldPlanNode sourceNode = nodeById[connection.SourceNodeId];
                WorldPlanNode targetNode = nodeById[connection.TargetNodeId];
                runtimeConnections.Add(new WorldPlanConnection(
                    i,
                    connection,
                    sourceNode,
                    sourceNode.Type.GetPort(connection.SourcePortId),
                    targetNode,
                    targetNode.Type.GetPort(connection.TargetPortId)));
            }

            return new WorldPlanCompilationResult(
                new WorldPlan(seed, runtimeNodes, runtimeConnections),
                validation);
        }

        public WorldPlanValidationResult Validate(WorldPlanGraphDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var issues = new List<WorldPlanValidationIssue>();
            var typeById = new Dictionary<string, WorldPlanNodeTypeDefinition>(StringComparer.Ordinal);

            for (int i = 0; i < definition.NodeTypes.Count; i++)
            {
                WorldPlanNodeTypeDefinition type = definition.NodeTypes[i];
                if (type == null)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "NullNodeType",
                        "Node type definition is null."));
                    continue;
                }

                if (typeById.ContainsKey(type.Id))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "DuplicateNodeType",
                        "Node type ID is duplicated: " + type.Id));
                    continue;
                }

                typeById.Add(type.Id, type);

                var portIds = new HashSet<string>(StringComparer.Ordinal);
                for (int p = 0; p < type.Ports.Count; p++)
                {
                    WorldPlanPortDefinition port = type.Ports[p];
                    if (port == null)
                    {
                        issues.Add(new WorldPlanValidationIssue(
                            WorldPlanValidationSeverity.Error,
                            "NullPort",
                            "Node type contains a null port.",
                            null,
                            null));
                        continue;
                    }

                    if (!portIds.Add(port.Id))
                    {
                        issues.Add(new WorldPlanValidationIssue(
                            WorldPlanValidationSeverity.Error,
                            "DuplicatePort",
                            "Port ID is duplicated on node type '" + type.Id + "': " + port.Id));
                    }
                }
            }

            var nodeById = new Dictionary<string, WorldPlanNodeDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < definition.Nodes.Count; i++)
            {
                WorldPlanNodeDefinition node = definition.Nodes[i];
                if (node == null)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "NullNode",
                        "Node definition is null."));
                    continue;
                }

                if (!nodeById.TryAdd(node.Id, node))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "DuplicateNode",
                        "Node ID is duplicated: " + node.Id,
                        node.Id));
                    continue;
                }

                if (!typeById.TryGetValue(node.TypeId, out WorldPlanNodeTypeDefinition nodeType))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "UnknownNodeType",
                        "Node '" + node.Id + "' references missing node type '" + node.TypeId + "'.",
                        node.Id));
                    continue;
                }

                var propertyKeys = new HashSet<string>(StringComparer.Ordinal);
                for (int p = 0; p < node.Properties.Count; p++)
                {
                    WorldPlanProperty property = node.Properties[p];
                    if (property == null)
                    {
                        issues.Add(new WorldPlanValidationIssue(
                            WorldPlanValidationSeverity.Error,
                            "NullProperty",
                            "Node contains a null property.",
                            node.Id));
                        continue;
                    }

                    if (!propertyKeys.Add(property.Key))
                    {
                        issues.Add(new WorldPlanValidationIssue(
                            WorldPlanValidationSeverity.Error,
                            "DuplicateProperty",
                            "Property key is duplicated: " + property.Key,
                            node.Id));
                    }

                    if (nodeType.PropertyKeys.Count > 0 && !ContainsOrdinal(nodeType.PropertyKeys, property.Key))
                    {
                        issues.Add(new WorldPlanValidationIssue(
                            WorldPlanValidationSeverity.Warning,
                            "UnknownProperty",
                            "Property '" + property.Key + "' is not declared by node type '" + node.TypeId + "'.",
                            node.Id));
                    }
                }
            }

            var connectionsByPort = new Dictionary<string, int>(StringComparer.Ordinal);
            var connectionIds = new HashSet<string>(StringComparer.Ordinal);
            var connectionKeys = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < definition.Connections.Count; i++)
            {
                WorldPlanConnectionDefinition connection = definition.Connections[i];
                if (connection == null)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "NullConnection",
                        "Connection definition is null."));
                    continue;
                }

                if (!connectionIds.Add(connection.Id))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "DuplicateConnection",
                        "Connection ID is duplicated: " + connection.Id,
                        null,
                        connection.Id));
                }

                if (!nodeById.TryGetValue(connection.SourceNodeId, out WorldPlanNodeDefinition sourceNode))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "MissingSourceNode",
                        "Connection source node does not exist: " + connection.SourceNodeId,
                        null,
                        connection.Id));
                    continue;
                }

                if (!nodeById.TryGetValue(connection.TargetNodeId, out WorldPlanNodeDefinition targetNode))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "MissingTargetNode",
                        "Connection target node does not exist: " + connection.TargetNodeId,
                        null,
                        connection.Id));
                    continue;
                }

                WorldPlanNodeTypeDefinition sourceType = typeById[sourceNode.TypeId];
                WorldPlanNodeTypeDefinition targetType = typeById[targetNode.TypeId];
                WorldPlanPortDefinition sourcePort = sourceType.GetPort(connection.SourcePortId);
                WorldPlanPortDefinition targetPort = targetType.GetPort(connection.TargetPortId);

                if (sourcePort == null)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "MissingSourcePort",
                        "Source port does not exist: " + connection.SourcePortId,
                        sourceNode.Id,
                        connection.Id));
                    continue;
                }

                if (targetPort == null)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "MissingTargetPort",
                        "Target port does not exist: " + connection.TargetPortId,
                        targetNode.Id,
                        connection.Id));
                    continue;
                }

                if (!IsSourceDirectionCompatible(sourcePort.Direction) || !IsTargetDirectionCompatible(targetPort.Direction))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "InvalidPortDirection",
                        "Connection must run from an output/bidirectional port to an input/bidirectional port.",
                        sourceNode.Id,
                        connection.Id));
                }

                if (!string.IsNullOrEmpty(sourcePort.SemanticType)
                    && !string.IsNullOrEmpty(targetPort.SemanticType)
                    && sourcePort.SemanticType != targetPort.SemanticType)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "IncompatiblePortTypes",
                        "Port semantic types do not match: '" + sourcePort.SemanticType + "' -> '" + targetPort.SemanticType + "'.",
                        sourceNode.Id,
                        connection.Id));
                }

                string sourceKey = sourceNode.Id + "::" + sourcePort.Id;
                string targetKey = targetNode.Id + "::" + targetPort.Id;
                string connectionKey = sourceKey + "->" + targetKey;
                if (!connectionKeys.Add(connectionKey))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "DuplicateConnectionEndpoint",
                        "The same source and target ports are connected more than once.",
                        sourceNode.Id,
                        connection.Id));
                }

                Increment(connectionsByPort, sourceKey);
                Increment(connectionsByPort, targetKey);
            }

            for (int i = 0; i < definition.NodeTypes.Count; i++)
            {
                WorldPlanNodeTypeDefinition type = definition.NodeTypes[i];
                if (type == null)
                    continue;

                for (int n = 0; n < definition.Nodes.Count; n++)
                {
                    WorldPlanNodeDefinition node = definition.Nodes[n];
                    if (node == null || node.TypeId != type.Id)
                        continue;

                    for (int p = 0; p < type.Ports.Count; p++)
                    {
                        WorldPlanPortDefinition port = type.Ports[p];
                        if (port == null)
                            continue;

                        string portKey = node.Id + "::" + port.Id;
                        connectionsByPort.TryGetValue(portKey, out int connectionCount);

                        if (port.Required && connectionCount == 0)
                        {
                            issues.Add(new WorldPlanValidationIssue(
                                WorldPlanValidationSeverity.Error,
                                "RequiredPortUnconnected",
                                "Required port '" + port.Id + "' has no connection.",
                                node.Id));
                        }

                        if (!port.AllowMultipleConnections && connectionCount > 1)
                        {
                            issues.Add(new WorldPlanValidationIssue(
                                WorldPlanValidationSeverity.Error,
                                "PortOverConnected",
                                "Port '" + port.Id + "' does not allow multiple connections.",
                                node.Id));
                        }
                    }
                }
            }

            return new WorldPlanValidationResult(issues);
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool IsSourceDirectionCompatible(WorldPlanPortDirection direction)
        {
            return direction == WorldPlanPortDirection.Output
                || direction == WorldPlanPortDirection.Bidirectional;
        }

        private static bool IsTargetDirectionCompatible(WorldPlanPortDirection direction)
        {
            return direction == WorldPlanPortDirection.Input
                || direction == WorldPlanPortDirection.Bidirectional;
        }

        private static void Increment(Dictionary<string, int> counts, string key)
        {
            counts.TryGetValue(key, out int count);
            counts[key] = count + 1;
        }

        private static int CompareTypes(WorldPlanNodeTypeDefinition left, WorldPlanNodeTypeDefinition right)
        {
            return string.CompareOrdinal(left.Id, right.Id);
        }

        private static int CompareNodes(WorldPlanNodeDefinition left, WorldPlanNodeDefinition right)
        {
            return string.CompareOrdinal(left.Id, right.Id);
        }

        private static int CompareConnections(
            WorldPlanConnectionDefinition left,
            WorldPlanConnectionDefinition right)
        {
            int comparison = string.CompareOrdinal(left.SourceNodeId, right.SourceNodeId);
            if (comparison != 0)
                return comparison;

            comparison = string.CompareOrdinal(left.SourcePortId, right.SourcePortId);
            if (comparison != 0)
                return comparison;

            comparison = string.CompareOrdinal(left.TargetNodeId, right.TargetNodeId);
            if (comparison != 0)
                return comparison;

            comparison = string.CompareOrdinal(left.TargetPortId, right.TargetPortId);
            if (comparison != 0)
                return comparison;

            comparison = left.Kind.CompareTo(right.Kind);
            if (comparison != 0)
                return comparison;

            return string.CompareOrdinal(left.Id, right.Id);
        }
    }
}
