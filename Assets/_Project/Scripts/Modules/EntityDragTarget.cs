using System;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Movement;

namespace Zenthium.Modules
{
    /// <summary>
    /// Lets Drag grab this entity while it's Incapacitated, not captured and not Immobile, whatever its team
    /// (modules.md, Drag row), and lets go if it gets back up or is captured. A civilian counts as a citizen, which
    /// Drag picks first, and can be grabbed on its feet too (missions.md, Extraction), so getting up doesn't free it.
    /// While dragged, the entity can't be hurt or knocked back, and its motor follows the carrier.
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
            IsCitizen ? DragTargetKind.Citizen : DragTargetKind.IncapacitatedEntity;
        public Rigidbody Body => body;
        private bool IsCitizen => entity != null && entity.Team == Team.Civilian;
        public bool UsesCapacity => true;
        public float CarrierSlow { get; private set; }

        public bool CanBeGrabbedBy(Entity carrier)
        {
            return isActiveAndEnabled && entity != null && carrier != null && carrier != entity
                && (entity.IsIncapacitated || IsCitizen) && !entity.IsCaptured && !entity.IsBeingDragged
                && !entity.HasTag(EntityTag.Immobile);
        }

        /// <summary>The carrier's slow while dragging it, such as a Stubborn citizen's; set by the citizen's AI.</summary>
        public void SetCarrierSlow(float fraction)
        {
            CarrierSlow = fraction;
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
            entity.OnRevived += HandleRevived;
            entity.OnCaptured += BreakDragIfDragged;
        }

        private void Unbind()
        {
            if (entity != null)
            {
                entity.OnRevived -= HandleRevived;
                entity.OnCaptured -= BreakDragIfDragged;
                entity = null;
            }
        }

        private void HandleRevived()
        {
            if (!IsCitizen)
            {
                BreakDragIfDragged();
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
