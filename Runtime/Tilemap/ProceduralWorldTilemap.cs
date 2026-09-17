using System.Collections.Generic;
using UnityEngine;
using UnityTilemap = UnityEngine.Tilemaps.Tilemap;
using UnityEngine.Tilemaps;
using Jolybob.ProceduralWorld;

namespace Jolybob.ProceduralWorld.Tilemap
{
    [DisallowMultipleComponent]
    public sealed class ProceduralWorldTilemap : MonoBehaviour
    {
        [Header("Generation")]
        [SerializeField] private int seed = 281604;
        [SerializeField] private WorldGenerationSettings settings = new WorldGenerationSettings();

        [Header("Preview")]
        [SerializeField, Min(1)] private int chunksRadius = 2;
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool centerCameraOnWorld = true;
        [SerializeField] private bool frameCameraOnWorld = true;

        private UnityTilemap tilemap;
        private TilemapRenderer tilemapRenderer;
        private WorldTilemapRenderer worldRenderer;
        private Material runtimeTilemapMaterial;
        private readonly Dictionary<WorldTile, TileBase> tiles = new Dictionary<WorldTile, TileBase>();
        private readonly Dictionary<CellTopology, TileBase> topologyTiles = new Dictionary<CellTopology, TileBase>();
        private readonly Dictionary<ResourceId, TileBase> resourceTiles = new Dictionary<ResourceId, TileBase>();
        private readonly Dictionary<StructureId, TileBase> structureTiles = new Dictionary<StructureId, TileBase>();

        private void Start()
        {
            if (generateOnStart)
                Generate();
        }

        [ContextMenu("Generate World")]
        public void Generate()
        {
            EnsureTilemap();
            BuildRuntimeTiles();
            tilemap.ClearAllTiles();

            var terrainCatalog = new WorldCellVisualCatalog(tiles);
            var visualResolver = new WorldCellVisualLayerCatalog(
                new IWorldCellVisualLayer[]
                {
                    new TopologyWorldCellVisualLayer(topologyTiles),
                    new ResourceWorldCellVisualLayer(resourceTiles),
                    new StructureWorldCellVisualLayer(structureTiles)
                },
                terrainCatalog);

            worldRenderer = new WorldTilemapRenderer(tilemap, settings.chunkSize, visualResolver);

            var generator = new ProceduralWorldGenerator(seed, settings);

            for (int chunkY = -chunksRadius; chunkY <= chunksRadius; chunkY++)
            {
                for (int chunkX = -chunksRadius; chunkX <= chunksRadius; chunkX++)
                {
                    var chunk = generator.GenerateChunk(new ChunkCoord(chunkX, chunkY));
                    worldRenderer.Load(chunk.Coordinate, chunk);
                }
            }

            if (centerCameraOnWorld && Camera.main != null)
                CenterCamera(Camera.main);
        }

        private void EnsureTilemap()
        {
            tilemap = GetComponent<UnityTilemap>();
            if (tilemap == null)
            {
                var grid = GetComponent<Grid>();
                if (grid == null)
                    gameObject.AddComponent<Grid>();

                tilemap = gameObject.AddComponent<UnityTilemap>();
            }

            tilemapRenderer = GetComponent<TilemapRenderer>();
            if (tilemapRenderer == null)
                tilemapRenderer = gameObject.AddComponent<TilemapRenderer>();

            tilemapRenderer.enabled = true;
            tilemapRenderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
            tilemapRenderer.sortingLayerName = "Default";
            tilemapRenderer.sortingOrder = 0;

            if (runtimeTilemapMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

                if (shader != null)
                {
                    runtimeTilemapMaterial = new Material(shader)
                    {
                        name = "ProceduralWorld_RuntimeTilemapMaterial"
                    };
                }
            }

            if (runtimeTilemapMaterial != null)
                tilemapRenderer.sharedMaterial = runtimeTilemapMaterial;
        }

        private void CenterCamera(Camera camera)
        {
            var cameraTransform = camera.transform;
            var cameraPosition = cameraTransform.position;
            cameraPosition.x = 0f;
            cameraPosition.y = 0f;

            cameraTransform.rotation = Quaternion.identity;

            if (cameraPosition.z >= -0.1f)
                cameraPosition.z = -10f;

            cameraTransform.position = cameraPosition;

            if (!frameCameraOnWorld)
                return;

            camera.orthographic = true;

            var diameter = (chunksRadius * 2 + 1) * settings.chunkSize;
            var halfHeight = diameter * 0.5f;
            var halfWidth = halfHeight * Mathf.Max(camera.aspect, 0.01f);
            camera.orthographicSize = Mathf.Max(halfHeight, halfWidth);
        }

        private void BuildRuntimeTiles()
        {
            if (tiles.Count == 0)
            {
                tiles[WorldTile.Core] = CreateTile("Core", new Color(0.95f, 0.75f, 0.25f));
                tiles[WorldTile.Inner] = CreateTile("Inner", new Color(0.45f, 0.30f, 0.65f));
                tiles[WorldTile.Mid] = CreateTile("Mid", new Color(0.25f, 0.55f, 0.75f));
                tiles[WorldTile.Deep] = CreateTile("Deep", new Color(0.18f, 0.24f, 0.30f));
                tiles[WorldTile.Empty] = CreateTile("Empty", new Color(0f, 0f, 0f, 0f));
            }

            if (topologyTiles.Count == 0)
            {
                // Solid is the default topology for generated terrain. Leave it unmapped here so
                // the terrain catalog remains the authoritative visual for normal solid cells.
                // Only explicit topology changes override terrain in this preview.
                topologyTiles[CellTopology.Water] = CreateTile("Water", new Color(0.08f, 0.42f, 0.85f, 0.9f));
                topologyTiles[CellTopology.Lava] = CreateTile("Lava", new Color(0.9f, 0.22f, 0.04f, 0.95f));
                topologyTiles[CellTopology.Chasm] = CreateTile("Chasm", new Color(0.04f, 0.03f, 0.05f, 1f));
            }

            if (resourceTiles.Count == 0)
                resourceTiles[new ResourceId(1)] = CreateTile("Resource", new Color(0.2f, 0.9f, 0.35f, 1f));

            if (structureTiles.Count == 0)
                structureTiles[new StructureId(1)] = CreateTile("Structure", new Color(0.95f, 0.55f, 0.2f, 1f));
        }

        private TileBase CreateTile(string tileName, Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = $"ProceduralWorld_{tileName}_Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f);
            sprite.name = $"ProceduralWorld_{tileName}_Sprite";

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = $"ProceduralWorld_{tileName}_Tile";
            tile.sprite = sprite;
            tile.color = Color.white;
            return tile;
        }
    }
}