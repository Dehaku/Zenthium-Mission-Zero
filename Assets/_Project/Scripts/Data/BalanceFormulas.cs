using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// The core formulas of balance.md section 1, and damage by standard tier (section 4).
    /// </summary>
    public sealed class BalanceFormulas
    {
        private readonly FormulaConstants constants;

        public BalanceFormulas(FormulaConstants constants)
        {
            this.constants = constants ?? throw new ArgumentNullException(nameof(constants));
        }

        public float MaxHp(float toughness, bool isLeader)
        {
            float hp = toughness * constants.HpPerToughness;
            return isLeader ? hp * constants.LeaderHpMultiplier : hp;
        }

        public float Armor(float toughness)
        {
            return toughness * constants.ArmorPerToughness;
        }

        /// <param name="stat">The module's stat: Strength for melee, Finesse for ranged, or the module's own.</param>
        public float NormalDamage(float stat)
        {
            return constants.NormalDamageBase + constants.NormalDamagePerStat * stat;
        }

        /// <summary>Normal damage times the damage tier's multiplier (Light, Heavy...).</summary>
        public float TierDamage(float stat, TierValue damageTier)
        {
            return NormalDamage(stat) * damageTier.Value;
        }

        /// <returns>A chance from 0 to 1.</returns>
        public float DodgeChance(float finesse)
        {
            return Mathf.Min(finesse * constants.DodgePerFinesse, constants.DodgeCap);
        }

        /// <summary>
        /// Seconds an enemy's awareness bar takes to fill at default Detectability: instant within
        /// the instant range, the max time at vision range, linear between.
        /// </summary>
        /// <returns>Infinity beyond vision range, where enemies can't see.</returns>
        public float AwarenessFillSeconds(float distance)
        {
            if (distance > constants.VisionRange)
            {
                return float.PositiveInfinity;
            }

            if (distance <= constants.AwarenessInstantRange)
            {
                return 0f;
            }

            float fraction = (distance - constants.AwarenessInstantRange)
                / (constants.VisionRange - constants.AwarenessInstantRange);
            return fraction * constants.AwarenessMaxTime;
        }

        /// <summary>How many seconds of cooldown a resource cost is worth.</summary>
        public float ResourceCostSeconds(float points)
        {
            return points * constants.ResourceSecondsPerPoint;
        }
    }
}
