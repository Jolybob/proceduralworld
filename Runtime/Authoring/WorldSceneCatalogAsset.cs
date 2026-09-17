using System.Collections.Generic;
using UnityEngine;

namespace Jolybob.ProceduralWorld.Authoring
{
    [CreateAssetMenu(fileName = "WorldSceneCatalog", menuName = "Procedural World/Scene Catalog", order = 21)]
    public sealed class WorldSceneCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<WorldSceneDefinitionAsset> scenes = new List<WorldSceneDefinitionAsset>();
        [SerializeField] private string selectionSalt = "world-scenes";

        public string SelectionSalt => selectionSalt;

        public WorldSceneCatalog BuildCatalog()
        {
            var definitions = new List<WorldSceneDefinition>();
            if (scenes != null)
                for (int i = 0; i < scenes.Count; i++)
                    if (scenes[i] != null) definitions.Add(scenes[i].BuildDefinition());
            return new WorldSceneCatalog(definitions);
        }
    }
}
