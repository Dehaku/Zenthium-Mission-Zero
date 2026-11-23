using UnityEngine;

namespace Zenthium.Movement
{
    /// <summary>
    /// A knocked-back entity ran into something solid: a wall, a prop or another entity. Milestone 5 uses it
    /// for wall damage (combat.md, Knockback; world.md).
    /// </summary>
    public readonly struct KnockbackObstacleHit
    {
        public KnockbackObstacleHit(Collider obstacle, Vector3 point, float unusedDistance)
        {
            Obstacle = obstacle;
            Point = point;
            UnusedDistance = unusedDistance;
        }

        public Collider Obstacle { get; }
        public Vector3 Point { get; }

        /// <summary>Meters of knockback the obstacle cut off.</summary>
        public float UnusedDistance { get; }
    }
}
