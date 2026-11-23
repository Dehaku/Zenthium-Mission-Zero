using System;
using UnityEngine;
using Zenthium.Entities;

namespace Zenthium.Movement
{
    /// <summary>
    /// Moves an entity's rigidbody on the XZ plane, and is the only thing that sets its velocity.
    /// One mode runs at a time, in priority order:
    /// <list type="number">
    /// <item>Carried: following a point behind whoever drags it.</item>
    /// <item>Knocked back: a fixed distance, swept ahead each step so it stops at obstacles instead of tunneling.</item>
    /// <item>Walking: the move input at the entity's move speed. Incapacitated entities don't walk.</item>
    /// </list>
    /// Walking and knockback follow Time Slow and Time Stop unless the entity is exempt.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(EntityBehaviour))]
    public sealed class EntityMotor : MonoBehaviour
    {
        private const int MaxObstacleHits = 16;
        private const int CapsuleAlongY = 1;

        [Tooltip("Meters per second a knockback travels. The distance comes from the module's knockback tier.")]
        [SerializeField, Min(0.1f)] private float knockbackSpeed = 20f;

        [Tooltip("How fast a dragged body closes the gap to its place behind the carrier: the fraction of the gap per second.")]
        [SerializeField, Min(0.1f)] private float carryFollowRate = 12f;

        [Tooltip("Meters kept between a knocked-back body and what stops it. Also keeps the sweep off the ground.")]
        [SerializeField, Min(0.001f)] private float obstacleSkin = 0.05f;

        private readonly RaycastHit[] obstacleHits = new RaycastHit[MaxObstacleHits];
        private readonly KnockbackMotion knockback = new KnockbackMotion();
        private Rigidbody body;
        private CapsuleCollider capsule;
        private EntityBehaviour entityBehaviour;
        private Entity entity;
        private Vector3 moveInput;
        private Vector3 knockbackDirection;
        private Vector3 carryPoint;
        private bool isCarried;

        /// <summary>A knockback ended early against something solid.</summary>
        public event Action<KnockbackObstacleHit> OnKnockbackObstacleHit;

        /// <param name="input">Screen-space direction: x is world X, y is world Z. Longer than 1 is clamped.</param>
        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector3.ClampMagnitude(new Vector3(input.x, 0f, input.y), 1f);
        }

        /// <summary>Starts a knockback, replacing any in progress. Ignored while carried.</summary>
        public void ApplyKnockback(Vector3 direction, float distance)
        {
            direction.y = 0f;
            if (isCarried || distance <= 0f || direction.sqrMagnitude <= 0f)
            {
                return;
            }

            knockbackDirection = direction.normalized;
            knockback.Start(distance);
        }

        public void BeginCarried()
        {
            isCarried = true;
            carryPoint = body.position;
            knockback.Cancel();
        }

        /// <summary>Where the carrier wants this body this step.</summary>
        public void SetCarryPoint(Vector3 point)
        {
            carryPoint = point;
        }

        public void EndCarried()
        {
            isCarried = false;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            entityBehaviour = GetComponent<EntityBehaviour>();
            if (capsule.direction != CapsuleAlongY)
            {
                Debug.LogError($"{name}: {nameof(EntityMotor)} needs its capsule collider along the Y axis.", this);
            }
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
            entity = null;
            knockback.Cancel();
            isCarried = false;
        }

        private void FixedUpdate()
        {
            if (entity == null || entityBehaviour.ClockDriver == null)
            {
                return;
            }

            float scale = entityBehaviour.ClockDriver.Clock.VelocityScale(entity.IsExemptFromTimePowers);
            Vector3 velocity;
            if (isCarried)
            {
                // The carrier moves this body, so it keeps up even when Time Slow affects it and not the carrier.
                velocity = Flatten(carryPoint - body.position) * carryFollowRate;
            }
            else if (knockback.IsActive)
            {
                velocity = KnockbackVelocity(scale);
            }
            else if (entity.IsIncapacitated)
            {
                velocity = Vector3.zero;
            }
            else
            {
                velocity = moveInput * (entity.MoveSpeed.Current * scale);
            }

            body.linearVelocity = velocity;
        }

        private void Bind(Entity newEntity)
        {
            entity = newEntity;
            knockback.Cancel();
        }

        private Vector3 KnockbackVelocity(float scale)
        {
            float deltaTime = Time.fixedDeltaTime;
            float travel = knockbackSpeed * scale * deltaTime;
            if (travel <= 0f)
            {
                return Vector3.zero;
            }

            float clearDistance = ClearDistance(knockbackDirection, travel + obstacleSkin, out RaycastHit obstacle);
            KnockbackStep step = knockback.Step(travel, clearDistance);
            if (step.HitObstacle)
            {
                OnKnockbackObstacleHit?.Invoke(new KnockbackObstacleHit(obstacle.collider, obstacle.point, step.UnusedDistance));
            }

            // Covers exactly the step's distance in one physics step.
            return knockbackDirection * (step.Distance / deltaTime);
        }

        /// <returns>Meters this body can move before touching something solid; positive infinity if nothing.</returns>
        private float ClearDistance(Vector3 direction, float castDistance, out RaycastHit nearest)
        {
            Vector3 scale = transform.lossyScale;

            // Shrunk by the skin, so the sweep doesn't scrape the ground the capsule stands on.
            float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) - obstacleSkin;
            float height = Mathf.Max(capsule.height * Mathf.Abs(scale.y), 2f * radius);
            Vector3 center = body.position + body.rotation * Vector3.Scale(capsule.center, scale);
            Vector3 halfSpine = body.rotation * Vector3.up * (height * 0.5f - radius);

            int count = Physics.CapsuleCastNonAlloc(center + halfSpine, center - halfSpine, Mathf.Max(radius, 0f),
                direction, obstacleHits, castDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            float clear = float.PositiveInfinity;
            nearest = default;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = obstacleHits[i];

                // Colliders already touching at the start report distance 0; the physics solver separates those.
                if (hit.distance <= 0f || !Blocks(hit.collider))
                {
                    continue;
                }

                float free = hit.distance - obstacleSkin;
                if (free < clear)
                {
                    clear = free;
                    nearest = hit;
                }
            }

            return clear;
        }

        /// <summary>Anything this body would physically collide with stops a knockback, other entities included.</summary>
        private bool Blocks(Collider other)
        {
            return other.attachedRigidbody != body
                && !Physics.GetIgnoreLayerCollision(gameObject.layer, other.gameObject.layer)
                && !Physics.GetIgnoreCollision(capsule, other);
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }
    }
}
