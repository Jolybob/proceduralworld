using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
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
            graphView.StretchToParentSize();
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
            Toolbar toolbar = new Toolbar();

            Button selectButton = new Button(() => SelectGraphAsset())
            {
                text = "Select Graph"
            };
            toolbar.Add(selectButton);

            ToolbarMenu addMenu = new ToolbarMenu
            {
                text = "Add Node"
            };
            toolbar.Add(addMenu);

            Button validateButton = new Button(() => ValidateGraph())
            {
                text = "Validate"
            };
            toolbar.Add(validateButton);

            Button frameButton = new Button(() => graphView?.FrameAll())
            {
                text = "Frame"
            };
            toolbar.Add(frameButton);

            statusLabel = new Label("No graph selected.");
            toolbar.Add(statusLabel);

            rootVisualElement.Add(toolbar);

            RegisterAddMenuItems(addMenu);
        }

        private void RegisterAddMenuItems(ToolbarMenu menu)
        {
            menu.menu.MenuItems().Clear();
        }

        private void Rebuild()
        {
            if (graphView == null)
                return;

            graphView.SetAsset(asset);
            RefreshAddMenu();

            if (asset == null)
            {
                statusLabel.text = "No graph selected.";
                return;
            }

            WorldPlanValidationResult validation = asset.Validate();
            statusLabel.text = validation.IsValid
                ? "Valid graph: " + asset.Nodes.Count + " nodes, " + asset.Connections.Count + " connections."
                : "Graph has " + validation.Issues.Count + " validation issue(s).";
        }

        private void RefreshAddMenu()
        {
            VisualElement toolbar = rootVisualElement.childCount > 0 ? rootVisualElement[0] : null;
            if (toolbar == null)
                return;

            ToolbarMenu addMenu = null;
            for (int i = 0; i < toolbar.childCount; i++)
            {
                addMenu = toolbar[i] as ToolbarMenu;
                if (addMenu != null && addMenu.text == "Add Node")
                    break;
            }

            if (addMenu == null)
                return;

            addMenu.menu.MenuItems().Clear();
            if (asset == null || asset.NodeTypes.Count == 0)
            {
                addMenu.menu.AppendAction(
                    "No node types defined",
                    _ => { },
                    DropdownMenuAction.Status.Disabled);
                return;
            }

            for (int i = 0; i < asset.NodeTypes.Count; i++)
            {
                WorldPlanGraphAsset.NodeTypeRecord type = asset.NodeTypes[i];
                if (type == null)
                    continue;

                string typeId = type.id;
                string displayName = string.IsNullOrWhiteSpace(type.displayName)
                    ? typeId
                    : type.displayName;

                addMenu.menu.AppendAction(
                    displayName,
                    _ => AddNode(typeId, new Vector2(80f + asset.Nodes.Count * 24f, 80f + asset.Nodes.Count * 16f)),
                    DropdownMenuAction.AlwaysEnabled);
            }
        }

        private void AddNode(string typeId, Vector2 position)
        {
            if (asset == null || asset.FindNodeType(typeId) == null)
                return;

            Undo.RecordObject(asset, "Add World Plan Node");
            asset.AddNode(WorldPlanGraphAsset.CreateNode(typeId, typeId, position));
            asset.EnsureIds();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Rebuild();
        }

        private void SelectGraphAsset()
        {
            Selection.activeObject = asset;
            if (asset != null)
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
                public WorldPlanGraphAsset.NodeTypeRecord TypeRecord { get; }

                public WorldPlanGraphNodeView(
                    WorldPlanGraphAsset.NodeRecord node,
                    WorldPlanGraphAsset.NodeTypeRecord type)
                {
                    NodeId = node.id;
                    TypeRecord = type;
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
                styleSheets.Add(Resources.Load<StyleSheet>("WorldPlanGraphWindow"));
                Insert(0, new GridBackground());
                AddManipulator(new ContentDragger());
                AddManipulator(new SelectionDragger());
                AddManipulator(new RectangleSelector());
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

                WorldPlanGraphAsset.NodeTypeRecord startType = asset.FindNodeType(
                    FindNodeTypeId(startBinding.NodeId));
                WorldPlanGraphAsset.PortRecord startPortRecord = FindPortRecord(startBinding);
                if (startType == null || startPortRecord == null)
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

                        WorldPlanGraphAsset.NodeTypeRecord type = asset.FindNodeType(node.typeId);
                        if (type == null)
                            continue;

                        WorldPlanGraphNodeView view = CreateNodeView(node, type);
                        nodesById.Add(node.id, view);
                        AddElement(view);
                    }

                    for (int i = 0; i < asset.Connections.Count; i++)
                    {
                        WorldPlanGraphAsset.ConnectionRecord connection = asset.Connections[i];
                        if (connection == null)
                            continue;
                        CreateEdgeView(connection);
                    }
                }

                rebuilding = false;
            }

            private WorldPlanGraphNodeView CreateNodeView(
                WorldPlanGraphAsset.NodeRecord node,
                WorldPlanGraphAsset.NodeTypeRecord type)
            {
                var view = new WorldPlanGraphNodeView(node, type);

                for (int i = 0; i < type.ports.Count; i++)
                {
                    WorldPlanGraphAsset.PortRecord portRecord = type.ports[i];
                    if (portRecord == null)
                        continue;

                    if (portRecord.direction == WorldPlanPortDirection.Input
                        || portRecord.direction == WorldPlanPortDirection.Bidirectional)
                    {
                        Port inputPort = CreatePort(
                            node.id,
                            portRecord,
                            Direction.Input,
                            "In");
                        view.inputContainer.Add(inputPort);
                    }

                    if (portRecord.direction == WorldPlanPortDirection.Output
                        || portRecord.direction == WorldPlanPortDirection.Bidirectional)
                    {
                        Port outputPort = CreatePort(
                            node.id,
                            portRecord,
                            Direction.Output,
                            "Out");
                        view.outputContainer.Add(outputPort);
                    }
                }

                view.RefreshExpandedState();
                view.RefreshPorts();
                view.SetPosition(new Rect(node.position, new Vector2(220f, 120f)));
                return view;
            }

            private Port CreatePort(
                string nodeId,
                WorldPlanGraphAsset.PortRecord portRecord,
                Direction direction,
                string suffix)
            {
                Port port = Port.Create(
                    Orientation.Horizontal,
                    direction,
                    portRecord.allowMultipleConnections
                        ? Port.Capacity.Multi
                        : Port.Capacity.Single,
                    typeof(float));
                port.portName = portRecord.displayName + " " + suffix;
                port.userData = new PortBinding(nodeId, portRecord.id);
                port.tooltip = string.IsNullOrWhiteSpace(portRecord.semanticType)
                    ? portRecord.displayName
                    : portRecord.displayName + " [" + portRecord.semanticType + "]";
                port.AddManipulator(new EdgeConnector<Edge>(new EdgeConnectorListener()));
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

                var edge = new Edge
                {
                    output = sourcePort,
                    input = targetPort,
                    userData = connection.id
                };
                edge.output.Connect(edge);
                edge.input.Connect(edge);
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
                        if (record == null)
                            continue;

                        if (record.position != nodeView.GetPosition().position)
                        {
                            Undo.RecordObject(asset, "Move World Plan Node");
                            record.position = nodeView.GetPosition().position;
                            changed = true;
                        }
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
                        {
                            continue;
                        }

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
                    asset.EnsureIds();
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssets();
                }

                return change;
            }

            private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
            {
                if (asset == null || asset.NodeTypes.Count == 0)
                    return;

                Vector2 localPosition = contentViewContainer.WorldToLocal(evt.eventInfo.mousePosition);
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
            }

            private void AddNode(string typeId, Vector2 position)
            {
                Undo.RecordObject(asset, "Add World Plan Node");
                asset.AddNode(WorldPlanGraphAsset.CreateNode(typeId, typeId, position));
                asset.EnsureIds();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Rebuild();
            }

            private string FindNodeTypeId(string nodeId)
            {
                WorldPlanGraphAsset.NodeRecord node = asset.FindNode(nodeId);
                return node?.typeId;
            }

            private WorldPlanGraphAsset.PortRecord FindPortRecord(PortBinding binding)
            {
                WorldPlanGraphAsset.NodeRecord node = asset.FindNode(binding.NodeId);
                if (node == null)
                    return null;

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

            private Port FindPort(
                WorldPlanGraphNodeView node,
                string portId,
                Direction direction)
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

            private sealed class EdgeConnectorListener : IEdgeConnectorListener
            {
                public void OnDropOutsidePort(Edge edge, Vector2 position)
                {
                }

                public void OnDrop(GraphView graphView, Edge edge)
                {
                    if (edge.output == null || edge.input == null)
                        return;

                    graphView.AddElement(edge);
                }
            }
        }
    }
}
