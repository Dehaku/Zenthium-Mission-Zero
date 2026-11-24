using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Drag (modules.md, Drag row): grabs the best target in reach and pulls it behind the carrier. Carry capacity
    /// grows with Strength. The carrier isn't slowed and can still fight. Dragged targets trail one behind another,
    /// like beads on a rope.
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour), typeof(Rigidbody))]
    public sealed class DragAction : MonoBehaviour
    {
        private const int MaxReachHits = 32;

        [SerializeField] private GameDatabase database;

        [Tooltip("Meters between each dragged target and the carrier or target ahead of it.")]
        [SerializeField, Min(0.1f)] private float trailSpacing = 1.5f;

        private readonly Collider[] reachHits = new Collider[MaxReachHits];
        private readonly List<IDragTarget> carried = new List<IDragTarget>();
        private readonly List<IDragTarget> candidates = new List<IDragTarget>();
        private readonly List<DragCandidate> candidateInfo = new List<DragCandidate>();
        private readonly List<Collider> targetColliders = new List<Collider>();
        private Collider[] ownColliders;
        private EntityBehaviour entityBehaviour;
        private Rigidbody body;
        private Entity entity;
        private DragRules rules;
        private float reach;

        public int CarriedCount => carried.Count;

        /// <summary>Lets go of everything and lists what was carried, such as for Power Throw.</summary>
        public void ReleaseAll(List<IDragTarget> released)
        {
            released.Clear();
            released.AddRange(carried);
            ReleaseAll();
        }

        /// <summary>Grabs the best target in reach if there's room; otherwise lets go of everything.</summary>
        public void Use()
        {
            if (entity == null || entity.IsIncapacitated)
            {
                return;
            }

            IDragTarget target = FindTarget();
            if (target != null)
            {
                Grab(target);
            }
            else
            {
                ReleaseAll();
            }
        }

        private void Awake()
        {
            entityBehaviour = GetComponent<EntityBehaviour>();
            body = GetComponent<Rigidbody>();
            ownColliders = GetComponentsInChildren<Collider>();

            if (database == null || !database.Modules.TryGet(DataIds.Modules.Drag, out ModuleData drag)
                || !DragRules.TryCreate(drag, out rules) || !drag.RangeTier.IsSet)
            {
                Debug.LogError($"{name}: {nameof(DragAction)} needs a Game Database with Drag's range and capacity.", this);
                enabled = false;
                return;
            }

            reach = drag.RangeTier.Value;
        }

        private void OnEnable()
        {
            // The entity may run Awake before or after this, so bind now and on every (re)creation.
            entityBehaviour.OnEntityCreated += Bind;
            if (entityBehaviour.Entity != null)
            {
                Bind(entityBehaviour.Entity);
            }
        }

        private void OnDisable()
        {
            entityBehaviour.OnEntityCreated -= Bind;
            Unbind();
        }

        private void FixedUpdate()
        {
            Vector3 ahead = body.position;
            foreach (IDragTarget target in carried)
            {
                Vector3 position = target.Body.position;
                Vector3 offset = position - ahead;
                offset.y = 0f;

                // Only pulled once the gap is wider than the spacing; closer targets stay put, like a slack rope.
                Vector3 point = position;
                if (offset.sqrMagnitude > trailSpacing * trailSpacing)
                {
                    point = ahead + offset.normalized * trailSpacing;
                    point.y = position.y;
                }

                target.FollowTo(point);
                ahead = point;
            }
        }

        private void Bind(Entity newEntity)
        {
            Unbind();
            entity = newEntity;
            entity.OnIncapacitated += ReleaseAll;
        }

        private void Unbind()
        {
            ReleaseAll();
            if (entity != null)
            {
                entity.OnIncapacitated -= ReleaseAll;
                entity = null;
            }
        }

        private IDragTarget FindTarget()
        {
            CountCarried(out int carriedCitizens, out int carriedOthers);
            int capacityLeft = DragRules.CapacityLeft(rules.Capacity(entity.Stats.Get(Stat.Strength)),
                DragRules.CitizenCapacity(entity.Slots), carriedCitizens, carriedOthers, out int citizenCapacityLeft);
            candidates.Clear();
            candidateInfo.Clear();

            Vector3 origin = body.position;
            int count = Physics.OverlapSphereNonAlloc(
                origin, reach, reachHits, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!TryGetDragTarget(reachHits[i], out IDragTarget target) || candidates.Contains(target)
                    || carried.Contains(target) || !target.CanBeGrabbedBy(entity))
                {
                    continue;
                }

                Vector3 offset = target.Body.position - origin;
                offset.y = 0f;
                candidates.Add(target);
                candidateInfo.Add(new DragCandidate(target.Kind, offset.magnitude, target.UsesCapacity));
            }

            int index = DragRules.SelectTarget(candidateInfo, capacityLeft, citizenCapacityLeft);
            return index < 0 ? null : candidates[index];
        }

        /// <summary>Carried targets that use capacity: citizens, which can also use citizen-only capacity, and the rest.</summary>
        private void CountCarried(out int citizens, out int others)
        {
            citizens = 0;
            others = 0;
            foreach (IDragTarget target in carried)
            {
                if (!target.UsesCapacity)
                {
                    continue;
                }

                if (target.Kind == DragTargetKind.Citizen)
                {
                    citizens++;
                }
                else
                {
                    others++;
                }
            }
        }

        private void Grab(IDragTarget target)
        {
            target.BeginDrag(entity);
            target.OnDragBroken += Release;
            carried.Add(target);
            SetCollisionIgnored(target, true);
        }

        private void ReleaseAll()
        {
            for (int i = carried.Count - 1; i >= 0; i--)
            {
                Release(carried[i]);
            }
        }

        private void Release(IDragTarget target)
        {
            if (!carried.Remove(target))
            {
                return;
            }

            target.OnDragBroken -= Release;
            SetCollisionIgnored(target, false);
            target.EndDrag();
        }

        /// <summary>The carrier and what it drags pass through each other, so the trail doesn't shove the carrier.</summary>
        private void SetCollisionIgnored(IDragTarget target, bool ignored)
        {
            if (target.Body == null)
            {
                return;
            }

            target.Body.GetComponentsInChildren(targetColliders);
            foreach (Collider own in ownColliders)
            {
                foreach (Collider other in targetColliders)
                {
                    Physics.IgnoreCollision(own, other, ignored);
                }
            }
        }

        private static bool TryGetDragTarget(Collider hit, out IDragTarget target)
        {
            Rigidbody attached = hit.attachedRigidbody;
            if (attached != null)
            {
                return attached.TryGetComponent(out target);
            }

            return hit.TryGetComponent(out target);
        }
    }
}
