using System;

namespace Zenthium.Movement
{
    /// <summary>The result of one <see cref="KnockbackMotion.Step"/>.</summary>
    public readonly struct KnockbackStep
    {
        public KnockbackStep(float distance, bool hitObstacle, float unusedDistance)
        {
            Distance = distance;
            HitObstacle = hitObstacle;
            UnusedDistance = unusedDistance;
        }

        /// <summary>Meters to move this step.</summary>
        public float Distance { get; }

        /// <summary>An obstacle ended the knockback early.</summary>
        public bool HitObstacle { get; }

        /// <summary>Meters the obstacle cut off. 0 unless <see cref="HitObstacle"/>.</summary>
        public float UnusedDistance { get; }
    }

    /// <summary>
    /// One knockback: a fixed distance covered over several steps (combat.md, Knockback). Each step is told how far
    /// the path is clear, and never moves further, so a fast knockback can't tunnel through a wall.
    /// </summary>
    public sealed class KnockbackMotion
    {
        /// <summary>Meters still to travel.</summary>
        public float Remaining { get; private set; }

        public bool IsActive => Remaining > 0f;

        /// <summary>Replaces any knockback in progress.</summary>
        public void Start(float distance)
        {
            if (distance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distance), distance, "Knockback can't be negative.");
            }

            Remaining = distance;
        }

        public void Cancel()
        {
            Remaining = 0f;
        }

        /// <param name="travel">Meters the knockback would cover this step at full speed.</param>
        /// <param name="clearDistance">Meters free of obstacles ahead; positive infinity when nothing is in the way.</param>
        public KnockbackStep Step(float travel, float clearDistance)
        {
            if (!IsActive)
            {
                return default;
            }

            float wanted = Math.Min(Math.Max(0f, travel), Remaining);
            if (clearDistance < wanted)
            {
                float distance = Math.Max(0f, clearDistance);
                float unused = Remaining - distance;
                Remaining = 0f;
                return new KnockbackStep(distance, true, unused);
            }

            Remaining -= wanted;
            return new KnockbackStep(wanted, false, 0f);
        }
    }
}
