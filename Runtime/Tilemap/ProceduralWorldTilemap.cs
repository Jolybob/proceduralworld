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
        [SerializeField] private int seed = 59273;
        [SerializeField] private WorldGenerationSettings settings = new WorldGenerationSettings();

        [Header("Preview")]
        [SerializeField, Min(1)] private int chunksRadius = 2;
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool centerCameraOnWorld = true;

        private UnityTilemap tilemap;
        private readonly Dictionary<WorldTile, TileBase> tiles = new Dictionary<WorldTile, TileBase>();

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

            var generator = new ProceduralWorldGenerator(seed, settings);

            for (int chunkY = -chunksRadius; chunkY <= chunksRadius; chunkY++)
            {
                for (int chunkX = -chunksRadius; chunkX <= chunksRadius; chunkX++)
                {
                    var chunk = generator.GenerateChunk(new ChunkCoord(chunkX, chunkY));
                    RenderChunk(chunk);
                }
            }

            if (centerCameraOnWorld && Camera.main != null)
                Camera.main.transform.position = new Vector3(0f, 0f, Camera.main.transform.position.z);
        }

        private void EnsureTilemap()
        {
            tilemap = GetComponent<UnityTilemap>();
            if (tilemap != null)
                return;

            var grid = GetComponent<Grid>();
            if (grid == null)
                grid = gameObject.AddComponent<Grid>();

            tilemap = GetComponent<UnityTilemap>();
            if (tilemap == null)
                tilemap = gameObject.AddComponent<UnityTilemap>();

            var renderer = GetComponent<TilemapRenderer>();
            if (renderer == null)
                renderer = gameObject.AddComponent<TilemapRenderer>();
        }

        private void BuildRuntimeTiles()
        {
            if (tiles.Count > 0)
                return;

            tiles[WorldTile.Core] = CreateTile("Core", new Color(0.95f, 0.75f, 0.25f));
            tiles[WorldTile.Inner] = CreateTile("Inner", new Color(0.45f, 0.30f, 0.65f));
            tiles[WorldTile.Mid] = CreateTile("Mid", new Color(0.25f, 0.55f, 0.75f));
            tiles[WorldTile.Deep] = CreateTile("Deep", new Color(0.18f, 0.24f, 0.30f));
            tiles[WorldTile.Empty] = CreateTile("Empty", new Color(0f, 0f, 0f, 0f));
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

        private void RenderChunk(GeneratedChunk chunk)
        {
            int size = chunk.Size;
            var positions = new Vector3Int[size * size];
            var tileBases = new TileBase[size * size];

            int i = 0;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var cell = chunk.GetCell(x, y);
                    positions[i] = new Vector3Int(
                        chunk.Coordinate.X * size + x,
                        chunk.Coordinate.Y * size + y,
                        0);
                    tileBases[i] = tiles[cell.Tile];
                    i++;
                }
            }

            tilemap.SetTiles(positions, tileBases);
        }
    }
}
