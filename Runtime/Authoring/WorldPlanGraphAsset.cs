using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jolybob.ProceduralWorld.Authoring
{
    /// <summary>
    /// Serialized, editor-friendly world plan graph. Canvas positions are authoring metadata only;
    /// they are deliberately excluded from the deterministic runtime graph definition.
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

        [Header("Graph")]
        [SerializeField] private int generationSeed;
        [SerializeField] private List<NodeTypeRecord> nodeTypes = new List<NodeTypeRecord>();
        [SerializeField] private List<NodeRecord> nodes = new List<NodeRecord>();
        [SerializeField] private List<ConnectionRecord> connections = new List<ConnectionRecord>();

        public int GenerationSeed => generationSeed;
        public IReadOnlyList<NodeTypeRecord> NodeTypes => nodeTypes;
        public IReadOnlyList<NodeRecord> Nodes => nodes;
        public IReadOnlyList<ConnectionRecord> Connections => connections;

        public void SetGenerationSeed(int seed)
        {
            generationSeed = seed;
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
                    node.label = node.typeId;
            }

            for (int i = 0; i < connections.Count; i++)
            {
                ConnectionRecord connection = connections[i];
                if (connection == null)
                    continue;

                if (string.IsNullOrWhiteSpace(connection.id))
                    connection.id = Guid.NewGuid().ToString("N");
            }
        }

        public WorldPlanGraphDefinition BuildDefinition()
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

            return new WorldPlanGraphDefinition(runtimeTypes, runtimeNodes, runtimeConnections);
        }

        public WorldPlanValidationResult Validate()
        {
            return new WorldPlanCompiler().Validate(BuildDefinition());
        }

        public WorldPlanCompilationResult Compile()
        {
            return new WorldPlanCompiler().Compile(generationSeed, BuildDefinition());
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
    }
}
