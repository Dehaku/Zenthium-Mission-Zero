using System;
using UnityEngine;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.World
{
    /// <summary>
    /// The fog of war (maps.md, Visibility): the hero the player controls reveals the map by line of sight, out to the
    /// vision range, seeing through windows, open doors and broken walls (<see cref="LineOfSight"/>). Every update it
    /// casts a fan of rays across the map at eye height into <see cref="Grid"/>. The grid is made for each new map, all
    /// unexplored. Smoke doesn't block it (a user decision for 8b). Views draw it; nothing here draws.
    /// </summary>
    public sealed class FogOfWar : MonoBehaviour
    {
        private const int RaycastBufferSize = 64;

        [Tooltip("The scene's module services, for the game data's vision range.")]
        [SerializeField] private ModuleWorld world;

        [SerializeField] private MapAssembler assembler;

        [Tooltip("The hero the player controls. Only it reveals the map.")]
        [SerializeField] private EntityBehaviour viewer;

        [Tooltip("Meters across one fog cell. Smaller is sharper but costs more to cast and draw.")]
        [SerializeField, Min(0.25f)] private float cellSize = 1f;

        [Tooltip("Rays in the fan. Enough that neighboring rays at the vision range are under a cell apart.")]
        [SerializeField, Min(8)] private int rayCount = 720;

        [Tooltip("Real seconds between updates.")]
        [SerializeField, Min(0.02f)] private float updateInterval = 0.1f;

        [Tooltip("Meters above the ground the rays are cast at: an entity's middle.")]
        [SerializeField, Min(0f)] private float eyeHeight = 1f;

        private readonly RaycastHit[] hits = new RaycastHit[RaycastBufferSize];
        private Vector2[] directions = new Vector2[0];
        private float sinceUpdate;

        /// <summary>Raised when a new map's grid is made, all unexplored.</summary>
        public event Action OnGridCreated;

        /// <summary>Raised after each update of the grid.</summary>
        public event Action OnUpdated;

        /// <summary>Null until a map is built.</summary>
        public VisibilityGrid Grid { get; private set; }

        public EntityBehaviour Viewer => viewer;

        /// <summary>Whether a point on the map is in the player's sight now; false off the map or before one exists.</summary>
        public bool IsVisible(Vector3 point)
        {
            return Grid != null && Grid.IsVisible(new Vector2(point.x, point.z));
        }

        private void Awake()
        {
            if (world == null || assembler == null || viewer == null)
            {
                Debug.LogError($"{name}: {nameof(FogOfWar)} needs the Module World, the Map Assembler and the viewer.",
                    this);
                enabled = false;
                return;
            }

            directions = new Vector2[rayCount];
            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * Mathf.PI * 2f / rayCount;
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
        }

        private void OnEnable()
        {
            assembler.OnAssembled += CreateGrid;
            if (assembler.Layout != null && Grid == null)
            {
                CreateGrid();
            }
        }

        private void OnDisable()
        {
            if (assembler != null)
            {
                assembler.OnAssembled -= CreateGrid;
            }
        }

        private void Update()
        {
            sinceUpdate += Time.unscaledDeltaTime;
            if (Grid == null || sinceUpdate < updateInterval)
            {
                return;
            }

            sinceUpdate = 0f;
            Reveal();
        }

        private void CreateGrid()
        {
            Rect local = assembler.Layout.Area;
            Vector3 center = assembler.transform.position;
            Grid = new VisibilityGrid(new Rect(local.x + center.x, local.y + center.z, local.width, local.height),
                cellSize);

            // Revealed on the next frame, not now: whoever built the map may still be moving the player onto it.
            sinceUpdate = updateInterval;
            OnGridCreated?.Invoke();
        }

        private void Reveal()
        {
            Vector3 position = viewer.transform.position;
            var eye = new Vector3(position.x, eyeHeight, position.z);
            var origin = new Vector2(eye.x, eye.z);
            float range = world.Database.FormulaConstants.VisionRange;

            // Half a cell past what blocks sight, so the face of the wall itself shows.
            float overshoot = cellSize * 0.5f;
            Grid.BeginUpdate();
            foreach (Vector2 direction in directions)
            {
                float distance = LineOfSight.ClearDistance(eye, new Vector3(direction.x, 0f, direction.y), range,
                    hits);
                Grid.RevealRay(origin, direction, Mathf.Min(distance + overshoot, range));
            }

            Grid.EndUpdate();
            OnUpdated?.Invoke();
        }
    }
}
