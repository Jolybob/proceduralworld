using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    public sealed class WorldPlanSubgraphExpansionResult
    {
        public WorldPlanGraphDefinition ExpandedDefinition { get; }
        public IReadOnlyList<WorldPlanValidationIssue> Issues { get; }
        public bool Succeeded
        {
            get
            {
                if (ExpandedDefinition == null || Issues == null)
                    return false;

                for (int i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i].Severity == WorldPlanValidationSeverity.Error)
                        return false;
                }

                return true;
            }
        }

        internal WorldPlanSubgraphExpansionResult(
            WorldPlanGraphDefinition expandedDefinition,
            List<WorldPlanValidationIssue> issues)
        {
            ExpandedDefinition = expandedDefinition;
            Issues = issues;
        }
    }

    /// <summary>
    /// Deterministically lowers reusable world-plan subgraphs into a flat runtime graph.
    /// Template instances disappear during compilation; their exposed ports are rewired to
    /// the concrete ports inside the template instance.
    /// </summary>
    public sealed class WorldPlanSubgraphCompiler
    {
        private sealed class Endpoint
        {
            public readonly string NodeId;
            public readonly string PortId;

            public Endpoint(string nodeId, string portId)
            {
                NodeId = nodeId;
                PortId = portId;
            }
        }

        private sealed class GraphExpansionResult
        {
            public readonly Dictionary<string, Endpoint> ExposedPorts =
                new Dictionary<string, Endpoint>(StringComparer.Ordinal);
        }

        public WorldPlanSubgraphExpansionResult Expand(WorldPlanGraphDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var expandedTypes = new List<WorldPlanNodeTypeDefinition>();
            var expandedNodes = new List<WorldPlanNodeDefinition>();
            var expandedConnections = new List<WorldPlanConnectionDefinition>();
            var issues = new List<WorldPlanValidationIssue>();

            ExpandGraph(
                definition,
                string.Empty,
                null,
                new HashSet<WorldPlanSubgraphTemplateDefinition>(),
                expandedTypes,
                expandedNodes,
                expandedConnections,
                issues,
                null);

            return new WorldPlanSubgraphExpansionResult(
                new WorldPlanGraphDefinition(expandedTypes, expandedNodes, expandedConnections),
                issues);
        }

        public WorldPlanValidationResult Validate(WorldPlanGraphDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            WorldPlanSubgraphExpansionResult expansion = Expand(definition);
            var issues = new List<WorldPlanValidationIssue>();
            for (int i = 0; i < expansion.Issues.Count; i++)
                issues.Add(expansion.Issues[i]);

            if (expansion.ExpandedDefinition != null && !HasErrors(expansion.Issues))
            {
                WorldPlanValidationResult flatValidation = new WorldPlanCompiler().Validate(expansion.ExpandedDefinition);
                for (int i = 0; i < flatValidation.Issues.Count; i++)
                    issues.Add(flatValidation.Issues[i]);
            }

            return new WorldPlanValidationResult(issues);
        }

        public WorldPlanCompilationResult Compile(int seed, WorldPlanGraphDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            WorldPlanSubgraphExpansionResult expansion = Expand(definition);
            var expansionIssues = new List<WorldPlanValidationIssue>();
            for (int i = 0; i < expansion.Issues.Count; i++)
                expansionIssues.Add(expansion.Issues[i]);

            if (HasErrors(expansionIssues))
                return new WorldPlanCompilationResult(null, new WorldPlanValidationResult(expansionIssues));

            WorldPlanCompilationResult flatCompilation = new WorldPlanCompiler().Compile(seed, expansion.ExpandedDefinition);
            for (int i = 0; i < flatCompilation.Validation.Issues.Count; i++)
                expansionIssues.Add(flatCompilation.Validation.Issues[i]);

            WorldPlanValidationResult validation = new WorldPlanValidationResult(expansionIssues);
            return new WorldPlanCompilationResult(
                flatCompilation.Succeeded ? flatCompilation.Plan : null,
                validation);
        }

        private static void ExpandGraph(
            WorldPlanGraphDefinition graph,
            string scope,
            IReadOnlyList<WorldPlanSubgraphPortDefinition> exposedPorts,
            HashSet<WorldPlanSubgraphTemplateDefinition> activeTemplates,
            List<WorldPlanNodeTypeDefinition> outputTypes,
            List<WorldPlanNodeDefinition> outputNodes,
            List<WorldPlanConnectionDefinition> outputConnections,
            List<WorldPlanValidationIssue> issues,
            GraphExpansionResult result)
        {
            var typeById = new Dictionary<string, WorldPlanNodeTypeDefinition>(StringComparer.Ordinal);
            var expandedTypeByOriginalId = new Dictionary<string, WorldPlanNodeTypeDefinition>(StringComparer.Ordinal);
            var orderedTypes = new List<WorldPlanNodeTypeDefinition>(graph.NodeTypes);
            orderedTypes.Sort(CompareTypes);

            for (int i = 0; i < orderedTypes.Count; i++)
            {
                WorldPlanNodeTypeDefinition type = orderedTypes[i];
                if (type == null)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "NullNodeType",
                        "Node type definition is null."));
                    continue;
                }
                if (!typeById.TryAdd(type.Id, type))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "DuplicateNodeType",
                        "Node type ID is duplicated: " + type.Id));
                    continue;
                }

                string expandedTypeId = string.IsNullOrEmpty(scope)
                    ? type.Id
                    : ScopeId(scope, "type::" + type.Id);
                expandedTypeByOriginalId.Add(
                    type.Id,
                    new WorldPlanNodeTypeDefinition(
                        expandedTypeId,
                        type.DisplayName,
                        type.Category,
                        type.MinimumWidth,
                        type.MinimumHeight,
                        type.MinimumClearance,
                        type.Ports,
                        type.PropertyKeys));
            }

            for (int i = 0; i < orderedTypes.Count; i++)
            {
                WorldPlanNodeTypeDefinition type = orderedTypes[i];
                if (type != null && expandedTypeByOriginalId.TryGetValue(type.Id, out WorldPlanNodeTypeDefinition expandedType))
                    outputTypes.Add(expandedType);
            }

            var templateIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < graph.Templates.Count; i++)
            {
                WorldPlanSubgraphTemplateDefinition template = graph.Templates[i];
                if (template == null)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "NullTemplate",
                        "Subgraph template definition is null."));
                    continue;
                }
                if (!templateIds.Add(template.Id))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "DuplicateTemplate",
                        "Subgraph template ID is duplicated: " + template.Id));
                }
            }

            var concreteNodeIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var templatePortBindings = new Dictionary<string, Dictionary<string, Endpoint>>(StringComparer.Ordinal);
            var orderedNodes = new List<WorldPlanNodeDefinition>(graph.Nodes);
            orderedNodes.Sort(CompareNodes);

            for (int i = 0; i < orderedNodes.Count; i++)
            {
                WorldPlanNodeDefinition node = orderedNodes[i];
                if (node == null)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "NullNode",
                        "Node definition is null."));
                    continue;
                }

                if (concreteNodeIds.ContainsKey(node.Id) || templatePortBindings.ContainsKey(node.Id))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "DuplicateNode",
                        "Node ID is duplicated: " + node.Id,
                        node.Id));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.TemplateId))
                {
                    if (!expandedTypeByOriginalId.TryGetValue(node.TypeId, out WorldPlanNodeTypeDefinition expandedType))
                    {
                        issues.Add(new WorldPlanValidationIssue(
                            WorldPlanValidationSeverity.Error,
                            "UnknownNodeType",
                            "Node '" + node.Id + "' references missing node type '" + node.TypeId + "'.",
                            node.Id));
                        continue;
                    }

                    string expandedNodeId = ScopeId(scope, node.Id);
                    concreteNodeIds.Add(node.Id, expandedNodeId);
                    outputNodes.Add(new WorldPlanNodeDefinition(
                        expandedNodeId,
                        expandedType.Id,
                        node.Label,
                        node.Properties));
                    continue;
                }

                WorldPlanSubgraphTemplateDefinition template = FindTemplate(graph.Templates, node.TemplateId);
                if (template == null)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "UnknownTemplate",
                        "Node '" + node.Id + "' references missing subgraph template '" + node.TemplateId + "'.",
                        node.Id));
                    continue;
                }

                if (!activeTemplates.Add(template))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "TemplateCycle",
                        "Subgraph template cycle detected through template '" + template.Id + "'.",
                        node.Id));
                    continue;
                }

                var childResult = new GraphExpansionResult();
                ExpandGraph(
                    template.Graph,
                    ScopeId(scope, node.Id),
                    template.ExposedPorts,
                    activeTemplates,
                    outputTypes,
                    outputNodes,
                    outputConnections,
                    issues,
                    childResult);
                activeTemplates.Remove(template);
                templatePortBindings.Add(node.Id, childResult.ExposedPorts);
            }

            var orderedConnections = new List<WorldPlanConnectionDefinition>(graph.Connections);
            orderedConnections.Sort(CompareConnections);
            for (int i = 0; i < orderedConnections.Count; i++)
            {
                WorldPlanConnectionDefinition connection = orderedConnections[i];
                if (connection == null)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "NullConnection",
                        "Connection definition is null."));
                    continue;
                }

                Endpoint source = ResolveEndpoint(
                    connection.SourceNodeId,
                    connection.SourcePortId,
                    concreteNodeIds,
                    templatePortBindings,
                    issues,
                    connection.Id);
                Endpoint target = ResolveEndpoint(
                    connection.TargetNodeId,
                    connection.TargetPortId,
                    concreteNodeIds,
                    templatePortBindings,
                    issues,
                    connection.Id);
                if (source == null || target == null)
                    continue;

                outputConnections.Add(new WorldPlanConnectionDefinition(
                    ScopeId(scope, connection.Id),
                    source.NodeId,
                    source.PortId,
                    target.NodeId,
                    target.PortId,
                    connection.Kind));
            }

            PopulateExposedPorts(exposedPorts, concreteNodeIds, templatePortBindings, issues, result);
        }

        private static void PopulateExposedPorts(
            IReadOnlyList<WorldPlanSubgraphPortDefinition> exposedPorts,
            Dictionary<string, string> concreteNodeIds,
            Dictionary<string, Dictionary<string, Endpoint>> templatePortBindings,
            List<WorldPlanValidationIssue> issues,
            GraphExpansionResult result)
        {
            if (result == null || exposedPorts == null)
                return;

            var orderedPorts = new List<WorldPlanSubgraphPortDefinition>(exposedPorts);
            orderedPorts.Sort((left, right) => string.CompareOrdinal(left?.Id, right?.Id));
            var ids = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < orderedPorts.Count; i++)
            {
                WorldPlanSubgraphPortDefinition exposed = orderedPorts[i];
                if (exposed == null)
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "NullExposedPort",
                        "Subgraph exposed port definition is null."));
                    continue;
                }
                if (!ids.Add(exposed.Id))
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "DuplicateExposedPort",
                        "Exposed port ID is duplicated: " + exposed.Id));
                    continue;
                }

                Endpoint endpoint = null;
                if (concreteNodeIds.TryGetValue(exposed.NodeId, out string concreteNodeId))
                {
                    endpoint = new Endpoint(concreteNodeId, exposed.PortId);
                }
                else if (templatePortBindings.TryGetValue(exposed.NodeId, out Dictionary<string, Endpoint> nestedPorts))
                {
                    if (!nestedPorts.TryGetValue(exposed.PortId, out endpoint))
                    {
                        issues.Add(new WorldPlanValidationIssue(
                            WorldPlanValidationSeverity.Error,
                            "MissingNestedExposedPort",
                            "Nested subgraph instance '" + exposed.NodeId + "' does not expose port '" + exposed.PortId + "'."));
                    }
                }
                else
                {
                    issues.Add(new WorldPlanValidationIssue(
                        WorldPlanValidationSeverity.Error,
                        "MissingExposedPortNode",
                        "Exposed port '" + exposed.Id + "' references missing node '" + exposed.NodeId + "'."));
                }

                if (endpoint != null)
                    result.ExposedPorts.Add(exposed.Id, endpoint);
            }
        }

        private static Endpoint ResolveEndpoint(
            string nodeId,
            string portId,
            Dictionary<string, string> concreteNodeIds,
            Dictionary<string, Dictionary<string, Endpoint>> templatePortBindings,
            List<WorldPlanValidationIssue> issues,
            string connectionId)
        {
            if (concreteNodeIds.TryGetValue(nodeId, out string expandedNodeId))
                return new Endpoint(expandedNodeId, portId);

            if (templatePortBindings.TryGetValue(nodeId, out Dictionary<string, Endpoint> exposedPorts))
            {
                if (exposedPorts.TryGetValue(portId, out Endpoint endpoint))
                    return endpoint;

                issues.Add(new WorldPlanValidationIssue(
                    WorldPlanValidationSeverity.Error,
                    "MissingExposedPort",
                    "Subgraph instance '" + nodeId + "' does not expose port '" + portId + "'.",
                    nodeId,
                    connectionId));
                return null;
            }

            issues.Add(new WorldPlanValidationIssue(
                WorldPlanValidationSeverity.Error,
                "MissingConnectionNode",
                "Connection references missing node '" + nodeId + "'.",
                nodeId,
                connectionId));
            return null;
        }

        private static WorldPlanSubgraphTemplateDefinition FindTemplate(
            IReadOnlyList<WorldPlanSubgraphTemplateDefinition> templates,
            string templateId)
        {
            if (templates == null)
                return null;

            for (int i = 0; i < templates.Count; i++)
            {
                WorldPlanSubgraphTemplateDefinition template = templates[i];
                if (template != null && string.Equals(template.Id, templateId, StringComparison.Ordinal))
                    return template;
            }

            return null;
        }

        private static bool HasErrors(IReadOnlyList<WorldPlanValidationIssue> issues)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == WorldPlanValidationSeverity.Error)
                    return true;
            }

            return false;
        }

        private static string ScopeId(string scope, string id)
        {
            return string.IsNullOrEmpty(scope) ? id : scope + "/" + id;
        }

        private static int CompareTypes(WorldPlanNodeTypeDefinition left, WorldPlanNodeTypeDefinition right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return string.CompareOrdinal(left.Id, right.Id);
        }

        private static int CompareNodes(WorldPlanNodeDefinition left, WorldPlanNodeDefinition right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return string.CompareOrdinal(left.Id, right.Id);
        }

        private static int CompareConnections(WorldPlanConnectionDefinition left, WorldPlanConnectionDefinition right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            int comparison = string.CompareOrdinal(left.SourceNodeId, right.SourceNodeId);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.SourcePortId, right.SourcePortId);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.TargetNodeId, right.TargetNodeId);
            if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(left.TargetPortId, right.TargetPortId);
            if (comparison != 0) return comparison;
            comparison = left.Kind.CompareTo(right.Kind);
            if (comparison != 0) return comparison;
            return string.CompareOrdinal(left.Id, right.Id);
        }
    }
}
