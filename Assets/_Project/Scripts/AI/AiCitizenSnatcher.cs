using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.AI
{
    /// <summary>
    /// An AI entity's citizen snatching (enemies.md, Dragging citizens): one whose module list has Drag finds the
    /// nearest citizen it can see and drags it. <see cref="AiBrain"/> decides when, walks it there and takes the
    /// citizen to its <c>LootDestination</c>.
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour), typeof(EntityModules), typeof(DragAction))]
    public sealed class AiCitizenSnatcher : MonoBehaviour
    {
        private readonly List<IDragTarget> released = new List<IDragTarget>();
        private EntityBehaviour self;
        private EntityModules modules;
        private DragAction drag;

        /// <summary>Only an entity whose data lists Drag drags citizens; others with the component don't.</summary>
        public bool HasDrag
        {
            get
            {
                EntityData data = self.FactionEntity;
                return data != null && modules.World != null
                    && modules.World.Database.Modules.TryGet(DataIds.Modules.Drag, out ModuleData dragData)
                    && data.Lists(dragData);
            }
        }

        public bool IsDraggingCitizen => drag.IsDraggingCitizen;

        /// <summary>Meters from a citizen it grabs it from.</summary>
        public float Reach => drag.Reach;

        /// <summary>The nearest citizen it can see within range that Drag could grab; null if none.</summary>
        /// <param name="everyone">Every entity on the map, as the brain last looked.</param>
        /// <param name="sightHits">Reused for the sight raycasts.</param>
        internal EntityBehaviour FindVisibleCitizen(IReadOnlyList<EntityBehaviour> everyone, Vector3 eye, float range,
            RaycastHit[] sightHits)
        {
            Entity carrier = self.Entity;
            if (carrier == null || modules.World == null)
            {
                return null;
            }

            EntityBehaviour nearest = null;
            float nearestDistance = range;
            for (int i = 0; i < everyone.Count; i++)
            {
                EntityBehaviour candidate = everyone[i];
                Entity citizen = candidate != null ? candidate.Entity : null;
                if (citizen == null || citizen.Team != Team.Civilian || citizen.IsAbducted
                    || !candidate.TryGetComponent(out EntityDragTarget target) || !target.CanBeGrabbedBy(carrier))
                {
                    continue;
                }

                Vector3 position = candidate.transform.position;
                float x = position.x - eye.x;
                float z = position.z - eye.z;
                float distance = Mathf.Sqrt(x * x + z * z);

                // Nearest first, so a sight raycast is only spent on a citizen that would be the new nearest.
                if (distance > nearestDistance || !modules.World.CanSee(eye, position, sightHits))
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            return nearest;
        }

        /// <summary>Grabs what's best in reach; Drag picks a citizen first (modules.md, Drag row).</summary>
        internal void Grab()
        {
            if (!drag.IsDraggingCitizen)
            {
                drag.Use();
            }
        }

        /// <summary>Leaving the map: the citizens it drags go with it, for good.</summary>
        internal void TakeCitizensOffMap()
        {
            drag.ReleaseAll(released);
            foreach (IDragTarget target in released)
            {
                if (target.Kind == DragTargetKind.Citizen && target is Component component
                    && component.TryGetComponent(out EntityBehaviour citizen) && citizen.Entity != null
                    && citizen.Entity.Abduct())
                {
                    citizen.gameObject.SetActive(false);
                }
            }

            released.Clear();
        }

        private void Awake()
        {
            self = GetComponent<EntityBehaviour>();
            modules = GetComponent<EntityModules>();
            drag = GetComponent<DragAction>();
        }
    }
}
