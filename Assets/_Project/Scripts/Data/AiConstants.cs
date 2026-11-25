using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>constants.csv's <c>ai</c> group (enemies.md, AI).</summary>
    [Serializable]
    public sealed class AiConstants
    {
        [SerializeField] private float alertDelaySeconds;
        [SerializeField] private float alertRadius;
        [SerializeField] private float searchSeconds;
        [SerializeField] private float awarenessDrainSeconds;
        [SerializeField] private float rangedBackoffDistance;
        [SerializeField] private float allyFollowDistance;
        [SerializeField] private float telegraphSeconds;
        [SerializeField] private float patrolSpeedFraction;

        // Unity's serializer needs a parameterless constructor.
        public AiConstants()
        {
        }

        public AiConstants(
            float alertDelaySeconds,
            float alertRadius,
            float searchSeconds,
            float awarenessDrainSeconds,
            float rangedBackoffDistance,
            float allyFollowDistance,
            float telegraphSeconds,
            float patrolSpeedFraction)
        {
            this.alertDelaySeconds = alertDelaySeconds;
            this.alertRadius = alertRadius;
            this.searchSeconds = searchSeconds;
            this.awarenessDrainSeconds = awarenessDrainSeconds;
            this.rangedBackoffDistance = rangedBackoffDistance;
            this.allyFollowDistance = allyFollowDistance;
            this.telegraphSeconds = telegraphSeconds;
            this.patrolSpeedFraction = patrolSpeedFraction;
        }

        /// <summary>Game seconds from becoming aware (the "!") to the alert going out.</summary>
        public float AlertDelaySeconds => alertDelaySeconds;

        /// <summary>Meters around an alerting entity within which its allies become aware.</summary>
        public float AlertRadius => alertRadius;

        /// <summary>Game seconds spent searching around the target's last known position.</summary>
        public float SearchSeconds => searchSeconds;

        /// <summary>Game seconds for a full awareness bar to drain to empty.</summary>
        public float AwarenessDrainSeconds => awarenessDrainSeconds;

        /// <summary>Ranged attackers back off from targets closer than this, in meters.</summary>
        public float RangedBackoffDistance => rangedBackoffDistance;

        /// <summary>Allies outside combat stay within about this many meters of the player.</summary>
        public float AllyFollowDistance => allyFollowDistance;

        /// <summary>Game seconds a Telegraph module flashes before it hits.</summary>
        public float TelegraphSeconds => telegraphSeconds;

        /// <summary>The fraction of their move speed idle squads patrol at.</summary>
        public float PatrolSpeedFraction => patrolSpeedFraction;
    }
}
