using UnityEngine;

namespace Zenthium.Movement
{
    /// <summary>
    /// A knocked-back entity ran into something solid: a wall, a prop or another entity. Impacts with walls and
    /// props deal damage (combat.md, Knockback; world.md).
    /// </summary>
    public readonly struct KnockbackObstacleHit
    {
        public KnockbackObstacleHit(Collider obstacle, Vector3 point, float unusedDistance, KnockbackCause cause)
        {
            Obstacle = obstacle;
            Point = point;
            UnusedDistance = unusedDistance;
            Cause = cause;
        }

        public Collider Obstacle { get; }
        public Vector3 Point { get; }

        /// <summary>Who knocked the entity back.</summary>
        public KnockbackCause Cause { get; }

        /// <summary>Meters of knockback the obstacle cut off.</summary>
        public float UnusedDistance { get; }
    }
}
