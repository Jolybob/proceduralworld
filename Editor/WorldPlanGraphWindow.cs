using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Jolybob.ProceduralWorld;
using Jolybob.ProceduralWorld.Authoring;
using Direction = UnityEditor.Experimental.GraphView.Direction;
using Orientation = UnityEditor.Experimental.GraphView.Orientation;
using Port = UnityEditor.Experimental.GraphView.Port;

namespace Jolybob.ProceduralWorld.Editor
{
    public sealed class WorldPlanGraphWindow : EditorWindow
    {
        private WorldPlanGraphAsset asset;
        private WorldPlanGraphView graphView;
        private Label statusLabel;

        [MenuItem("Window/Procedural World/World Plan Graph")]
        private static void OpenEmpty()
        {
            GetWindow<WorldPlanGraphWindow>("World Plan Graph");
        }

        public static void Open(WorldPlanGraphAsset graphAsset)
        {
            if (graphAsset == null)
                return;

            WorldPlanGraphWindow window = GetWindow<WorldPlanGraphWindow>("World Plan Graph");
            window.asset = graphAsset;
            window.Rebuild();
            window.Show();
        }

        private void CreateGUI()
        {
            BuildToolbar();

            graphView = new WorldPlanGraphView();
            graphView.style.flexGrow = 1f;
            rootVisualElement.Add(graphView);
            Rebuild();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is WorldPlanGraphAsset selected)
            {
                asset = selected;
                Rebuild();
            }
        }

        private void BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.minHeight = 24f;
            toolbar.style.paddingLeft = 4f;
            toolbar.style.paddingRight = 4f;

            toolbar.Add(new Button(SelectGraphAsset) { text = "Select Graph" });
            toolbar.Add(new Button(ValidateGraph) { text = "Validate" });
            toolbar.Add(new Button(() => graphView?.FrameAll()) { text = "Frame" });

            statusLabel = new Label("No graph selected.");
            statusLabel.style.flexGrow = 1f;
            statusLabel.style.marginLeft = 8f;
            toolbar.Add(statusLabel);
            rootVisualElement.Add(toolbar);
        }

        private void Rebuild()
        {
            if (graphView == null)
                return;

            graphView.SetAsset(asset);
            if (asset == null)
            {
                statusLabel.text = "No graph selected. Define node types in the asset inspector, then right-click the canvas.";
                return;
            }

            WorldPlanValidationResult validation = asset.Validate();
            statusLabel.text = validation.IsValid
                ? "Valid graph: " + asset.Nodes.Count + " nodes, " + asset.Connections.Count + " connections."
                : BuildIssueSummary(validation);
        }

        private void SelectGraphAsset()
        {
            if (asset == null)
                return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void ValidateGraph()
        {
            if (asset == null)
                return;

            WorldPlanValidationResult validation = asset.Validate();
            statusLabel.text = validation.IsValid
                ? "Valid graph: " + asset.Nodes.Count + " nodes, " + asset.Connections.Count + " connections."
                : BuildIssueSummary(validation);
        }

        private static string BuildIssueSummary(WorldPlanValidationResult validation)
        {
            int errors = 0;
            int warnings = 0;
            for (int i = 0; i < validation.Issues.Count; i++)
            {
                if (validation.Issues[i].Severity == WorldPlanValidationSeverity.Error)
                    errors++;
                else
                    warnings++;
            }

            return "Graph validation: " + errors + " error(s), " + warnings + " warning(s).";
        }

        private sealed class WorldPlanGraphView : GraphView
        {
            private sealed class PortBinding
            {
                public readonly string NodeId;
                public readonly string PortId;

                public PortBinding(string nodeId, string portId)
                {
                    NodeId = nodeId;
                    PortId = portId;
                }
            }

            private sealed class WorldPlanGraphNodeView : Node
            {
                public string NodeId { get; }

                public WorldPlanGraphNodeView(WorldPlanGraphAsset.NodeRecord node)
                {
                    NodeId = node.id;
                    title = node.label;
                    viewDataKey = node.id;
                    capabilities |= Capabilities.Movable | Capabilities.Selectable | Capabilities.Deletable;
                }
            }

            private WorldPlanGraphAsset asset;
            private readonly Dictionary<string, WorldPlanGraphNodeView> nodesById =
                new Dictionary<string, WorldPlanGraphNodeView>(StringComparer.Ordinal);
            private bool rebuilding;

            public WorldPlanGraphView()
            {
                Insert(0, new GridBackground());
                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new SelectionDragger());
                this.AddManipulator(new RectangleSelector());
                graphViewChanged = OnGraphViewChanged;
                SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
            }

            public void SetAsset(WorldPlanGraphAsset graphAsset)
            {
                asset = graphAsset;
                Rebuild();
            }

            public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
            {
                var compatible = new List<Port>();
                if (!(startPort.userData is PortBinding startBinding) || asset == null)
                    return compatible;

                WorldPlanGraphAsset.PortRecord startPortRecord = FindPortRecord(startBinding);
                if (startPortRecord == null)
                    return compatible;

                foreach (Port port in ports)
                {
                    if (port == startPort || port.node == startPort.node)
                        continue;
                    if (!(port.userData is PortBinding candidateBinding))
                        continue;

                    WorldPlanGraphAsset.PortRecord candidate = FindPortRecord(candidateBinding);
                    if (candidate == null)
                        continue;
                    if (!DirectionCompatible(startPortRecord.direction, candidate.direction))
                        continue;
                    if (!SemanticTypesCompatible(startPortRecord.semanticType, candidate.semanticType))
                        continue;

                    compatible.Add(port);
                }

                return compatible;
            }

            private void Rebuild()
            {
                rebuilding = true;
                DeleteElements(graphElements.ToList());
                nodesById.Clear();

                if (asset != null)
                {
                    asset.EnsureIds();
                    for (int i = 0; i < asset.Nodes.Count; i++)
                    {
                        WorldPlanGraphAsset.NodeRecord node = asset.Nodes[i];
                        if (node == null)
                            continue;

                        WorldPlanGraphNodeView view = CreateNodeView(node);
                        if (view == null)
                            continue;

                        nodesById.Add(node.id, view);
                        AddElement(view);
                    }

                    for (int i = 0; i < asset.Connections.Count; i++)
                    {
                        WorldPlanGraphAsset.ConnectionRecord connection = asset.Connections[i];
                        if (connection != null)
                            CreateEdgeView(connection);
                    }
                }

                rebuilding = false;
            }

            private WorldPlanGraphNodeView CreateNodeView(WorldPlanGraphAsset.NodeRecord node)
            {
                if (!string.IsNullOrWhiteSpace(node.templateId))
                {
                    WorldPlanGraphAsset template = asset.FindReferencedTemplate(node.templateId);
                    if (template == null)
                        return null;
                    return CreateTemplateNodeView(node, template);
                }

                WorldPlanGraphAsset.NodeTypeRecord type = asset.FindNodeType(node.typeId);
                if (type == null)
                    return null;

                return CreateRegularNodeView(node, type);
            }

            private WorldPlanGraphNodeView CreateRegularNodeView(
                WorldPlanGraphAsset.NodeRecord node,
                WorldPlanGraphAsset.NodeTypeRecord type)
            {
                var view = new WorldPlanGraphNodeView(node);
                for (int i = 0; i < type.ports.Count; i++)
                {
                    WorldPlanGraphAsset.PortRecord portRecord = type.ports[i];
                    if (portRecord == null)
                        continue;

                    if (portRecord.direction == WorldPlanPortDirection.Input
                        || portRecord.direction == WorldPlanPortDirection.Bidirectional)
                    {
                        view.inputContainer.Add(CreatePort(node.id, portRecord, Direction.Input, "In"));
                    }

                    if (portRecord.direction == WorldPlanPortDirection.Output
                        || portRecord.direction == WorldPlanPortDirection.Bidirectional)
                    {
                        view.outputContainer.Add(CreatePort(node.id, portRecord, Direction.Output, "Out"));
                    }
                }

                view.RefreshExpandedState();
                view.RefreshPorts();
                view.SetPosition(new Rect(node.position, new Vector2(220f, 120f)));
                return view;
            }

            private WorldPlanGraphNodeView CreateTemplateNodeView(
                WorldPlanGraphAsset.NodeRecord node,
                WorldPlanGraphAsset template)
            {
                var view = new WorldPlanGraphNodeView(node);
                template.EnsureIds();

                for (int i = 0; i < template.ExposedPorts.Count; i++)
                {
                    WorldPlanGraphAsset.ExposedPortRecord exposed = template.ExposedPorts[i];
                    if (exposed == null)
                        continue;

                    WorldPlanGraphAsset.PortRecord portRecord = ResolveTemplatePort(template, exposed);
                    if (portRecord == null)
                        continue;

                    string displayName = string.IsNullOrWhiteSpace(exposed.displayName)
                        ? exposed.id
                        : exposed.displayName;

                    if (portRecord.direction == WorldPlanPortDirection.Input
                        || portRecord.direction == WorldPlanPortDirection.Bidirectional)
                    {
                        view.inputContainer.Add(CreatePort(node.id, exposed.id, portRecord, Direction.Input, displayName + " In"));
                    }

                    if (portRecord.direction == WorldPlanPortDirection.Output
                        || portRecord.direction == WorldPlanPortDirection.Bidirectional)
                    {
                        view.outputContainer.Add(CreatePort(node.id, exposed.id, portRecord, Direction.Output, displayName + " Out"));
                    }
                }

                view.RefreshExpandedState();
                view.RefreshPorts();
                view.SetPosition(new Rect(node.position, new Vector2(240f, 140f)));
                return view;
            }

            private Port CreatePort(
                string nodeId,
                WorldPlanGraphAsset.PortRecord portRecord,
                Direction direction,
                string suffix)
            {
                return CreatePort(nodeId, portRecord.id, portRecord, direction, portRecord.displayName + " " + suffix);
            }

            private Port CreatePort(
                string nodeId,
                string bindingPortId,
                WorldPlanGraphAsset.PortRecord portRecord,
                Direction direction,
                string displayName)
            {
                Port port = Port.Create<Edge>(
                    Orientation.Horizontal,
                    direction,
                    portRecord.allowMultipleConnections
                        ? Port.Capacity.Multi
                        : Port.Capacity.Single,
                    typeof(float));
                port.portName = displayName;
                port.userData = new PortBinding(nodeId, bindingPortId);
                port.tooltip = string.IsNullOrWhiteSpace(portRecord.semanticType)
                    ? displayName
                    : displayName + " [" + portRecord.semanticType + "]";
                return port;
            }

            private void CreateEdgeView(WorldPlanGraphAsset.ConnectionRecord connection)
            {
                if (!nodesById.TryGetValue(connection.sourceNodeId, out WorldPlanGraphNodeView sourceNode))
                    return;
                if (!nodesById.TryGetValue(connection.targetNodeId, out WorldPlanGraphNodeView targetNode))
                    return;

                Port sourcePort = FindPort(sourceNode, connection.sourcePortId, Direction.Output);
                Port targetPort = FindPort(targetNode, connection.targetPortId, Direction.Input);
                if (sourcePort == null || targetPort == null)
                    return;

                Edge edge = sourcePort.ConnectTo(targetPort);
                edge.userData = connection.id;
                AddElement(edge);
            }

            private GraphViewChange OnGraphViewChanged(GraphViewChange change)
            {
                if (rebuilding || asset == null)
                    return change;

                bool changed = false;

                if (change.movedElements != null)
                {
                    for (int i = 0; i < change.movedElements.Count; i++)
                    {
                        if (!(change.movedElements[i] is WorldPlanGraphNodeView nodeView))
                            continue;

                        WorldPlanGraphAsset.NodeRecord record = asset.FindNode(nodeView.NodeId);
                        if (record == null || record.position == nodeView.GetPosition().position)
                            continue;

                        Undo.RecordObject(asset, "Move World Plan Node");
                        record.position = nodeView.GetPosition().position;
                        changed = true;
                    }
                }

                if (change.edgesToCreate != null)
                {
                    for (int i = 0; i < change.edgesToCreate.Count; i++)
                    {
                        Edge edge = change.edgesToCreate[i];
                        if (!(edge.output?.userData is PortBinding sourceBinding)
                            || !(edge.input?.userData is PortBinding targetBinding))
                            continue;

                        if (FindConnection(
                                sourceBinding.NodeId,
                                sourceBinding.PortId,
                                targetBinding.NodeId,
                                targetBinding.PortId) != null)
                            continue;

                        Undo.RecordObject(asset, "Create World Plan Connection");
                        var record = new WorldPlanGraphAsset.ConnectionRecord
                        {
                            id = Guid.NewGuid().ToString("N"),
                            sourceNodeId = sourceBinding.NodeId,
                            sourcePortId = sourceBinding.PortId,
                            targetNodeId = targetBinding.NodeId,
                            targetPortId = targetBinding.PortId,
                            kind = WorldPlanConnectionKind.Required
                        };
                        asset.AddConnection(record);
                        edge.userData = record.id;
                        changed = true;
                    }
                }

                if (change.elementsToRemove != null)
                {
                    for (int i = 0; i < change.elementsToRemove.Count; i++)
                    {
                        if (change.elementsToRemove[i] is WorldPlanGraphNodeView nodeView)
                        {
                            Undo.RecordObject(asset, "Delete World Plan Node");
                            asset.RemoveNode(nodeView.NodeId);
                            changed = true;
                        }
                        else if (change.elementsToRemove[i] is Edge edge
                            && edge.userData is string connectionId)
                        {
                            Undo.RecordObject(asset, "Delete World Plan Connection");
                            asset.RemoveConnection(connectionId);
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssets();
                }

                return change;
            }

            private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
            {
                if (asset == null)
                    return;

                Vector2 localPosition = contentViewContainer.WorldToLocal(evt.localMousePosition);
                evt.menu.AppendSeparator();

                for (int i = 0; i < asset.NodeTypes.Count; i++)
                {
                    WorldPlanGraphAsset.NodeTypeRecord type = asset.NodeTypes[i];
                    if (type == null)
                        continue;

                    string typeId = type.id;
                    string displayName = string.IsNullOrWhiteSpace(type.displayName)
                        ? typeId
                        : type.displayName;
                    evt.menu.AppendAction(
                        "Add Node/" + displayName,
                        _ => AddNode(typeId, localPosition));
                }

                for (int i = 0; i < asset.ReferencedTemplates.Count; i++)
                {
                    WorldPlanGraphAsset template = asset.ReferencedTemplates[i];
                    if (template == null)
                        continue;

                    template.EnsureIds();
                    string templateId = template.TemplateId;
                    string displayName = template.TemplateDisplayName;
                    evt.menu.AppendAction(
                        "Add Subgraph/" + displayName,
                        _ => AddTemplateInstance(templateId, displayName, localPosition));
                }
            }

            private void AddNode(string typeId, Vector2 position)
            {
                if (asset == null || asset.FindNodeType(typeId) == null)
                    return;

                Undo.RecordObject(asset, "Add World Plan Node");
                asset.AddNode(WorldPlanGraphAsset.CreateNode(typeId, typeId, position));
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Rebuild();
            }

            private void AddTemplateInstance(string templateId, string label, Vector2 position)
            {
                if (asset == null || asset.FindReferencedTemplate(templateId) == null)
                    return;

                Undo.RecordObject(asset, "Add World Plan Subgraph Instance");
                asset.AddNode(WorldPlanGraphAsset.CreateTemplateInstance(templateId, label, position));
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Rebuild();
            }

            private WorldPlanGraphAsset.PortRecord FindPortRecord(PortBinding binding)
            {
                WorldPlanGraphAsset.NodeRecord node = asset.FindNode(binding.NodeId);
                if (node == null)
                    return null;

                if (!string.IsNullOrWhiteSpace(node.templateId))
                {
                    WorldPlanGraphAsset template = asset.FindReferencedTemplate(node.templateId);
                    if (template == null)
                        return null;
                    WorldPlanGraphAsset.ExposedPortRecord exposed = template.FindExposedPort(binding.PortId);
                    return exposed == null ? null : ResolveTemplatePort(template, exposed);
                }

                WorldPlanGraphAsset.NodeTypeRecord type = asset.FindNodeType(node.typeId);
                if (type == null)
                    return null;

                for (int i = 0; i < type.ports.Count; i++)
                {
                    WorldPlanGraphAsset.PortRecord port = type.ports[i];
                    if (port != null && string.Equals(port.id, binding.PortId, StringComparison.Ordinal))
                        return port;
                }

                return null;
            }

            private static WorldPlanGraphAsset.PortRecord ResolveTemplatePort(
                WorldPlanGraphAsset template,
                WorldPlanGraphAsset.ExposedPortRecord exposed)
            {
                if (template == null || exposed == null)
                    return null;

                WorldPlanGraphAsset.NodeRecord targetNode = template.FindNode(exposed.nodeId);
                if (targetNode == null || !string.IsNullOrWhiteSpace(targetNode.templateId))
                    return null;

                WorldPlanGraphAsset.NodeTypeRecord type = template.FindNodeType(targetNode.typeId);
                if (type == null)
                    return null;

                for (int i = 0; i < type.ports.Count; i++)
                {
                    WorldPlanGraphAsset.PortRecord port = type.ports[i];
                    if (port != null && string.Equals(port.id, exposed.portId, StringComparison.Ordinal))
                        return port;
                }

                return null;
            }

            private Port FindPort(WorldPlanGraphNodeView node, string portId, Direction direction)
            {
                VisualElement container = direction == Direction.Input
                    ? node.inputContainer
                    : node.outputContainer;

                for (int i = 0; i < container.childCount; i++)
                {
                    if (!(container[i] is Port port))
                        continue;
                    if (!(port.userData is PortBinding binding))
                        continue;
                    if (string.Equals(binding.PortId, portId, StringComparison.Ordinal))
                        return port;
                }

                return null;
            }

            private WorldPlanGraphAsset.ConnectionRecord FindConnection(
                string sourceNodeId,
                string sourcePortId,
                string targetNodeId,
                string targetPortId)
            {
                for (int i = 0; i < asset.Connections.Count; i++)
                {
                    WorldPlanGraphAsset.ConnectionRecord connection = asset.Connections[i];
                    if (connection == null)
                        continue;

                    if (string.Equals(connection.sourceNodeId, sourceNodeId, StringComparison.Ordinal)
                        && string.Equals(connection.sourcePortId, sourcePortId, StringComparison.Ordinal)
                        && string.Equals(connection.targetNodeId, targetNodeId, StringComparison.Ordinal)
                        && string.Equals(connection.targetPortId, targetPortId, StringComparison.Ordinal))
                    {
                        return connection;
                    }
                }

                return null;
            }

            private static bool DirectionCompatible(
                WorldPlanPortDirection left,
                WorldPlanPortDirection right)
            {
                bool leftOutput = left == WorldPlanPortDirection.Output
                    || left == WorldPlanPortDirection.Bidirectional;
                bool leftInput = left == WorldPlanPortDirection.Input
                    || left == WorldPlanPortDirection.Bidirectional;
                bool rightOutput = right == WorldPlanPortDirection.Output
                    || right == WorldPlanPortDirection.Bidirectional;
                bool rightInput = right == WorldPlanPortDirection.Input
                    || right == WorldPlanPortDirection.Bidirectional;

                return (leftOutput && rightInput) || (leftInput && rightOutput);
            }

            private static bool SemanticTypesCompatible(string left, string right)
            {
                return string.IsNullOrEmpty(left)
                    || string.IsNullOrEmpty(right)
                    || string.Equals(left, right, StringComparison.Ordinal);
            }
        }
    }
}