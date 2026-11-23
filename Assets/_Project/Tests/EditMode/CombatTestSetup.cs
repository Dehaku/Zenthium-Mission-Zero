using Zenthium.Combat;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Builds entities and the damage pipeline from the first-pass values in constants.csv, written out
    /// so the tests don't read game data.
    /// </summary>
    internal static class CombatTestSetup
    {
        /// <summary>A roll that never dodges and never makes a Defiance chance below 99% proc.</summary>
        public const float RollThatMisses = 0.99f;

        public static EntityRules CreateRules()
        {
            return new EntityRules(
                new FormulaConstants(
                    hpPerToughness: 10f,
                    leaderHpMultiplier: 2f,
                    armorPerToughness: 0.5f,
                    normalDamageBase: 10f,
                    normalDamagePerStat: 2f,
                    kickCooldown: 3f,
                    minDamageFraction: 0.05f,
                    dodgePerFinesse: 0.005f,
                    dodgeCap: 0.75f,
                    visionRange: 100f,
                    awarenessInstantRange: 10f,
                    awarenessMaxTime: 10f,
                    resourceSecondsPerPoint: 1f,
                    enemyDefaultDamageTier: new TierValue("Light", 0.5f)),
                new EntityConstants(
                    baseStat: 10f,
                    baseMoveSpeed: 6f,
                    defianceChancePerWill: 0.001f,
                    defianceRollCost: 1f,
                    defianceInvulnerablePlayer: 5f,
                    defianceInvulnerableOther: 1f,
                    resourceRegenPerSecond: 1f),
                new RankScalingConstants(
                    innateBoostGoon: 0f,
                    innateBoostChampion: 1f,
                    innateBoostLeader: 2f,
                    statBonusPerRank: 0.2f,
                    hpDamageBonusPerRank: 0.1f));
        }

        public static Entity CreateEntity(
            Team team, StatBlock stats, RankTier? rankTier = null, int difficultyLevel = 0, float roll = RollThatMisses)
        {
            return new Entity(
                new EntitySetup(team, stats, rankTier, difficultyLevel), CreateRules(), new FixedRandom(roll));
        }

        /// <summary>A rank D entity with 10 in every stat: 100 HP, 5 Armor, 5% dodge, 1% Defiance.</summary>
        public static Entity CreateEntity(Team team, RankTier? rankTier = null, int difficultyLevel = 0)
        {
            return CreateEntity(team, StatBlock.Uniform(10f), rankTier, difficultyLevel);
        }

        public static DamagePipeline CreatePipeline(float roll = RollThatMisses, FriendlyFireRules friendlyFire = null)
        {
            return new DamagePipeline(
                CreateRules().Formula, friendlyFire ?? new FriendlyFireRules(), new FixedRandom(roll));
        }

        /// <summary>Damage with no source, so it can't be dodged and friendly fire allows it.</summary>
        public static Hit Environmental(float damage, bool isDamageOverTime = false)
        {
            return new Hit(null, damage, DamageType.Physical, isDamageOverTime);
        }
    }

    /// <summary>Every roll returns the same value.</summary>
    internal sealed class FixedRandom : IRandomSource
    {
        private readonly float value;

        public FixedRandom(float value)
        {
            this.value = value;
        }

        public float Value01()
        {
            return value;
        }
    }
}
