using NUnit.Framework;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// combat.md: Armor, then Damage Reduction (sources multiply), the 5% minimum, DoTs ignoring both,
    /// dodge, and the source's rank damage bonus.
    /// </summary>
    public class DamagePipelineTests
    {
        private const float Tolerance = 0.0001f;

        /// <summary>Toughness 20: 200 HP and 10 Armor.</summary>
        private static Entity CreateToughTarget()
        {
            return CombatTestSetup.CreateEntity(Team.Enemy, StatBlock.Uniform(20f));
        }

        [Test]
        public void Apply_WorkedExample_100DamageAgainst10ArmorAnd30Percent_Deals63()
        {
            Entity target = CreateToughTarget();
            target.Defenses.AddDamageReduction(0.3f);

            DamageEvent result = CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(100f), target);

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Dealt));
            Assert.That(result.Amount, Is.EqualTo(63f).Within(Tolerance));
            Assert.That(target.Health.Current, Is.EqualTo(137f).Within(Tolerance));
        }

        [Test]
        public void DamageReduction_TwoFiftyPercentSources_Give75Percent()
        {
            Entity target = CreateToughTarget();

            target.Defenses.AddDamageReduction(0.5f);
            target.Defenses.AddDamageReduction(0.5f);

            Assert.That(target.Defenses.DamageReduction, Is.EqualTo(0.75f).Within(Tolerance));
        }

        [TestCase(8f, 0f, 0.4f, TestName = "Apply_ArmorAboveDamage_Deals5Percent")]
        [TestCase(100f, 0.99f, 5f, TestName = "Apply_ReductionBelow5Percent_Deals5Percent")]
        [TestCase(100f, 0.9f, 9f, TestName = "Apply_AboveTheMinimum_DealsTheReducedDamage")]
        public void Apply_DealsAtLeast5PercentOfTheHit(float damage, float reduction, float expected)
        {
            Entity target = CreateToughTarget();
            target.Defenses.AddDamageReduction(reduction);

            DamageEvent result = CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(damage), target);

            Assert.That(result.Amount, Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void Apply_DamageOverTime_IgnoresArmorAndDamageReduction()
        {
            Entity target = CreateToughTarget();
            target.Defenses.AddDamageReduction(0.5f);

            DamageEvent result = CombatTestSetup.CreatePipeline()
                .Apply(CombatTestSetup.Environmental(7f, isDamageOverTime: true), target);

            Assert.That(result.Amount, Is.EqualTo(7f).Within(Tolerance));
        }

        [TestCase(0.05f, DamageOutcome.Dodged, 200f, TestName = "Apply_RollBelowDodgeChance_Dodges")]
        [TestCase(0.5f, DamageOutcome.Dealt, 160f, TestName = "Apply_RollAboveDodgeChance_Hits")]
        public void Apply_DirectHit_RollsDodge(float roll, DamageOutcome expectedOutcome, float expectedHp)
        {
            // Finesse 20: 10% dodge.
            Entity target = CreateToughTarget();
            Entity source = CombatTestSetup.CreateEntity(Team.Player);

            DamageEvent result = CombatTestSetup.CreatePipeline(roll)
                .Apply(new Hit(source, 50f, DamageType.Physical), target);

            Assert.That(result.Outcome, Is.EqualTo(expectedOutcome));
            Assert.That(target.Health.Current, Is.EqualTo(expectedHp).Within(Tolerance));
        }

        [Test]
        public void Apply_DamageOverTime_IsNeverDodged()
        {
            Entity target = CreateToughTarget();
            Entity source = CombatTestSetup.CreateEntity(Team.Player);

            DamageEvent result = CombatTestSetup.CreatePipeline(roll: 0f)
                .Apply(new Hit(source, 5f, DamageType.Magic, isDamageOverTime: true), target);

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Dealt));
        }

        [Test]
        public void Apply_EnvironmentalDamage_IsNeverDodged()
        {
            Entity target = CreateToughTarget();

            DamageEvent result = CombatTestSetup.CreatePipeline(roll: 0f)
                .Apply(CombatTestSetup.Environmental(50f), target);

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Dealt));
        }

        [TestCase(0, 90f, TestName = "Apply_RankDSource_NoDamageBonus")]
        [TestCase(2, 110f, TestName = "Apply_RankBSource_Plus20PercentBeforeArmor")]
        public void Apply_AddsTheSourcesRankDamageBonus(int sourceDifficultyLevel, float expected)
        {
            Entity target = CombatTestSetup.CreateEntity(Team.Player, StatBlock.Uniform(20f));
            Entity source = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon, sourceDifficultyLevel);

            DamageEvent result = CombatTestSetup.CreatePipeline()
                .Apply(new Hit(source, 100f, DamageType.Energy), target);

            Assert.That(result.Amount, Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void Apply_IncapacitatedTarget_IsNotAllowedAndRaisesNothing()
        {
            Entity target = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            DamagePipeline pipeline = CombatTestSetup.CreatePipeline();
            pipeline.Apply(CombatTestSetup.Environmental(1000f), target);
            int eventsRaised = 0;
            target.OnDamaged += _ => eventsRaised++;

            DamageEvent result = pipeline.Apply(CombatTestSetup.Environmental(10f), target);

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.NotAllowed));
            Assert.That(eventsRaised, Is.EqualTo(0));
        }

        [Test]
        public void Apply_FriendlyFireNotAllowed_LeavesHpUnchanged()
        {
            Entity target = CombatTestSetup.CreateEntity(Team.Ally);
            Entity source = CombatTestSetup.CreateEntity(Team.Player);

            DamageEvent result = CombatTestSetup.CreatePipeline()
                .Apply(new Hit(source, 50f, DamageType.Physical), target);

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.NotAllowed));
            Assert.That(target.Health.Current, Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void Apply_DraggedTarget_IsBlocked()
        {
            Entity target = CombatTestSetup.CreateEntity(Team.Civilian);
            target.SetDraggedBy(CombatTestSetup.CreateEntity(Team.Player));

            DamageEvent result = CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(50f), target);

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Blocked));
            Assert.That(target.Health.Current, Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void Apply_ReleasedTarget_TakesDamageAgain()
        {
            Entity target = CombatTestSetup.CreateEntity(Team.Civilian);
            target.SetDraggedBy(CombatTestSetup.CreateEntity(Team.Player));
            target.SetDraggedBy(null);

            DamageEvent result = CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(50f), target);

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Dealt));
            Assert.That(target.Health.Current, Is.EqualTo(55f).Within(Tolerance));
        }
    }
}
