using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// The <c>mirror</c> rows of constants.csv: innate rank boosts (factions.md) and per-rank scaling (difficulty.md).
    /// </summary>
    [Serializable]
    public sealed class RankScalingConstants
    {
        [SerializeField] private float innateBoostGoon;
        [SerializeField] private float innateBoostChampion;
        [SerializeField] private float innateBoostLeader;
        [SerializeField] private float statBonusPerRank;
        [SerializeField] private float hpDamageBonusPerRank;

        // Unity's serializer needs a parameterless constructor.
        public RankScalingConstants()
        {
        }

        public RankScalingConstants(
            float innateBoostGoon,
            float innateBoostChampion,
            float innateBoostLeader,
            float statBonusPerRank,
            float hpDamageBonusPerRank)
        {
            this.innateBoostGoon = innateBoostGoon;
            this.innateBoostChampion = innateBoostChampion;
            this.innateBoostLeader = innateBoostLeader;
            this.statBonusPerRank = statBonusPerRank;
            this.hpDamageBonusPerRank = hpDamageBonusPerRank;
        }

        /// <summary>Stat bonus added per difficulty rank above D.</summary>
        public float StatBonusPerRank => statBonusPerRank;

        /// <summary>HP and damage bonus added per difficulty rank above D.</summary>
        public float HpDamageBonusPerRank => hpDamageBonusPerRank;

        /// <summary>
        /// The difficulty stat bonus as a fraction. It adds to the innate boost rather than multiplying (stats.md).
        /// </summary>
        /// <param name="difficultyLevel">Ranks above D: 0 at D, 4 at S, 5 at S2.</param>
        public float StatBonus(int difficultyLevel)
        {
            return difficultyLevel * statBonusPerRank;
        }

        /// <summary>The difficulty HP and damage bonus as a fraction.</summary>
        /// <param name="difficultyLevel">Ranks above D: 0 at D, 4 at S, 5 at S2.</param>
        public float HpDamageBonus(int difficultyLevel)
        {
            return difficultyLevel * hpDamageBonusPerRank;
        }

        /// <summary>The innate stat boost of a rank tier, as a fraction (1 is +100%).</summary>
        public float InnateBoost(RankTier tier)
        {
            switch (tier)
            {
                case RankTier.Goon: return innateBoostGoon;
                case RankTier.Champion: return innateBoostChampion;
                case RankTier.Leader: return innateBoostLeader;
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }
    }
}
