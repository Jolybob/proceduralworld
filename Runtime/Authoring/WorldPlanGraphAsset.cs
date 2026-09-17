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

        public void EnsureIds()
        {
            HashSet<string> nodeTypeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < nodeTypes.Count; i++)
            {
                NodeTypeRecord type = nodeTypes[i];
                if (type == null)
                    continue;

                type.id = EnsureUnique(type.id, "node", nodeTypeIds);
                HashSet<string> portIds = new HashSet<string>(StringComparer.Ordinal);
                for (int p = 0; p < type.ports.Count; p++)
                {
                    PortRecord port = type.ports[p];
                    if (port == null)
                        continue;
                    port.id = EnsureUnique(port.id, "port", portIds);
                }
            }

            HashSet<string> nodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < nodes.Count; i++)
            {
                NodeRecord node = nodes[i];
                if (node == null)
                    continue;
                node.id = EnsureUnique(node.id, "node", nodeIds);
                if (string.IsNullOrWhiteSpace(node.label))
                    node.label = node.typeId;
            }

            HashSet<string> connectionIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < connections.Count; i++)
            {
                ConnectionRecord connection = connections[i];
                if (connection == null)
                    continue;
                connection.id = EnsureUnique(connection.id, "connection", connectionIds);
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

        private static string EnsureUnique(string value, string fallbackPrefix, HashSet<string> used)
        {
            string candidate = string.IsNullOrWhiteSpace(value) ? fallbackPrefix : value.Trim();
            if (used.Add(candidate))
                return candidate;

            int suffix = 2;
            string suffixed;
            do
            {
                suffixed = candidate + "_" + suffix;
                suffix++;
            }
            while (!used.Add(suffixed));

            return suffixed;
        }
    }
}
