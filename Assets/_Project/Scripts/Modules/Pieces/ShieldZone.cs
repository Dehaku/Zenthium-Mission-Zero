using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Movement;

namespace Zenthium.Modules
{
    /// <summary>
    /// A raised shield (Energy Shield, Barrier Shield): it stays in front of its user's aim for its duration, on the
    /// user's game time. Projectiles from anyone who could hurt the user stop at it, and opponents inside its arc
    /// are knocked back. Its visual is the prefab's children.
    /// </summary>
    public sealed class ShieldZone : MonoBehaviour
    {
        [Tooltip("Meters from the user's center the shield reaches. A feel value until shields carry their own size.")]
        [SerializeField, Min(0.1f)] private float radius = 1.5f;

        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private ModuleContext context;
        private TierValue knockbackTier;
        private float halfAngle;
        private float remaining;

        public void Raise(ModuleContext user, TierValue knockback, float seconds, float halfAngleDegrees)
        {
            context = user;
            knockbackTier = knockback;
            remaining = seconds;
            halfAngle = halfAngleDegrees;
            context.World.RegisterShield(this);
            Follow();
        }

        /// <summary>Whether a projectile from this shooter at this point is stopped.</summary>
        public bool Blocks(Vector3 point, Entity shooter)
        {
            if (context == null || !context.World.CombatRules.FriendlyFire.CanHurt(shooter, context.Owner))
            {
                return false;
            }

            Vector3 offset = point - context.Position;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius
                && MeleeArc.Contains(context.Position, context.Aim.Direction, point, halfAngle);
        }

        private void Update()
        {
            if (context == null)
            {
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            remaining -= context.World.Clock.Clock.GameDelta(realDelta, context.Owner.IsExemptFromTimePowers);
            if (remaining <= 0f || context.Owner.IsIncapacitated || context.Body == null)
            {
                Destroy(gameObject);
                return;
            }

            Follow();
            PushOpponents();
        }

        private void OnDestroy()
        {
            if (context != null && context.World != null)
            {
                context.World.UnregisterShield(this);
            }
        }

        private void Follow()
        {
            transform.position = context.Position;
            Vector3 aim = context.Aim.Direction;
            if (aim.sqrMagnitude > 0f)
            {
                transform.rotation = Quaternion.LookRotation(aim, Vector3.up);
            }
        }

        private void PushOpponents()
        {
            Vector3 origin = context.Position;
            context.World.CollectEntities(origin, radius, reached);
            foreach (EntityBehaviour target in reached)
            {
                Entity entity = target.Entity;
                if (!Opponents.Are(context.Owner, entity) || entity.IsIncapacitated
                    || !MeleeArc.Contains(origin, context.Aim.Direction, target.transform.position, halfAngle)
                    || !target.TryGetComponent(out EntityMotor motor) || motor.IsKnockedBack)
                {
                    continue;
                }

                // The resolver needs a hit to judge, so it judges a push from the user landing on the target.
                var push = new DamageEvent(context.Owner, entity, DamageType.Physical, false, DamageOutcome.Dealt, 0f);
                if (context.World.CombatRules.Knockback.TryGetDistance(knockbackTier, push, out float distance))
                {
                    motor.ApplyKnockback(MeleeStrikeModule.Away(origin, target, context.Aim.Direction), distance);
                }
            }
        }
    }
}
