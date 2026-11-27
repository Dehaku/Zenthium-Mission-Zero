using UnityEngine;

namespace Zenthium.World
{
    /// <summary>
    /// Draws <see cref="FogOfWar"/> on a flat quad over the map, above the walls: black where unexplored, dimmed where
    /// explored, clear in sight. The quad's material should be unlit and transparent, colored black; the fog sets its
    /// texture's alpha. Switched off, it hides the quad.
    /// </summary>
    public sealed class FogOfWarView : MonoBehaviour
    {
        // URP's base texture property.
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private const int BytesPerPixel = 4;
        private const int AlphaOffset = 3;

        [SerializeField] private FogOfWar fog;

        [Tooltip("A Quad primitive with the fog material. It's moved, turned and scaled to the map.")]
        [SerializeField] private Renderer quad;

        [Tooltip("Meters above the ground: over the walls and entities, under the camera.")]
        [SerializeField, Min(0f)] private float height = 4f;

        [SerializeField, Range(0f, 1f)] private float unexploredOpacity = 1f;
        [SerializeField, Range(0f, 1f)] private float exploredOpacity = 0.55f;

        private Texture2D texture;
        private byte[] pixels = new byte[0];
        private MaterialPropertyBlock block;
        private int shownVersion = -1;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            fog.OnGridCreated += Build;
            fog.OnUpdated += Redraw;
            if (fog.Grid != null)
            {
                Build();
            }

            quad.enabled = texture != null;
        }

        private void OnDisable()
        {
            if (fog != null)
            {
                fog.OnGridCreated -= Build;
                fog.OnUpdated -= Redraw;
            }

            if (quad != null)
            {
                quad.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }

        private void Build()
        {
            VisibilityGrid grid = fog.Grid;
            if (texture != null)
            {
                Destroy(texture);
            }

            texture = new Texture2D(grid.Columns, grid.Rows, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            pixels = new byte[grid.Columns * grid.Rows * BytesPerPixel];
            block.SetTexture(BaseMapId, texture);
            quad.SetPropertyBlock(block);

            // A Quad faces -Z; turned 90° about X it lies flat facing up, its U along X and its V along Z.
            // The last cells may reach a little past the map, so the quad covers the cells, not the map.
            float width = grid.Columns * grid.CellSize;
            float depth = grid.Rows * grid.CellSize;
            Transform quadTransform = quad.transform;
            quadTransform.SetPositionAndRotation(
                new Vector3(grid.Area.xMin + width * 0.5f, height, grid.Area.yMin + depth * 0.5f),
                Quaternion.Euler(90f, 0f, 0f));
            quadTransform.localScale = new Vector3(width, depth, 1f);
            quad.enabled = true;
            shownVersion = -1;
            Redraw();
        }

        private void Redraw()
        {
            VisibilityGrid grid = fog.Grid;
            if (texture == null || grid.Version == shownVersion)
            {
                return;
            }

            shownVersion = grid.Version;
            byte unexplored = (byte)Mathf.RoundToInt(unexploredOpacity * byte.MaxValue);
            byte explored = (byte)Mathf.RoundToInt(exploredOpacity * byte.MaxValue);
            int pixel = AlphaOffset;
            for (int row = 0; row < grid.Rows; row++)
            {
                for (int column = 0; column < grid.Columns; column++)
                {
                    FogState state = grid[column, row];
                    pixels[pixel] = state == FogState.Visible ? (byte)0
                        : state == FogState.Explored ? explored : unexplored;
                    pixel += BytesPerPixel;
                }
            }

            texture.SetPixelData(pixels, 0);
            texture.Apply(false);
        }
    }
}
