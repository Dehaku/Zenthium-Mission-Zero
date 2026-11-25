using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// The <c>formula</c> rows of constants.csv (balance.md section 1).
    /// </summary>
    [Serializable]
    public sealed class FormulaConstants
    {
        [SerializeField] private float hpPerToughness;
        [SerializeField] private float leaderHpMultiplier;
        [SerializeField] private float armorPerToughness;
        [SerializeField] private float normalDamageBase;
        [SerializeField] private float normalDamagePerStat;
        [SerializeField] private float kickCooldown;
        [SerializeField] private float minDamageFraction;
        [SerializeField] private float dodgePerFinesse;
        [SerializeField] private float dodgeCap;
        [SerializeField] private float visionRange;
        [SerializeField] private float awarenessInstantRange;
        [SerializeField] private float awarenessRange;
        [SerializeField] private float awarenessMaxTime;
        [SerializeField] private float resourceSecondsPerPoint;
        [SerializeField] private TierValue enemyDefaultDamageTier;

        // Unity's serializer needs a parameterless constructor.
        public FormulaConstants()
        {
        }

        public FormulaConstants(
            float hpPerToughness,
            float leaderHpMultiplier,
            float armorPerToughness,
            float normalDamageBase,
            float normalDamagePerStat,
            float kickCooldown,
            float minDamageFraction,
            float dodgePerFinesse,
            float dodgeCap,
            float visionRange,
            float awarenessInstantRange,
            float awarenessRange,
            float awarenessMaxTime,
            float resourceSecondsPerPoint,
            TierValue enemyDefaultDamageTier)
        {
            this.hpPerToughness = hpPerToughness;
            this.leaderHpMultiplier = leaderHpMultiplier;
            this.armorPerToughness = armorPerToughness;
            this.normalDamageBase = normalDamageBase;
            this.normalDamagePerStat = normalDamagePerStat;
            this.kickCooldown = kickCooldown;
            this.minDamageFraction = minDamageFraction;
            this.dodgePerFinesse = dodgePerFinesse;
            this.dodgeCap = dodgeCap;
            this.visionRange = visionRange;
            this.awarenessInstantRange = awarenessInstantRange;
            this.awarenessRange = awarenessRange;
            this.awarenessMaxTime = awarenessMaxTime;
            this.resourceSecondsPerPoint = resourceSecondsPerPoint;
            this.enemyDefaultDamageTier = enemyDefaultDamageTier;
        }

        public float HpPerToughness => hpPerToughness;
        public float LeaderHpMultiplier => leaderHpMultiplier;
        public float ArmorPerToughness => armorPerToughness;
        public float NormalDamageBase => normalDamageBase;
        public float NormalDamagePerStat => normalDamagePerStat;

        /// <summary>Seconds. The 1x reference for the power budget.</summary>
        public float KickCooldown => kickCooldown;

        /// <summary>Every hit deals at least this fraction of its damage.</summary>
        public float MinDamageFraction => minDamageFraction;

        public float DodgePerFinesse => dodgePerFinesse;
        public float DodgeCap => dodgeCap;

        /// <summary>Meters an aware entity sees, and Taunt reaches.</summary>
        public float VisionRange => visionRange;

        /// <summary>Meters within which the awareness bar fills at once.</summary>
        public float AwarenessInstantRange => awarenessInstantRange;

        /// <summary>Meters beyond which an unaware entity doesn't notice anyone: its bar doesn't fill.</summary>
        public float AwarenessRange => awarenessRange;

        /// <summary>Seconds to fill the awareness bar at <see cref="AwarenessRange"/>.</summary>
        public float AwarenessMaxTime => awarenessMaxTime;

        public float ResourceSecondsPerPoint => resourceSecondsPerPoint;

        /// <summary>The damage tier enemy attacks use unless their module sets one.</summary>
        public TierValue EnemyDefaultDamageTier => enemyDefaultDamageTier;
    }
}
