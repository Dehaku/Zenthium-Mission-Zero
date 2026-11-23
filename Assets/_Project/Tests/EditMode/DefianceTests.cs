using NUnit.Framework;
using Zenthium.Combat;
using Zenthium.Entities;
using Zenthium.Timing;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Defiance (stats.md): proc chance from Will, guaranteed procs above 100%, Defiance Roll stacks,
    /// the 1 HP save, the cleanse, and Invulnerable time.
    /// </summary>
    public class DefianceTests
    {
        private const float Tolerance = 0.0001f;
        private const float LethalDamage = 100000f;

        /// <summary>Will 2130 is a 213% proc chance.</summary>
        private static Entity CreateEntityWith213Percent(Team team, float roll)
        {
            return CombatTestSetup.CreateEntity(team, new StatBlock(10f, 2130f, 10f, 10f, 10f), roll: roll);
        }

        private static Entity CreateEntityWithNoDefiance(Team team)
        {
            return CombatTestSetup.CreateEntity(team, new StatBlock(10f, 0f, 10f, 10f, 10f));
        }

        private static void WaitOutInvulnerability(Entity entity)
        {
            entity.Tick(new GameClock(), 10f);
        }

        [Test]
        public void ProcChance_IsOnePercentPer10Will()
        {
            Entity entity = CreateEntityWith213Percent(Team.Enemy, CombatTestSetup.RollThatMisses);

            Assert.That(entity.Defiance.ProcChance, Is.EqualTo(2.13f).Within(Tolerance));
        }

        [TestCase(0.12f, true, TestName = "Sequence213Percent_TwoGuaranteedProcs_ThenA13PercentRollThatProcs")]
        [TestCase(0.14f, false, TestName = "Sequence213Percent_TwoGuaranteedProcs_ThenA13PercentRollThatFails")]
        public void Sequence213Percent(float thirdRoll, bool thirdProcs)
        {
            // The roll is the same for every proc; the first two are guaranteed, so they must not use it.
            Entity entity = CreateEntityWith213Percent(Team.Enemy, thirdRoll);
            DamagePipeline pipeline = CombatTestSetup.CreatePipeline();

            pipeline.Apply(CombatTestSetup.Environmental(LethalDamage), entity);
            Assert.That(entity.IsIncapacitated, Is.False, "First proc");
            Assert.That(entity.Defiance.ProcChance, Is.EqualTo(1.13f).Within(Tolerance));

            WaitOutInvulnerability(entity);
            pipeline.Apply(CombatTestSetup.Environmental(LethalDamage), entity);
            Assert.That(entity.IsIncapacitated, Is.False, "Second proc");
            Assert.That(entity.Defiance.ProcChance, Is.EqualTo(0.13f).Within(Tolerance));

            WaitOutInvulnerability(entity);
            pipeline.Apply(CombatTestSetup.Environmental(LethalDamage), entity);
            Assert.That(entity.IsIncapacitated, Is.EqualTo(!thirdProcs), "Third roll");
            Assert.That(entity.Defiance.RollStacks, Is.EqualTo(thirdProcs ? 3 : 2));
        }

        [Test]
        public void Proc_LeavesTheEntityAt1Hp_AndRaisesOnDefianceProc()
        {
            Entity entity = CreateEntityWith213Percent(Team.Enemy, CombatTestSetup.RollThatMisses);
            bool procRaised = false;
            bool incapacitatedRaised = false;
            entity.OnDefianceProc += () => procRaised = true;
            entity.OnIncapacitated += () => incapacitatedRaised = true;

            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(LethalDamage), entity);

            Assert.That(entity.Health.Current, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(procRaised, Is.True);
            Assert.That(incapacitatedRaised, Is.False);
        }

        [Test]
        public void Proc_CallsTheCleanse()
        {
            Entity entity = CreateEntityWith213Percent(Team.Enemy, CombatTestSetup.RollThatMisses);
            var cleanse = new CountingCleanse();
            entity.SetDefianceCleanse(cleanse);

            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(LethalDamage), entity);

            Assert.That(cleanse.Calls, Is.EqualTo(1));
        }

        [TestCase(Team.Player, 5f, TestName = "Proc_PlayerIsInvulnerableFor5Seconds")]
        [TestCase(Team.Enemy, 1f, TestName = "Proc_EveryoneElseIsInvulnerableFor1Second")]
        [TestCase(Team.Ally, 1f, TestName = "Proc_AllyIsInvulnerableFor1Second")]
        public void Proc_GrantsInvulnerableTimeByTeam(Team team, float expectedSeconds)
        {
            Entity entity = CreateEntityWith213Percent(team, CombatTestSetup.RollThatMisses);

            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(LethalDamage), entity);

            Assert.That(entity.Invulnerability.RemainingSeconds, Is.EqualTo(expectedSeconds).Within(Tolerance));
        }

        [Test]
        public void Invulnerable_BlocksHitsUntilItsTimeRunsOut()
        {
            Entity entity = CreateEntityWith213Percent(Team.Player, CombatTestSetup.RollThatMisses);
            DamagePipeline pipeline = CombatTestSetup.CreatePipeline();
            var clock = new GameClock();
            pipeline.Apply(CombatTestSetup.Environmental(LethalDamage), entity);

            entity.Tick(clock, 4.9f);
            DamageEvent during = pipeline.Apply(CombatTestSetup.Environmental(LethalDamage), entity);
            entity.Tick(clock, 0.2f);
            DamageEvent after = pipeline.Apply(CombatTestSetup.Environmental(LethalDamage), entity);

            Assert.That(during.Outcome, Is.EqualTo(DamageOutcome.Blocked));
            Assert.That(after.Outcome, Is.EqualTo(DamageOutcome.Dealt));
        }

        [Test]
        public void NoProcChance_LethalHitIncapacitates()
        {
            Entity entity = CreateEntityWithNoDefiance(Team.Enemy);
            bool incapacitatedRaised = false;
            entity.OnIncapacitated += () => incapacitatedRaised = true;

            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(LethalDamage), entity);

            Assert.That(entity.IsIncapacitated, Is.True);
            Assert.That(entity.Health.Current, Is.EqualTo(0f));
            Assert.That(incapacitatedRaised, Is.True);
        }

        [Test]
        public void HitThatLeavesHpAboveZero_DoesNotUseDefiance()
        {
            Entity entity = CreateEntityWith213Percent(Team.Enemy, CombatTestSetup.RollThatMisses);

            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(50f), entity);

            Assert.That(entity.Defiance.RollStacks, Is.EqualTo(0));
            Assert.That(entity.Health.Current, Is.EqualTo(55f).Within(Tolerance));
        }

        private sealed class CountingCleanse : IDefianceCleanse
        {
            public int Calls { get; private set; }

            public void CleanseForDefiance()
            {
                Calls++;
            }
        }
    }
}
