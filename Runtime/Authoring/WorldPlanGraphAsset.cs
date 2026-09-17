using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jolybob.ProceduralWorld.Authoring
{
    /// <summary>
    /// Serialized, editor-friendly world plan graph. Canvas positions are authoring metadata only;
    /// they are deliberately excluded from the deterministic runtime graph definition.
    /// A graph can also be marked as a reusable subgraph template and expose selected internal ports.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WorldPlanGraph",
        menuName = "Procedural World/World Plan Graph",
        order = 10)]
    public sealed class WorldPlanGraphAsset : ScriptableObject
    {
        [Serializable]
        public sealed class PortRecord
        {
            public string id = "port";
            public string displayName = "Port";
            public WorldPlanPortDirection direction = WorldPlanPortDirection.Input;
            public string semanticType = string.Empty;
            public bool required;
            public bool allowMultipleConnections = true;
        }

        [Serializable]
        public sealed class NodeTypeRecord
        {
            public string id = "node";
            public string displayName = "Node";
            public string category = "General";
            [Min(1)] public int minimumWidth = 1;
            [Min(1)] public int minimumHeight = 1;
            [Min(0)] public int minimumClearance;
            public List<PortRecord> ports = new List<PortRecord>();
            public List<string> propertyKeys = new List<string>();
        }

        [Serializable]
        public sealed class PropertyRecord
        {
            public string key = "Property";
            [TextArea(1, 3)] public string value = string.Empty;
        }

        [Serializable]
        public sealed class NodeRecord
        {
            public string id = string.Empty;
            public string typeId = "node";
            public string templateId = string.Empty;
            public string label = "Node";
            public Vector2 position;
            public List<PropertyRecord> properties = new List<PropertyRecord>();
        }

        [Serializable]
        public sealed class ConnectionRecord
        {
            public string id = string.Empty;
            public string sourceNodeId = string.Empty;
            public string sourcePortId = string.Empty;
            public string targetNodeId = string.Empty;
            public string targetPortId = string.Empty;
            public WorldPlanConnectionKind kind = WorldPlanConnectionKind.Required;
        }

        [Serializable]
        public sealed class ExposedPortRecord
        {
            public string id = "port";
            public string displayName = "Port";
            public string nodeId = string.Empty;
            public string portId = string.Empty;
        }

        [Header("Graph")]
        [SerializeField] private int generationSeed;
        [SerializeField] private List<NodeTypeRecord> nodeTypes = new List<NodeTypeRecord>();
        [SerializeField] private List<NodeRecord> nodes = new List<NodeRecord>();
        [SerializeField] private List<ConnectionRecord> connections = new List<ConnectionRecord>();

        [Header("Reusable Template")]
        [SerializeField] private bool reusableTemplate;
        [SerializeField] private string templateId = string.Empty;
        [SerializeField] private string templateDisplayName = string.Empty;
        [SerializeField] private List<ExposedPortRecord> exposedPorts = new List<ExposedPortRecord>();
        [SerializeField] private List<WorldPlanGraphAsset> referencedTemplates = new List<WorldPlanGraphAsset>();

        public int GenerationSeed => generationSeed;
        public bool IsReusableTemplate => reusableTemplate;
        public string TemplateId => templateId;
        public string TemplateDisplayName => string.IsNullOrWhiteSpace(templateDisplayName) ? name : templateDisplayName;
        public IReadOnlyList<NodeTypeRecord> NodeTypes => nodeTypes;
        public IReadOnlyList<NodeRecord> Nodes => nodes;
        public IReadOnlyList<ConnectionRecord> Connections => connections;
        public IReadOnlyList<ExposedPortRecord> ExposedPorts => exposedPorts;
        public IReadOnlyList<WorldPlanGraphAsset> ReferencedTemplates => referencedTemplates;

        public void SetGenerationSeed(int seed)
        {
            generationSeed = seed;
        }

        public void SetTemplateId(string id)
        {
            templateId = id ?? string.Empty;
        }

        public void AddReferencedTemplate(WorldPlanGraphAsset template)
        {
            if (template == null || referencedTemplates.Contains(template))
                return;
            referencedTemplates.Add(template);
        }

        public void RemoveReferencedTemplate(WorldPlanGraphAsset template)
        {
            if (template == null)
                return;
            referencedTemplates.Remove(template);
        }

        public WorldPlanGraphAsset FindReferencedTemplate(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            for (int i = 0; i < referencedTemplates.Count; i++)
            {
                WorldPlanGraphAsset template = referencedTemplates[i];
                if (template != null && string.Equals(template.TemplateId, id, StringComparison.Ordinal))
                    return template;
            }

            return null;
        }

        public void AddExposedPort(ExposedPortRecord port)
        {
            if (port == null)
                throw new ArgumentNullException(nameof(port));
            exposedPorts.Add(port);
        }

        public void RemoveExposedPort(string exposedPortId)
        {
            if (string.IsNullOrWhiteSpace(exposedPortId))
                return;

            for (int i = exposedPorts.Count - 1; i >= 0; i--)
            {
                if (exposedPorts[i] != null
                    && string.Equals(exposedPorts[i].id, exposedPortId, StringComparison.Ordinal))
                {
                    exposedPorts.RemoveAt(i);
                }
            }
        }

        public ExposedPortRecord FindExposedPort(string exposedPortId)
        {
            if (string.IsNullOrWhiteSpace(exposedPortId))
                return null;

            for (int i = 0; i < exposedPorts.Count; i++)
            {
                ExposedPortRecord exposed = exposedPorts[i];
                if (exposed != null && string.Equals(exposed.id, exposedPortId, StringComparison.Ordinal))
                    return exposed;
            }

            return null;
        }

        public void AddNode(NodeRecord node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            nodes.Add(node);
        }

        public void RemoveNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return;

            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if (nodes[i] != null && string.Equals(nodes[i].id, nodeId, StringComparison.Ordinal))
                    nodes.RemoveAt(i);
            }

            RemoveConnectionsForNode(nodeId);
        }

        public void AddConnection(ConnectionRecord connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            connections.Add(connection);
        }

        public void RemoveConnection(string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                return;

            for (int i = connections.Count - 1; i >= 0; i--)
            {
                if (connections[i] != null && string.Equals(connections[i].id, connectionId, StringComparison.Ordinal))
                    connections.RemoveAt(i);
            }
        }

        public void RemoveConnectionsForNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return;

            for (int i = connections.Count - 1; i >= 0; i--)
            {
                ConnectionRecord connection = connections[i];
                if (connection == null
                    || string.Equals(connection.sourceNodeId, nodeId, StringComparison.Ordinal)
                    || string.Equals(connection.targetNodeId, nodeId, StringComparison.Ordinal))
                {
                    connections.RemoveAt(i);
                }
            }
        }

        public void EnsureIds()
        {
            for (int i = 0; i < nodeTypes.Count; i++)
            {
                NodeTypeRecord type = nodeTypes[i];
                if (type == null)
                    continue;

                if (string.IsNullOrWhiteSpace(type.id))
                    type.id = "node_" + Guid.NewGuid().ToString("N");

                for (int p = 0; p < type.ports.Count; p++)
                {
                    PortRecord port = type.ports[p];
                    if (port == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(port.id))
                        port.id = "port_" + Guid.NewGuid().ToString("N");
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                NodeRecord node = nodes[i];
                if (node == null)
                    continue;

                if (string.IsNullOrWhiteSpace(node.id))
                    node.id = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(node.label))
                    node.label = string.IsNullOrWhiteSpace(node.templateId) ? node.typeId : node.templateId;
            }

            for (int i = 0; i < connections.Count; i++)
            {
                ConnectionRecord connection = connections[i];
                if (connection == null)
                    continue;

                if (string.IsNullOrWhiteSpace(connection.id))
                    connection.id = Guid.NewGuid().ToString("N");
            }

            for (int i = 0; i < exposedPorts.Count; i++)
            {
                ExposedPortRecord exposedPort = exposedPorts[i];
                if (exposedPort == null)
                    continue;

                if (string.IsNullOrWhiteSpace(exposedPort.id))
                    exposedPort.id = "port_" + Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(exposedPort.displayName))
                    exposedPort.displayName = exposedPort.id;
            }

            if (string.IsNullOrWhiteSpace(templateId))
                templateId = "template_" + Guid.NewGuid().ToString("N");
            if (reusableTemplate && string.IsNullOrWhiteSpace(templateDisplayName))
                templateDisplayName = name;
        }

        public WorldPlanGraphDefinition BuildDefinition()
        {
            return BuildDefinition(new HashSet<WorldPlanGraphAsset>());
        }

        private WorldPlanGraphDefinition BuildDefinition(HashSet<WorldPlanGraphAsset> activeAssets)
        {
            if (!activeAssets.Add(this))
                throw new InvalidOperationException("World plan template reference cycle detected at asset '" + name + "'.");

            try
            {
                EnsureIds();

                var runtimeTypes = new List<WorldPlanNodeTypeDefinition>();
                for (int i = 0; i < nodeTypes.Count; i++)
                {
                    NodeTypeRecord type = nodeTypes[i];
                    if (type == null)
                        continue;

                    var runtimePorts = new List<WorldPlanPortDefinition>();
                    for (int p = 0; p < type.ports.Count; p++)
                    {
                        PortRecord port = type.ports[p];
                        if (port == null)
                            continue;

                        runtimePorts.Add(new WorldPlanPortDefinition(
                            port.id,
                            port.displayName,
                            port.direction,
                            port.semanticType,
                            port.required,
                            port.allowMultipleConnections));
                    }

                    runtimeTypes.Add(new WorldPlanNodeTypeDefinition(
                        type.id,
                        type.displayName,
                        type.category,
                        Mathf.Max(1, type.minimumWidth),
                        Mathf.Max(1, type.minimumHeight),
                        Mathf.Max(0, type.minimumClearance),
                        runtimePorts,
                        type.propertyKeys));
                }

                var runtimeNodes = new List<WorldPlanNodeDefinition>();
                for (int i = 0; i < nodes.Count; i++)
                {
                    NodeRecord node = nodes[i];
                    if (node == null)
                        continue;

                    var properties = new List<WorldPlanProperty>();
                    for (int p = 0; p < node.properties.Count; p++)
                    {
                        PropertyRecord property = node.properties[p];
                        if (property == null)
                            continue;
                        properties.Add(new WorldPlanProperty(property.key, property.value));
                    }

                    runtimeNodes.Add(new WorldPlanNodeDefinition(
                        node.id,
                        node.typeId,
                        node.label,
                        node.templateId,
                        properties));
                }

                var runtimeConnections = new List<WorldPlanConnectionDefinition>();
                for (int i = 0; i < connections.Count; i++)
                {
                    ConnectionRecord connection = connections[i];
                    if (connection == null)
                        continue;

                    runtimeConnections.Add(new WorldPlanConnectionDefinition(
                        connection.id,
                        connection.sourceNodeId,
                        connection.sourcePortId,
                        connection.targetNodeId,
                        connection.targetPortId,
                        connection.kind));
                }

                var runtimeTemplates = new List<WorldPlanSubgraphTemplateDefinition>();
                for (int i = 0; i < referencedTemplates.Count; i++)
                {
                    WorldPlanGraphAsset templateAsset = referencedTemplates[i];
                    if (templateAsset == null)
                        continue;

                    templateAsset.EnsureIds();
                    WorldPlanGraphDefinition templateGraph = templateAsset.BuildDefinition(activeAssets);
                    var runtimeExposedPorts = new List<WorldPlanSubgraphPortDefinition>();
                    for (int p = 0; p < templateAsset.exposedPorts.Count; p++)
                    {
                        ExposedPortRecord exposedPort = templateAsset.exposedPorts[p];
                        if (exposedPort == null)
                            continue;

                        runtimeExposedPorts.Add(new WorldPlanSubgraphPortDefinition(
                            exposedPort.id,
                            exposedPort.displayName,
                            exposedPort.nodeId,
                            exposedPort.portId));
                    }

                    runtimeTemplates.Add(new WorldPlanSubgraphTemplateDefinition(
                        templateAsset.templateId,
                        templateAsset.TemplateDisplayName,
                        templateGraph,
                        runtimeExposedPorts));
                }

                return new WorldPlanGraphDefinition(
                    runtimeTypes,
                    runtimeNodes,
                    runtimeConnections,
                    runtimeTemplates);
            }
            finally
            {
                activeAssets.Remove(this);
            }
        }

        public WorldPlanValidationResult Validate()
        {
            return new WorldPlanSubgraphCompiler().Validate(BuildDefinition());
        }

        public WorldPlanCompilationResult Compile()
        {
            return new WorldPlanSubgraphCompiler().Compile(generationSeed, BuildDefinition());
        }

        public NodeTypeRecord FindNodeType(string typeId)
        {
            for (int i = 0; i < nodeTypes.Count; i++)
            {
                NodeTypeRecord type = nodeTypes[i];
                if (type != null && string.Equals(type.id, typeId, StringComparison.Ordinal))
                    return type;
            }

            return null;
        }

        public NodeRecord FindNode(string nodeId)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                NodeRecord node = nodes[i];
                if (node != null && string.Equals(node.id, nodeId, StringComparison.Ordinal))
                    return node;
            }

            return null;
        }

        public static NodeRecord CreateNode(string typeId, string label, Vector2 position)
        {
            return new NodeRecord
            {
                id = Guid.NewGuid().ToString("N"),
                typeId = typeId,
                label = string.IsNullOrWhiteSpace(label) ? typeId : label,
                position = position
            };
        }

        public static NodeRecord CreateTemplateInstance(string templateId, string label, Vector2 position)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ArgumentException("Template ID must not be empty.", nameof(templateId));

            return new NodeRecord
            {
                id = Guid.NewGuid().ToString("N"),
                typeId = "__subgraph_instance__",
                templateId = templateId,
                label = string.IsNullOrWhiteSpace(label) ? templateId : label,
                position = position
            };
        }
    }
}
