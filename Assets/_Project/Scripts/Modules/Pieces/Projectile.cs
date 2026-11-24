using UnityEngine;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// A projectile flying on the XZ plane. It sweeps ahead each frame instead of using physics, so it can't tunnel.
    /// It passes through entities its shooter can't hurt, stops at the first one it can or at anything solid, and
    /// falls after its module's range. Time Slow slows it; under Time Stop it freezes and has no collision, so the
    /// player can walk through it (time.md).
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private const int MaxSweepHits = 16;

        [Tooltip("Meters per second. A feel value until projectile modules carry their own speed.")]
        [SerializeField, Min(0.1f)] private float speed = 15f;

        [Tooltip("Meters. The size of the sweep that finds what it hits.")]
        [SerializeField, Min(0.01f)] private float radius = 0.2f;

        private readonly RaycastHit[] sweepHits = new RaycastHit[MaxSweepHits];
        private ModuleContext context;
        private HitEffects effects;
        private Vector3 direction;
        private float remainingRange;

        public void Launch(ModuleContext shooter, HitEffects hitEffects, Vector3 flightDirection, float range)
        {
            context = shooter;
            effects = hitEffects;
            flightDirection.y = 0f;
            direction = flightDirection.sqrMagnitude > 0f ? flightDirection.normalized : Vector3.forward;
            remainingRange = range;
        }

        private void Update()
        {
            if (context == null)
            {
                return;
            }

            // Projectiles aren't exempt from time powers, whoever fired them.
            float scale = context.World.Clock.Clock.VelocityScale(false);
            if (scale <= 0f)
            {
                return;
            }

            float step = Mathf.Min(speed * scale * Time.deltaTime, remainingRange);
            if (step <= 0f)
            {
                return;
            }

            if (TryFindHit(step, out EntityBehaviour target))
            {
                if (target != null)
                {
                    effects.ApplyTo(context, target, direction);
                }

                Destroy(gameObject);
                return;
            }

            transform.position += direction * step;
            remainingRange -= step;
            if (remainingRange <= 0f)
            {
                Destroy(gameObject);
            }
        }

        /// <param name="target">The entity hit, or null if something solid stopped it.</param>
        /// <returns>True if anything stops it within the step.</returns>
        private bool TryFindHit(float step, out EntityBehaviour target)
        {
            target = null;
            int count = Physics.SphereCastNonAlloc(transform.position, radius, direction, sweepHits, step,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = sweepHits[i];
                if (hit.distance >= nearest || hit.collider.attachedRigidbody == context.Body)
                {
                    continue;
                }

                if (ModuleWorld.TryGetEntity(hit.collider, out EntityBehaviour entity))
                {
                    if (!CanHit(entity))
                    {
                        continue;
                    }

                    target = entity;
                }
                else
                {
                    target = null;
                }

                nearest = hit.distance;
                found = true;
            }

            return found;
        }

        private bool CanHit(EntityBehaviour entity)
        {
            Entity victim = entity.Entity;
            return victim != null && !victim.IsIncapacitated && !victim.IsBeingDragged
                && context.World.CombatRules.FriendlyFire.CanHurt(context.Owner, victim);
        }
    }
}
