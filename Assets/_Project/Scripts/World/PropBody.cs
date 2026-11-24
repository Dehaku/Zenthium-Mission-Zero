using System;
using UnityEngine;
using UnityEngine.AI;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Movement;

namespace Zenthium.World
{
    /// <summary>
    /// A prop: a car, dumpster, bench and so on (world.md, Props), with its row of props.csv. It blocks movement and
    /// sight, and is a kinematic body nothing can shove.
    /// <list type="bullet">
    /// <item>Drag grabs it if the carrier has the Strength (it uses capacity); dragged, it can't be hurt.</item>
    /// <item>Thrown (Kick or Power Throw), it flies like a knockback, sweeping ahead so it can't tunnel. It hits
    /// the first entity the thrower can hurt or anything solid, for the thrower's Kick damage times its factor,
    /// and takes the same damage itself. Walls, doors and windows take the throw multiplier.</item>
    /// <item>Small props always break when thrown.</item>
    /// <item>Broken, it's gone: no collider, visual or NavMesh carving. A vehicle's explosion is
    /// <see cref="VehicleExplosion"/>'s.</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(Structure), typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class PropBody : MonoBehaviour, IDragTarget, IThrowable
    {
        private const int MaxSweepHits = 16;

        [SerializeField] private PropData data;

        [Tooltip("How fast a dragged prop closes the gap to its place behind the carrier: the fraction of the gap per second.")]
        [SerializeField, Min(0.1f)] private float carryFollowRate = 12f;

        [Tooltip("Meters per second a thrown prop flies. The distance comes from the throw.")]
        [SerializeField, Min(0.1f)] private float throwSpeed = 20f;

        [Tooltip("Meters between the thrower's center and the prop's near side when it's thrown.")]
        [SerializeField, Min(0f)] private float throwClearance = 1.2f;

        [Tooltip("Meters kept between a flying prop and what stops it. Also keeps the sweep off the ground.")]
        [SerializeField, Min(0.001f)] private float sweepSkin = 0.05f;

        private readonly RaycastHit[] sweepHits = new RaycastHit[MaxSweepHits];
        private readonly KnockbackMotion flight = new KnockbackMotion();
        private Structure structure;
        private Rigidbody body;
        private BoxCollider box;
        private NavMeshObstacle obstacle;
        private Renderer[] renderers = new Renderer[0];
        private Entity carrier;
        private Vector3 carryPoint;
        private Entity thrower;
        private Rigidbody throwerBody;
        private Vector3 flightDirection;
        private float thrownDamage;

        public event Action<IDragTarget> OnDragBroken;

        public PropData Data => data;
        public Structure Structure => structure;
        public bool IsFlying => flight.IsActive;

        public DragTargetKind Kind => DragTargetKind.Prop;
        public Rigidbody Body => body;
        public bool UsesCapacity => true;
        public float CarrierSlow => 0f;
        public float GrabSeconds => 0f;

        public bool CanBeGrabbedBy(Entity newCarrier)
        {
            return isActiveAndEnabled && data != null && newCarrier != null && carrier == null && !flight.IsActive
                && structure.CanBeHit && PropRules.CanLift(newCarrier.Stats.Get(Stat.Strength), data);
        }

        public void BeginDrag(Entity newCarrier)
        {
            carrier = newCarrier;
            carryPoint = body.position;
            structure.IsProtected = true;
        }

        public void FollowTo(Vector3 point)
        {
            carryPoint = point;
        }

        public void EndDrag()
        {
            carrier = null;
            structure.IsProtected = false;
        }

        public void Throw(in ThrowInfo info)
        {
            Vector3 direction = info.Direction;
            direction.y = 0f;
            if (data == null || structure.IsBroken || direction.sqrMagnitude <= 0f)
            {
                return;
            }

            direction.Normalize();
            thrower = info.Thrower;
            throwerBody = null;
            if (structure.World != null && structure.World.TryGetBehaviour(thrower, out EntityBehaviour throwerBehaviour))
            {
                throwerBehaviour.TryGetComponent(out throwerBody);
            }

            thrownDamage = PropRules.ThrownDamage(info.KickDamage, data);
            Vector3 start = info.Origin + direction * (throwClearance + ExtentAlong(direction));
            start.y = body.position.y;
            body.position = start;
            transform.position = start;
            flightDirection = direction;
            flight.Start(info.Distance);
        }

        private void Awake()
        {
            structure = GetComponent<Structure>();
            body = GetComponent<Rigidbody>();
            box = GetComponent<BoxCollider>();
            TryGetComponent(out obstacle);
            renderers = GetComponents<Renderer>();

            // Kinematic: entities can't shove props; only Drag and throws move them.
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (data == null)
            {
                Debug.LogError($"{name}: {nameof(PropBody)} needs its prop data.", this);
                enabled = false;
                return;
            }

            structure.Configure(data.Hp, data.Armor, data.DamageReduction, false);
        }

        private void OnEnable()
        {
            structure.OnBroken += ShowBroken;
        }

        private void OnDisable()
        {
            structure.OnBroken -= ShowBroken;
            BreakDrag();
            flight.Cancel();
        }

        private void FixedUpdate()
        {
            if (carrier != null)
            {
                Vector3 offset = carryPoint - body.position;
                offset.y = 0f;
                body.MovePosition(body.position + offset * Mathf.Min(1f, carryFollowRate * Time.fixedDeltaTime));
            }
            else if (flight.IsActive)
            {
                Fly();
            }
        }

        private void Fly()
        {
            ModuleWorld world = structure.World;
            if (world == null)
            {
                flight.Cancel();
                return;
            }

            // Thrown props aren't exempt from time powers, whoever threw them.
            float travel = throwSpeed * world.Clock.Clock.VelocityScale(false) * Time.fixedDeltaTime;
            if (travel <= 0f)
            {
                return;
            }

            float clear = ClearDistance(world, travel + sweepSkin, out Collider obstacleHit);
            KnockbackStep step = flight.Step(travel, clear);
            body.MovePosition(body.position + flightDirection * step.Distance);
            if (step.HitObstacle)
            {
                HitSomething(world, obstacleHit);
            }

            if (!flight.IsActive)
            {
                Land();
            }
        }

        private void HitSomething(ModuleWorld world, Collider other)
        {
            var hit = new Hit(thrower, thrownDamage, DamageType.Physical);
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

            // world.md: "It takes the same damage it deals."
            structure.TakeHit(new StructureHit(hit, StructureHitKind.Thrown));
        }

        private void Land()
        {
            thrower = null;
            throwerBody = null;
            if (data != null && PropRules.BreaksWhenThrown(data))
            {
                structure.Break();
            }
        }

        /// <returns>Meters it can fly before touching something; positive infinity if nothing.</returns>
        private float ClearDistance(ModuleWorld world, float castDistance, out Collider nearest)
        {
            Vector3 scale = transform.lossyScale;

            // Shrunk by the skin, so the sweep doesn't scrape the ground the prop stands on.
            Vector3 halfExtents = Vector3.Scale(box.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y),
                Mathf.Abs(scale.z))) * 0.5f - Vector3.one * sweepSkin;
            halfExtents = Vector3.Max(halfExtents, Vector3.zero);
            Vector3 center = transform.TransformPoint(box.center);
            int count = Physics.BoxCastNonAlloc(center, halfExtents, flightDirection, sweepHits, transform.rotation,
                castDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            float clear = float.PositiveInfinity;
            nearest = null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = sweepHits[i];

                // Colliders already touching at the start report distance 0; it flies on past those.
                if (hit.distance <= 0f || !Blocks(world, hit.collider))
                {
                    continue;
                }

                float free = hit.distance - sweepSkin;
                if (free < clear)
                {
                    clear = free;
                    nearest = hit.collider;
                }
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

        /// <summary>How far the box reaches from its center along a direction on the ground.</summary>
        private float ExtentAlong(Vector3 direction)
        {
            Vector3 half = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
            return Mathf.Abs(Vector3.Dot(transform.right, direction)) * Mathf.Abs(half.x)
                + Mathf.Abs(Vector3.Dot(transform.forward, direction)) * Mathf.Abs(half.z);
        }

        private void ShowBroken()
        {
            BreakDrag();
            flight.Cancel();
            box.enabled = false;
            foreach (Renderer visual in renderers)
            {
                visual.enabled = false;
            }

            if (obstacle != null)
            {
                obstacle.enabled = false;
            }
        }

        private void BreakDrag()
        {
            if (carrier != null)
            {
                OnDragBroken?.Invoke(this);
            }
        }
    }
}
