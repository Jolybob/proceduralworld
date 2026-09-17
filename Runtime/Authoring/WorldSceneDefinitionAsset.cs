using System.Collections.Generic;
using UnityEngine;

namespace Jolybob.ProceduralWorld.Authoring
{
    [CreateAssetMenu(fileName = "WorldSceneDefinition", menuName = "Procedural World/Scene Definition", order = 20)]
    public sealed class WorldSceneDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string id = "scene";
        [SerializeField, Min(0)] private int featureId;
        [SerializeField, Min(1)] private int width = 8;
        [SerializeField, Min(1)] private int height = 8;
        [SerializeField, Range(0f, 1f)] private float spawnChance = 1f;
        [SerializeField, Min(1)] private int maxPerChunk = 1;
        [SerializeField, Min(0)] private int minimumDistanceFromOrigin;
        [SerializeField, Min(0)] private int maxWorldInstances;
        [SerializeField, Min(0)] private int weight = 1;
        [SerializeField] private bool enabled = true;
        [SerializeField] private bool unique;
        [SerializeField] private WorldSceneOrientationMode orientation;
        [SerializeField] private List<string> tags = new List<string>();

        public string Id => id;

        public WorldSceneDefinition BuildDefinition()
        {
            return new WorldSceneDefinition(
                id,
                featureId,
                width,
                height,
                spawnChance,
                maxPerChunk,
                minimumDistanceFromOrigin,
                maxWorldInstances,
                weight,
                enabled,
                unique,
                orientation,
                tags);
        }
    }
}
