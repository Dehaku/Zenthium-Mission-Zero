using System;
using UnityEngine;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Movement;

namespace Zenthium.World
{
    /// <summary>
    /// A loose Valuable: a generic money bag (world.md, Valuables).
    /// <list type="bullet">
    /// <item>Drag finds it after citizens and before downed entities, and the player picks it up at once; it takes
    /// no Drag capacity.</item>
    /// <item>Launched by Kick, it flies like a knockback, sweeping ahead so it can't tunnel, and deals the
    /// thrower's Kick damage to the first entity the thrower can hurt or anything solid, then lands there. Walls,
    /// doors and windows take the throw multiplier.</item>
    /// <item>It melts if it's in a building when it burns out, and counts as lost.</item>
    /// </list>
    /// Its collider is a trigger, so it never blocks anyone.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class ValuableBag : MonoBehaviour, IDragTarget, IPickup
    {
        private const int MaxSweepHits = 16;

        [SerializeField] private ValuableService service;

        [Tooltip("Meters per second a launched bag flies. The distance comes from the launch.")]
        [SerializeField, Min(0.1f)] private float flySpeed = 15f;

        [Tooltip("Meters. The size of the sweep that finds what a launched bag hits.")]
        [SerializeField, Min(0.01f)] private float sweepRadius = 0.25f;

        private readonly RaycastHit[] sweepHits = new RaycastHit[MaxSweepHits];
        private readonly KnockbackMotion flight = new KnockbackMotion();
        private Rigidbody body;
        private Entity thrower;
        private Rigidbody throwerBody;
        private Vector3 flightDirection;
        private float damage;

        // Bags are picked up (IPickup), never dragged, so nothing ever breaks a drag on one.
        public event Action<IDragTarget> OnDragBroken
        {
            add { }
            remove { }
        }

        public DragTargetKind Kind => DragTargetKind.Valuable;
        public Rigidbody Body => body;
        public bool UsesCapacity => false;
        public float CarrierSlow => 0f;
        public float GrabSeconds => 0f;
        public bool IsFlying => flight.IsActive;

        /// <summary>For bags spawned in code: set before it's used.</summary>
        public void SetService(ValuableService newService)
        {
            service = newService;
        }

        public bool CanBeGrabbedBy(Entity carrier)
        {
            return isActiveAndEnabled && carrier != null && !flight.IsActive;
        }

        public void BeginDrag(Entity carrier)
        {
        }

        public void FollowTo(Vector3 point)
        {
        }

        public void EndDrag()
        {
        }

        public bool PickUp(EntityBehaviour picker)
        {
            if (picker == null || !CanBeGrabbedBy(picker.Entity)
                || !picker.TryGetComponent(out ValuableCarrier carrier))
            {
                return false;
            }

            carrier.Add(1);
            Destroy(gameObject);
            return true;
        }

        /// <param name="kickDamage">Dealt to the first thing it hits.</param>
        public void Launch(Entity launchedBy, Vector3 direction, float distance, float kickDamage)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0f || distance <= 0f)
            {
                return;
            }

            thrower = launchedBy;
            throwerBody = null;
            if (service != null && service.World != null
                && service.World.TryGetBehaviour(thrower, out EntityBehaviour throwerBehaviour))
            {
                throwerBehaviour.TryGetComponent(out throwerBody);
            }

            flightDirection = direction.normalized;
            damage = kickDamage;
            flight.Start(distance);
        }

        /// <summary>It's lost in a fire (world.md, Valuables secondary goal).</summary>
        public void Melt()
        {
            if (service != null)
            {
                service.CountLost(1);
            }

            Destroy(gameObject);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            GetComponent<SphereCollider>().isTrigger = true;
        }

        private void FixedUpdate()
        {
            if (flight.IsActive)
            {
                Fly();
            }
        }

        private void Fly()
        {
            ModuleWorld world = service != null ? service.World : null;
            if (world == null)
            {
                flight.Cancel();
                return;
            }

            // Launched bags aren't exempt from time powers, whoever launched them.
            float travel = flySpeed * world.Clock.Clock.VelocityScale(false) * Time.fixedDeltaTime;
            if (travel <= 0f)
            {
                return;
            }

            float clear = ClearDistance(world, travel, out Collider obstacle);
            KnockbackStep step = flight.Step(travel, clear);
            body.MovePosition(body.position + flightDirection * step.Distance);
            if (step.HitObstacle)
            {
                HitSomething(world, obstacle);
            }
        }

        private void HitSomething(ModuleWorld world, Collider other)
        {
            var hit = new Hit(thrower, damage, DamageType.Physical);
            if (ModuleWorld.TryGetEntity(other, out EntityBehaviour target))
            {
                if (target.Entity != null && world.CombatRules.Pipeline != null)
                {
                    world.CombatRules.Pipeline.Apply(hit, target.Entity);
                }
            }
            else if (ModuleWorld.TryGetStructure(other, out IDamageableStructure solid) && solid.CanBeHit)
            {
                solid.TakeHit(new StructureHit(hit, StructureHitKind.Thrown));
            }
        }

        /// <returns>Meters it can fly before touching something; positive infinity if nothing.</returns>
        private float ClearDistance(ModuleWorld world, float castDistance, out Collider nearest)
        {
            int count = Physics.SphereCastNonAlloc(body.position, sweepRadius, flightDirection, sweepHits, castDistance,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            float clear = float.PositiveInfinity;
            nearest = null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = sweepHits[i];

                // Colliders already touching at the start report distance 0; it flies on past those.
                if (hit.distance <= 0f || hit.distance >= clear || !Blocks(world, hit.collider))
                {
                    continue;
                }

                clear = hit.distance;
                nearest = hit.collider;
            }

            return clear;
        }

        /// <summary>
        /// Like a projectile, it passes through its thrower and entities the thrower can't hurt, Incapacitated or
        /// dragged ones; anything else stops it.
        /// </summary>
        private bool Blocks(ModuleWorld world, Collider other)
        {
            Rigidbody attached = other.attachedRigidbody;
            if (attached == body || (throwerBody != null && attached == throwerBody))
            {
                return false;
            }

            if (!ModuleWorld.TryGetEntity(other, out EntityBehaviour target))
            {
                return true;
            }

            Entity victim = target.Entity;
            return victim != null && !victim.IsIncapacitated && !victim.IsBeingDragged
                && world.CombatRules.FriendlyFire.CanHurt(thrower, victim);
        }
    }
}
