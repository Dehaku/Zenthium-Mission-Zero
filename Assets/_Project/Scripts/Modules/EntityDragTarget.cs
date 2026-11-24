using System;
using UnityEngine;
using Zenthium.Entities;
using Zenthium.Movement;

namespace Zenthium.Modules
{
    /// <summary>
    /// Lets Drag grab this entity while it's Incapacitated, not captured and not Immobile, whatever its team
    /// (modules.md, Drag row), and lets go if it gets back up or is captured. A civilian counts as a citizen, which Drag picks first;
    /// grabbing citizens who are still on their feet comes in Milestone 6. While dragged, the entity can't be hurt
    /// or knocked back, and its motor follows the carrier.
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour), typeof(EntityMotor), typeof(Rigidbody))]
    public sealed class EntityDragTarget : MonoBehaviour, IDragTarget
    {
        private EntityBehaviour entityBehaviour;
        private EntityMotor motor;
        private Rigidbody body;
        private Entity entity;

        public event Action<IDragTarget> OnDragBroken;

        public DragTargetKind Kind =>
            entity != null && entity.Team == Team.Civilian ? DragTargetKind.Citizen : DragTargetKind.IncapacitatedEntity;
        public Rigidbody Body => body;
        public bool UsesCapacity => true;
        public float CarrierSlow => 0f;
        public float GrabSeconds => 0f;

        public bool CanBeGrabbedBy(Entity carrier)
        {
            return isActiveAndEnabled && entity != null && carrier != null && carrier != entity
                && entity.IsIncapacitated && !entity.IsCaptured && !entity.IsBeingDragged
                && !entity.HasTag(EntityTag.Immobile);
        }

        public void BeginDrag(Entity carrier)
        {
            entity.SetDraggedBy(carrier);
            motor.BeginCarried();
        }

        public void FollowTo(Vector3 point)
        {
            motor.SetCarryPoint(point);
        }

        public void EndDrag()
        {
            if (entity != null)
            {
                entity.SetDraggedBy(null);
            }

            motor.EndCarried();
        }

        private void Awake()
        {
            entityBehaviour = GetComponent<EntityBehaviour>();
            motor = GetComponent<EntityMotor>();
            body = GetComponent<Rigidbody>();
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
            BreakDragIfDragged();
            Unbind();
        }

        private void Bind(Entity newEntity)
        {
            // A reset replaces the entity; the fresh one isn't dragged, so the carrier must let go.
            BreakDragIfDragged();
            Unbind();
            entity = newEntity;
            entity.OnRevived += BreakDragIfDragged;
            entity.OnCaptured += BreakDragIfDragged;
        }

        private void Unbind()
        {
            if (entity != null)
            {
                entity.OnRevived -= BreakDragIfDragged;
                entity.OnCaptured -= BreakDragIfDragged;
                entity = null;
            }
        }

        private void BreakDragIfDragged()
        {
            if (entity != null && entity.IsBeingDragged)
            {
                OnDragBroken?.Invoke(this);
            }
        }
    }
}
