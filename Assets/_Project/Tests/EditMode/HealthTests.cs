using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Max HP from Toughness (balance.md section 1), how HP follows Toughness changes (stats.md), and healing.
    /// </summary>
    public class HealthTests
    {
        private const float Tolerance = 0.0001f;

        [TestCase(RankTier.Goon, 0, 100f, TestName = "Max_RankDGoon_Is100")]
        [TestCase(RankTier.Leader, 0, 600f, TestName = "Max_RankDLeader_DoubledForLeaders_Is600")]
        [TestCase(RankTier.Goon, 2, 168f, TestName = "Max_RankBGoon_StatAndHpBonuses_Is168")]
        public void Max_FromToughnessLeaderAndRank(RankTier tier, int difficultyLevel, float expected)
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Enemy, tier, difficultyLevel);

            Assert.That(entity.Health.Max, Is.EqualTo(expected).Within(Tolerance));
            Assert.That(entity.Health.Current, Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void GainingToughness_RaisesCurrentHpByTheSameAmount()
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Player);
            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(35f), entity);

            entity.Stats.SetBase(Stat.Toughness, 20f);

            Assert.That(entity.Health.Max, Is.EqualTo(200f).Within(Tolerance));
            Assert.That(entity.Health.Current, Is.EqualTo(170f).Within(Tolerance));
        }

        [TestCase(150f, 50f, TestName = "LosingToughness_BelowNewMax_CurrentHpUnchanged")]
        [TestCase(10f, 100f, TestName = "LosingToughness_AboveNewMax_CurrentHpCappedAtMax")]
        public void LosingToughness_DoesNotLowerCurrentHp(float damageDealt, float expected)
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Player, StatBlock.Uniform(20f));
            // Toughness 20: 200 HP and 10 Armor, so a hit deals 10 less than its damage.
            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(damageDealt + 10f), entity);

            entity.Stats.SetBase(Stat.Toughness, 10f);

            Assert.That(entity.Health.Current, Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void Heal_StopsAtMax()
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Player);
            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(35f), entity);

            float healed = entity.Heal(50f);

            Assert.That(healed, Is.EqualTo(30f).Within(Tolerance));
            Assert.That(entity.Health.Current, Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void Heal_DoesNotReviveAnIncapacitatedEntity()
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(1000f), entity);

            entity.Heal(50f);

            Assert.That(entity.IsIncapacitated, Is.True);
            Assert.That(entity.Health.Current, Is.EqualTo(0f));
        }
    }
}
