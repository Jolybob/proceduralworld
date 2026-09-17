using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Runtime policies for turning an authored semantic world plan into stable world-space data.
    /// Feature lowering and realization are optional so the plan/layout stages can be consumed on
    /// their own by applications that own feature catalogs or materialization semantics.
    /// </summary>
    public sealed class WorldPlanRuntimeSettings
    {
        public WorldPlanLayoutSettings LayoutSettings { get; }
        public WorldPlanFeatureLoweringSettings LoweringSettings { get; }
        public IWorldPlanFeatureResolver FeatureResolver { get; }
        public IWorldPlanPlacementFeasibility PlacementFeasibility { get; }
        public IWorldPlanRealizationSource RealizationSource { get; }

        public WorldPlanRuntimeSettings(
            WorldPlanLayoutSettings layoutSettings = null,
            WorldPlanFeatureLoweringSettings loweringSettings = null,
            IWorldPlanFeatureResolver featureResolver = null,
            IWorldPlanPlacementFeasibility placementFeasibility = null,
            IWorldPlanRealizationSource realizationSource = null)
        {
            LayoutSettings = layoutSettings ?? WorldPlanLayoutSettings.Default;
            LoweringSettings = loweringSettings ?? WorldPlanFeatureLoweringSettings.Default;
            FeatureResolver = featureResolver;
            PlacementFeasibility = placementFeasibility;
            RealizationSource = realizationSource;
        }

        public static WorldPlanRuntimeSettings Default => new WorldPlanRuntimeSettings();
    }

    /// <summary>
    /// Immutable runtime snapshot of the complete semantic-plan execution stages that have been
    /// configured. World coordinates are authoritative; chunk lookup is an acceleration path over
    /// the resulting realization map.
    /// </summary>
    public sealed class WorldPlanRuntime
    {
        private readonly List<WorldPlanValidationIssue> issues;
        private readonly WorldRealizationMap realizationMap;

        public int Seed { get; }
        public WorldPlanCompilationResult Compilation { get; }
        public WorldPlanLayoutResult LayoutResult { get; }
        public WorldPlanFeatureLoweringResult LoweringResult { get; }
        public WorldRealizationBatch Realization { get; }
        public WorldPlan Plan => Compilation == null ? null : Compilation.Plan;
        public WorldPlanLayout Layout => LayoutResult == null ? null : LayoutResult.Layout;
        public IReadOnlyList<WorldPlanValidationIssue> Issues => issues;
        public bool Succeeded
        {
            get
            {
                if (Plan == null || Layout == null)
                    return false;

                for (int i = 0; i < issues.Count; i++)
                {
                    if (issues[i].Severity == WorldPlanValidationSeverity.Error)
                        return false;
                }

                return true;
            }
        }

        internal WorldPlanRuntime(
            int seed,
            WorldPlanCompilationResult compilation,
            WorldPlanLayoutResult layoutResult,
            WorldPlanFeatureLoweringResult loweringResult,
            WorldRealizationBatch realization,
            WorldRealizationMap realizationMap,
            List<WorldPlanValidationIssue> issues)
        {
            Seed = seed;
            Compilation = compilation;
            LayoutResult = layoutResult;
            LoweringResult = loweringResult;
            Realization = realization ?? WorldRealizationBatch.Empty;
            this.realizationMap = realizationMap ?? new WorldRealizationMap();
            this.issues = issues ?? new List<WorldPlanValidationIssue>();
        }

        /// <summary>Copies only realization edits intersecting one chunk into the caller's buffer.</summary>
        public void CollectChunkRealizationEdits(ChunkCoord chunk, List<WorldRealizationEdit> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            realizationMap.CollectChunk(chunk, output);
        }

        public bool HasChunkRealization(ChunkCoord chunk)
        {
            var edits = new List<WorldRealizationEdit>();
            realizationMap.CollectChunk(chunk, edits);
            return edits.Count > 0;
        }
    }

    /// <summary>
    /// Authoritative runtime orchestration for semantic world plans.
    /// 
    /// The order is intentionally explicit:
    /// expand/compile -> world-space layout -> feature lowering -> realization -> chunk index.
    /// No chunk-local graph is created, and no mutable random state is used.
    /// </summary>
    public sealed class WorldPlanRuntimeBuilder
    {
        public WorldPlanRuntime Build(int seed, WorldPlanGraphDefinition definition, WorldPlanRuntimeSettings settings = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            settings = settings ?? WorldPlanRuntimeSettings.Default;
            var issues = new List<WorldPlanValidationIssue>();

            WorldPlanCompilationResult compilation = new WorldPlanSubgraphCompiler().Compile(seed, definition);
            AppendIssues(issues, compilation.Validation == null ? null : compilation.Validation.Issues);

            if (!compilation.Succeeded)
            {
                return CreateRuntime(seed, compilation, null, EmptyLowering(), WorldRealizationBatch.Empty, new WorldRealizationMap(), issues);
            }

            WorldPlanLayoutResult layout = new WorldPlanLayoutSolver().Solve(compilation.Plan, settings.LayoutSettings);
            AppendIssues(issues, layout.Issues);
            if (!layout.Succeeded)
            {
                return CreateRuntime(seed, compilation, layout, EmptyLowering(), WorldRealizationBatch.Empty, new WorldRealizationMap(), issues);
            }

            WorldPlanFeatureLoweringResult lowering = EmptyLowering();
            if (settings.FeatureResolver != null)
            {
                lowering = new WorldPlanFeatureLowerer().Lower(
                    compilation.Plan,
                    layout.Layout,
                    settings.FeatureResolver,
                    settings.PlacementFeasibility,
                    settings.LoweringSettings);
                AppendIssues(issues, lowering.Issues);
            }

            if (settings.RealizationSource != null && settings.FeatureResolver == null)
            {
                issues.Add(new WorldPlanValidationIssue(
                    WorldPlanValidationSeverity.Error,
                    "RealizationSourceWithoutFeatureResolver",
                    "A world-plan realization source requires a feature resolver so semantic nodes can be lowered into placements."));
            }

            WorldRealizationBatch realization = WorldRealizationBatch.Empty;
            if (settings.RealizationSource != null && lowering.Succeeded && settings.FeatureResolver != null)
            {
                realization = new WorldPlanRealizer().Realize(lowering, settings.RealizationSource);
            }

            var realizationMap = new WorldRealizationMap(settings.LoweringSettings.ChunkSize);
            for (int i = 0; i < realization.Edits.Count; i++)
            {
                WorldRealizationEdit edit = realization.Edits[i];
                if (realizationMap.TryAdd(edit, out WorldRealizationConflict conflict))
                    continue;

                issues.Add(new WorldPlanValidationIssue(
                    WorldPlanValidationSeverity.Error,
                    "RealizationConflict",
                    "World realization edit '" + edit.Id + "' conflicts with existing edit '" + conflict.Existing.Id + "' at " + edit.Position + "."));
            }

            return CreateRuntime(seed, compilation, layout, lowering, realization, realizationMap, issues);
        }

        private static WorldPlanRuntime CreateRuntime(
            int seed,
            WorldPlanCompilationResult compilation,
            WorldPlanLayoutResult layout,
            WorldPlanFeatureLoweringResult lowering,
            WorldRealizationBatch realization,
            WorldRealizationMap realizationMap,
            List<WorldPlanValidationIssue> issues)
        {
            return new WorldPlanRuntime(seed, compilation, layout, lowering, realization, realizationMap, issues);
        }

        private static WorldPlanFeatureLoweringResult EmptyLowering()
        {
            return new WorldPlanFeatureLoweringResult(
                new List<WorldPlanFeaturePlacement>(),
                new List<WorldPlanValidationIssue>());
        }

        private static void AppendIssues(
            List<WorldPlanValidationIssue> destination,
            IReadOnlyList<WorldPlanValidationIssue> source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
                destination.Add(source[i]);
        }
    }
}
