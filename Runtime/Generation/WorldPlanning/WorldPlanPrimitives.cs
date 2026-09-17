using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public enum WorldPlanPortDirection
    {
        Input,
        Output,
        Bidirectional
    }

    public enum WorldPlanConnectionKind
    {
        Required,
        Optional,
        Derived
    }

    public sealed class WorldPlanProperty
    {
        public string Key { get; }
        public string Value { get; }

        public WorldPlanProperty(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property key must not be empty.", nameof(key));

            Key = key;
            Value = value ?? string.Empty;
        }
    }

    public sealed class WorldPlanPortDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public WorldPlanPortDirection Direction { get; }
        public string SemanticType { get; }
        public bool Required { get; }
        public bool AllowMultipleConnections { get; }

        public WorldPlanPortDefinition(
            string id,
            string displayName,
            WorldPlanPortDirection direction,
            string semanticType,
            bool required,
            bool allowMultipleConnections)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Port ID must not be empty.", nameof(id));

            Id = id;
            DisplayName = displayName ?? id;
            Direction = direction;
            SemanticType = semanticType ?? string.Empty;
            Required = required;
            AllowMultipleConnections = allowMultipleConnections;
        }
    }

    public sealed class WorldPlanNodeTypeDefinition
    {
        private readonly List<WorldPlanPortDefinition> ports;
        private readonly List<string> propertyKeys;

        public string Id { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public int MinimumWidth { get; }
        public int MinimumHeight { get; }
        public int MinimumClearance { get; }
        public IReadOnlyList<WorldPlanPortDefinition> Ports => ports;
        public IReadOnlyList<string> PropertyKeys => propertyKeys;

        public WorldPlanNodeTypeDefinition(
            string id,
            string displayName,
            string category,
            int minimumWidth,
            int minimumHeight,
            int minimumClearance,
            IEnumerable<WorldPlanPortDefinition> ports,
            IEnumerable<string> propertyKeys = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Node type ID must not be empty.", nameof(id));
            if (minimumWidth < 1)
                throw new ArgumentOutOfRangeException(nameof(minimumWidth));
            if (minimumHeight < 1)
                throw new ArgumentOutOfRangeException(nameof(minimumHeight));
            if (minimumClearance < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumClearance));

            Id = id;
            DisplayName = displayName ?? id;
            Category = category ?? string.Empty;
            MinimumWidth = minimumWidth;
            MinimumHeight = minimumHeight;
            MinimumClearance = minimumClearance;

            this.ports = new List<WorldPlanPortDefinition>();
            if (ports != null)
                this.ports.AddRange(ports);

            this.propertyKeys = new List<string>();
            if (propertyKeys != null)
            {
                foreach (string propertyKey in propertyKeys)
                {
                    if (!string.IsNullOrWhiteSpace(propertyKey) && !this.propertyKeys.Contains(propertyKey))
                        this.propertyKeys.Add(propertyKey);
                }
            }

            this.ports.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            this.propertyKeys.Sort(StringComparer.Ordinal);
        }

        public WorldPlanPortDefinition GetPort(string id)
        {
            for (int i = 0; i < ports.Count; i++)
            {
                if (ports[i].Id == id)
                    return ports[i];
            }

            return null;
        }
    }

    public sealed class WorldPlanNodeDefinition
    {
        private readonly List<WorldPlanProperty> properties;

        public string Id { get; }
        public string TypeId { get; }
        public string TemplateId { get; }
        public string Label { get; }
        public IReadOnlyList<WorldPlanProperty> Properties => properties;

        public WorldPlanNodeDefinition(
            string id,
            string typeId,
            string label,
            IEnumerable<WorldPlanProperty> properties = null)
            : this(id, typeId, label, null, properties)
        {
        }

        public WorldPlanNodeDefinition(
            string id,
            string typeId,
            string label,
            string templateId,
            IEnumerable<WorldPlanProperty> properties = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Node ID must not be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(typeId))
                throw new ArgumentException("Node type ID must not be empty.", nameof(typeId));

            Id = id;
            TypeId = typeId;
            TemplateId = templateId ?? string.Empty;
            Label = string.IsNullOrWhiteSpace(label) ? typeId : label;
            this.properties = new List<WorldPlanProperty>();

            if (properties != null)
                this.properties.AddRange(properties);
        }
    }

    public sealed class WorldPlanConnectionDefinition
    {
        public string Id { get; }
        public string SourceNodeId { get; }
        public string SourcePortId { get; }
        public string TargetNodeId { get; }
        public string TargetPortId { get; }
        public WorldPlanConnectionKind Kind { get; }

        public WorldPlanConnectionDefinition(
            string id,
            string sourceNodeId,
            string sourcePortId,
            string targetNodeId,
            string targetPortId,
            WorldPlanConnectionKind kind)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Connection ID must not be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(sourceNodeId))
                throw new ArgumentException("Source node ID must not be empty.", nameof(sourceNodeId));
            if (string.IsNullOrWhiteSpace(sourcePortId))
                throw new ArgumentException("Source port ID must not be empty.", nameof(sourcePortId));
            if (string.IsNullOrWhiteSpace(targetNodeId))
                throw new ArgumentException("Target node ID must not be empty.", nameof(targetNodeId));
            if (string.IsNullOrWhiteSpace(targetPortId))
                throw new ArgumentException("Target port ID must not be empty.", nameof(targetPortId));

            Id = id;
            SourceNodeId = sourceNodeId;
            SourcePortId = sourcePortId;
            TargetNodeId = targetNodeId;
            TargetPortId = targetPortId;
            Kind = kind;
        }
    }

    public sealed class WorldPlanSubgraphPortDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string NodeId { get; }
        public string PortId { get; }

        public WorldPlanSubgraphPortDefinition(
            string id,
            string displayName,
            string nodeId,
            string portId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Exposed port ID must not be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException("Exposed node ID must not be empty.", nameof(nodeId));
            if (string.IsNullOrWhiteSpace(portId))
                throw new ArgumentException("Exposed port target port ID must not be empty.", nameof(portId));

            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            NodeId = nodeId;
            PortId = portId;
        }
    }

    public sealed class WorldPlanSubgraphTemplateDefinition
    {
        private readonly List<WorldPlanSubgraphPortDefinition> exposedPorts;

        public string Id { get; }
        public string DisplayName { get; }
        public WorldPlanGraphDefinition Graph { get; }
        public IReadOnlyList<WorldPlanSubgraphPortDefinition> ExposedPorts => exposedPorts;

        public WorldPlanSubgraphTemplateDefinition(
            string id,
            string displayName,
            WorldPlanGraphDefinition graph,
            IEnumerable<WorldPlanSubgraphPortDefinition> exposedPorts = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Template ID must not be empty.", nameof(id));
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            Graph = graph;
            this.exposedPorts = new List<WorldPlanSubgraphPortDefinition>();

            if (exposedPorts != null)
                this.exposedPorts.AddRange(exposedPorts);

            this.exposedPorts.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        }

        public WorldPlanSubgraphPortDefinition GetExposedPort(string id)
        {
            for (int i = 0; i < exposedPorts.Count; i++)
            {
                if (string.Equals(exposedPorts[i].Id, id, StringComparison.Ordinal))
                    return exposedPorts[i];
            }

            return null;
        }
    }

    public sealed class WorldPlanGraphDefinition
    {
        private readonly List<WorldPlanNodeTypeDefinition> nodeTypes;
        private readonly List<WorldPlanNodeDefinition> nodes;
        private readonly List<WorldPlanConnectionDefinition> connections;
        private readonly List<WorldPlanSubgraphTemplateDefinition> templates;

        public IReadOnlyList<WorldPlanNodeTypeDefinition> NodeTypes => nodeTypes;
        public IReadOnlyList<WorldPlanNodeDefinition> Nodes => nodes;
        public IReadOnlyList<WorldPlanConnectionDefinition> Connections => connections;
        public IReadOnlyList<WorldPlanSubgraphTemplateDefinition> Templates => templates;

        public WorldPlanGraphDefinition(
            IEnumerable<WorldPlanNodeTypeDefinition> nodeTypes,
            IEnumerable<WorldPlanNodeDefinition> nodes,
            IEnumerable<WorldPlanConnectionDefinition> connections)
            : this(nodeTypes, nodes, connections, null)
        {
        }

        public WorldPlanGraphDefinition(
            IEnumerable<WorldPlanNodeTypeDefinition> nodeTypes,
            IEnumerable<WorldPlanNodeDefinition> nodes,
            IEnumerable<WorldPlanConnectionDefinition> connections,
            IEnumerable<WorldPlanSubgraphTemplateDefinition> templates)
        {
            this.nodeTypes = new List<WorldPlanNodeTypeDefinition>();
            this.nodes = new List<WorldPlanNodeDefinition>();
            this.connections = new List<WorldPlanConnectionDefinition>();
            this.templates = new List<WorldPlanSubgraphTemplateDefinition>();

            if (nodeTypes != null)
                this.nodeTypes.AddRange(nodeTypes);
            if (nodes != null)
                this.nodes.AddRange(nodes);
            if (connections != null)
                this.connections.AddRange(connections);
            if (templates != null)
                this.templates.AddRange(templates);
        }
    }

    public sealed class WorldPlanNode
    {
        private readonly List<WorldPlanProperty> properties;

        public int Index { get; }
        public string Id { get; }
        public string TypeId { get; }
        public string Label { get; }
        public WorldPlanNodeTypeDefinition Type { get; }
        public IReadOnlyList<WorldPlanProperty> Properties => properties;

        internal WorldPlanNode(
            int index,
            WorldPlanNodeDefinition definition,
            WorldPlanNodeTypeDefinition type)
        {
            Index = index;
            Id = definition.Id;
            TypeId = definition.TypeId;
            Label = definition.Label;
            Type = type;
            properties = new List<WorldPlanProperty>(definition.Properties);
        }
    }

    public sealed class WorldPlanConnection
    {
        public int Index { get; }
        public string Id { get; }
        public WorldPlanNode SourceNode { get; }
        public WorldPlanPortDefinition SourcePort { get; }
        public WorldPlanNode TargetNode { get; }
        public WorldPlanPortDefinition TargetPort { get; }
        public WorldPlanConnectionKind Kind { get; }

        internal WorldPlanConnection(
            int index,
            WorldPlanConnectionDefinition definition,
            WorldPlanNode sourceNode,
            WorldPlanPortDefinition sourcePort,
            WorldPlanNode targetNode,
            WorldPlanPortDefinition targetPort)
        {
            Index = index;
            Id = definition.Id;
            SourceNode = sourceNode;
            SourcePort = sourcePort;
            TargetNode = targetNode;
            TargetPort = targetPort;
            Kind = definition.Kind;
        }
    }

    public sealed class WorldPlan
    {
        private readonly List<WorldPlanNode> nodes;
        private readonly List<WorldPlanConnection> connections;

        public int Seed { get; }
        public IReadOnlyList<WorldPlanNode> Nodes => nodes;
        public IReadOnlyList<WorldPlanConnection> Connections => connections;

        internal WorldPlan(
            int seed,
            List<WorldPlanNode> nodes,
            List<WorldPlanConnection> connections)
        {
            Seed = seed;
            this.nodes = nodes;
            this.connections = connections;
        }
    }
}
