using UnityEngine;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// A projectile flying on the XZ plane. It sweeps ahead each frame instead of using physics, so it can't tunnel.
    /// It passes through entities its shooter can't hurt, stops at the first one it can or at anything solid (damaging
    /// a wall or prop it hits), and falls after its module's range. Time Slow slows it; under Time Stop it freezes
    /// and has no collision, so the player can walk through it (time.md).
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

            if (context.World.IsBlockedByShield(transform.position + direction * step, context.Owner))
            {
                Destroy(gameObject);
                return;
            }

            if (SweepHit.TryFindFirst(context, transform.position, direction, radius, step, sweepHits,
                    out EntityBehaviour target, out Collider solid, out _))
            {
                if (target != null)
                {
                    effects.ApplyTo(context, target, direction);
                }
                else if (ModuleWorld.TryGetStructure(solid, out IDamageableStructure structure))
                {
                    effects.ApplyToStructure(context, structure);
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
    }
}
