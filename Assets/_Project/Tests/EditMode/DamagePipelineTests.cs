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

        [Test]
        public void Apply_SourceDealsLess_AppliesBeforeArmor()
        {
            // Marked: deals 30% less. 100 x 0.7 = 70, minus 5 Armor.
            Entity source = CombatTestSetup.CreateEntity(Team.Player);
            source.DamageModifiers.AddDealt(-0.3f);
            Entity target = CombatTestSetup.CreateEntity(Team.Enemy);

            DamageEvent result = CombatTestSetup.CreatePipeline().Apply(new Hit(source, 100f, DamageType.Physical), target);

            Assert.That(result.Amount, Is.EqualTo(65f).Within(Tolerance));
        }

        [Test]
        public void Apply_TargetTakesMore_FromAnySource()
        {
            // Marked: takes 25% more. 100 x 1.25 = 125, minus 5 Armor.
            Entity target = CombatTestSetup.CreateEntity(Team.Enemy);
            target.DamageModifiers.AddTaken(0.25f);

            DamageEvent result = CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(100f), target);

            Assert.That(result.Amount, Is.EqualTo(120f).Within(Tolerance));
        }

        [Test]
        public void Apply_IncomingHitHandler_CutsTheHitAndCancelsItsKnockback()
        {
            Entity target = CombatTestSetup.CreateEntity(Team.Enemy);
            target.AddIncomingHitHandler(new HalvingHandler());

            DamageEvent result = CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(100f), target);

            // (100 - 5 Armor) x (1 - 0.5).
            Assert.That(result.Amount, Is.EqualTo(47.5f).Within(Tolerance));
            Assert.That(result.KnockbackPrevented, Is.True);
        }

        [Test]
        public void Apply_IncomingHitHandler_DoesNotSeeDoTTicks()
        {
            Entity target = CombatTestSetup.CreateEntity(Team.Enemy);
            target.AddIncomingHitHandler(new HalvingHandler());

            DamageEvent result = CombatTestSetup.CreatePipeline()
                .Apply(CombatTestSetup.Environmental(20f, isDamageOverTime: true), target);

            Assert.That(result.Amount, Is.EqualTo(20f).Within(Tolerance));
        }

        [Test]
        public void Apply_HitThatIncapacitates_RaisesTheSourcesHitAndDefeatEvents()
        {
            Entity source = CombatTestSetup.CreateEntity(Team.Player, StatBlock.Uniform(90f));
            Entity target = CombatTestSetup.CreateEntity(Team.Enemy, StatBlock.Uniform(10f), roll: 0.99f);
            int hits = 0;
            Entity defeated = null;
            source.OnHitDealt += _ => hits++;
            source.OnDefeatedTarget += entity => defeated = entity;

            CombatTestSetup.CreatePipeline().Apply(new Hit(source, 1000f, DamageType.Physical), target);

            Assert.That(hits, Is.EqualTo(1));
            Assert.That(defeated, Is.SameAs(target));
        }

        [Test]
        public void Apply_HitThatCantBeDodged_IsNeverDodged()
        {
            // Finesse 20: 10% dodge, and the roll is under it. Like slamming into a wall.
            Entity target = CreateToughTarget();
            Entity source = CombatTestSetup.CreateEntity(Team.Player);

            DamageEvent result = CombatTestSetup.CreatePipeline(roll: 0f)
                .Apply(new Hit(source, 50f, DamageType.Physical, canBeDodged: false), target);

            Assert.That(result.Outcome, Is.EqualTo(DamageOutcome.Dealt));
        }

        [TestCase(100f, 20f, 0.5f, 40f, TestName = "ApplyToStructure_ArmorThenDamageReduction")]
        [TestCase(10f, 20f, 0f, 0.5f, TestName = "ApplyToStructure_ArmorAboveDamage_Deals5Percent")]
        [TestCase(100f, 0f, 0f, 100f, TestName = "ApplyToStructure_NoDefenses_DealsItAll")]
        public void ApplyToStructure_MitigatesLikeAnEntity(float damage, float armor, float reduction, float expected)
        {
            float dealt = CombatTestSetup.CreatePipeline()
                .ApplyToStructure(CombatTestSetup.Environmental(damage), armor, reduction);

            Assert.That(dealt, Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void ApplyToStructure_AddsTheSourcesRankDamageBonus()
        {
            // Rank B: +20%, before Armor.
            Entity source = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon, difficultyLevel: 2);

            float dealt = CombatTestSetup.CreatePipeline()
                .ApplyToStructure(new Hit(source, 100f, DamageType.Physical), 20f, 0f);

            Assert.That(dealt, Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void ApplyToStructure_DamageOverTime_IgnoresArmorAndDamageReduction()
        {
            float dealt = CombatTestSetup.CreatePipeline()
                .ApplyToStructure(CombatTestSetup.Environmental(20f, isDamageOverTime: true), 20f, 0.5f);

            Assert.That(dealt, Is.EqualTo(20f).Within(Tolerance));
        }

        private sealed class HalvingHandler : IIncomingHitHandler
        {
            public void OnIncomingHit(ref IncomingHit hit)
            {
                hit.ExtraDamageReduction = 0.5f;
                hit.PreventKnockback = true;
            }
        }
    }
}
