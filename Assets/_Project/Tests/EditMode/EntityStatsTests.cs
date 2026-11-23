using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Stat stacking (stats.md): percentage boosts multiply, except the difficulty bonus, which adds to the
    /// innate rank boost.
    /// </summary>
    public class EntityStatsTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Get_ChadsStrength_InnateAndSuperStatsMultiplyTo90()
        {
            // stats.md: 10 Strength x3 (innate +200%, a Leader) x3 (Super Stats, +200%) = 90.
            Entity chad = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Leader);

            chad.Stats.AddBoost(Stat.Strength, 2f);

            Assert.That(chad.Stats.Get(Stat.Strength), Is.EqualTo(90f).Within(Tolerance));
        }

        [TestCase(RankTier.Goon, 0, 10f, TestName = "Get_RankDGoon_Is10")]
        [TestCase(RankTier.Goon, 1, 12f, TestName = "Get_RankCGoon_Is12")]
        [TestCase(RankTier.Goon, 5, 20f, TestName = "Get_RankS2Goon_Is20")]
        [TestCase(RankTier.Champion, 0, 20f, TestName = "Get_RankDChampion_Is20")]
        [TestCase(RankTier.Leader, 1, 32f, TestName = "Get_RankCLeader_DifficultyAddsToInnate_Is32Not36")]
        public void Get_DifficultyBonusAddsToInnateBoost(RankTier tier, int difficultyLevel, float expected)
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Enemy, tier, difficultyLevel);

            Assert.That(entity.Stats.Get(Stat.Toughness), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void Get_TwoBoosts_Multiply()
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Player);

            entity.Stats.AddBoost(Stat.Finesse, 1f);
            entity.Stats.AddBoost(Stat.Finesse, 0.5f);

            Assert.That(entity.Stats.Get(Stat.Finesse), Is.EqualTo(30f).Within(Tolerance));
        }

        [Test]
        public void RemoveBoost_RestoresTheStat()
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Player);
            ModifierHandle boost = entity.Stats.AddBoost(Stat.Will, 1f);

            entity.Stats.RemoveBoost(Stat.Will, boost);

            Assert.That(entity.Stats.Get(Stat.Will), Is.EqualTo(10f).Within(Tolerance));
        }

        [Test]
        public void MoveSpeed_BonusesAdd()
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Player);

            entity.MoveSpeed.AddBonus(0.5f);
            entity.MoveSpeed.AddBonus(0.5f);

            Assert.That(entity.MoveSpeed.Current, Is.EqualTo(12f).Within(Tolerance));
        }
    }
}
