using UnityEditor;
using UnityEngine;
using Jolybob.ProceduralWorld.Authoring;

namespace Jolybob.ProceduralWorld.Editor
{
    [CustomEditor(typeof(WorldPlanGraphAsset))]
    public sealed class WorldPlanGraphAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            var graph = (WorldPlanGraphAsset)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Node Graph"))
                {
                    WorldPlanGraphWindow.Open(graph);
                }

                if (GUILayout.Button("Validate"))
                {
                    WorldPlanValidationResult result = graph.Validate();
                    int errors = 0;
                    int warnings = 0;
                    for (int i = 0; i < result.Issues.Count; i++)
                    {
                        if (result.Issues[i].Severity == WorldPlanValidationSeverity.Error)
                            errors++;
                        else
                            warnings++;
                    }

                    if (result.IsValid)
                        Debug.Log("World plan graph is valid: " + result.Issues.Count + " warning(s).", graph);
                    else
                        Debug.LogError("World plan graph has " + errors + " error(s) and " + warnings + " warning(s).", graph);
                }
            }

            if (GUILayout.Button("Compile Runtime Plan"))
            {
                WorldPlanCompilationResult result = graph.Compile();
                if (!result.Succeeded)
                {
                    Debug.LogError("World plan graph could not compile because validation failed.", graph);
                }
                else
                {
                    Debug.Log(
                        "Compiled world plan: "
                        + result.Plan.Nodes.Count
                        + " node(s), "
                        + result.Plan.Connections.Count
                        + " connection(s).",
                        graph);
                }
            }
        }
    }
}
