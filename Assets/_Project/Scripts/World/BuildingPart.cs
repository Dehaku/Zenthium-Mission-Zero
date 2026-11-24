using UnityEngine;
using UnityEngine.AI;
using Zenthium.Data;

namespace Zenthium.World
{
    /// <summary>
    /// A wall, window or door panel (world.md, Walls and Doors): its material sets its HP. Broken, it leaves a gap
    /// to walk and see through: its colliders and visuals go, and so does its NavMesh carving. Walls and windows are
    /// left out of the NavMesh bake and carve their own hole while they stand; doors don't carve, since enemies plan
    /// paths as if doors weren't there.
    /// </summary>
    [RequireComponent(typeof(Structure))]
    public sealed class BuildingPart : MonoBehaviour
    {
        [SerializeField] private StructureMaterial material = StructureMaterial.Wood;

        private Structure structure;
        private Collider[] colliders = new Collider[0];
        private Renderer[] renderers = new Renderer[0];
        private NavMeshObstacle obstacle;
        private bool isSolid = true;

        public StructureMaterial Material => material;
        public Structure Structure => structure;
        public bool IsBroken => structure != null && structure.IsBroken;

        /// <summary>Null until its building wakes, or if it has none.</summary>
        public Building Building { get; internal set; }

        /// <summary>
        /// Shows or hides it while it stands, such as a door opening. Once broken it stays gone.
        /// </summary>
        public void SetSolid(bool solid)
        {
            isSolid = solid;
            Apply();
        }

        private void Awake()
        {
            structure = GetComponent<Structure>();
            colliders = GetComponents<Collider>();
            renderers = GetComponents<Renderer>();
            TryGetComponent(out obstacle);
            if (structure.World == null)
            {
                return;
            }

            float hp = structure.World.Database.WorldConstants.MaterialHp(material);
            if (hp <= 0f)
            {
                Debug.LogError($"{name}: the game data has no HP for {material}. Re-import the design CSVs.", this);
                return;
            }

            structure.Configure(hp, 0f, 0f, true);
        }

        private void OnEnable()
        {
            structure.OnBroken += Apply;
        }

        private void OnDisable()
        {
            structure.OnBroken -= Apply;
        }

        private void Apply()
        {
            bool present = isSolid && !IsBroken;
            foreach (Collider part in colliders)
            {
                part.enabled = present;
            }

            foreach (Renderer visual in renderers)
            {
                visual.enabled = present;
            }

            if (obstacle != null)
            {
                obstacle.enabled = present;
            }
        }
    }
}
