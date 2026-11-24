using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Statuses;
using Zenthium.Timing;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// statuses.md: Refresh, Intensity and Independent stacking; half-length crowd control on Leaders and the player;
    /// "while inside" pulsing (0.5 s on, 0.5 s off); Invulnerable blocking debuffs; the Defiance cleanse; durations on
    /// game time; disables and <c>CannotBeDisabled</c>.
    /// </summary>
    public class StatusEffectsTests
    {
        private const float Tolerance = 0.0001f;

        private static StatusApplication Apply(StatusType type, float seconds, params StatusModifier[] modifiers)
        {
            return new StatusApplication(type, null, seconds, modifiers);
        }

        private static void Tick(Entity entity, float seconds)
        {
            entity.Tick(new GameClock(), seconds);
        }

        [Test]
        public void Refresh_ReapplyingKeepsOneInstance_AndRestartsTheTimer()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            goon.Statuses.TryApply(Apply(StatusType.Stun, 3f));
            Tick(goon, 2f);

            goon.Statuses.TryApply(Apply(StatusType.Stun, 3f));
            Tick(goon, 2.5f);

            Assert.That(goon.Statuses.Has(StatusType.Stun), Is.True);
            Assert.That(goon.Statuses.Layers(StatusType.Stun), Is.EqualTo(1));
            Assert.That(goon.Statuses.RemainingSeconds(StatusType.Stun), Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Refresh_AShorterReapply_DoesNotShortenTheTimeLeft()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            goon.Statuses.TryApply(Apply(StatusType.Root, 5f));

            goon.Statuses.TryApply(Apply(StatusType.Root, 1f));

            Assert.That(goon.Statuses.RemainingSeconds(StatusType.Root), Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void Intensity_EachApplicationAddsALayerUpToTheCap_AndScalesItsModifier()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            var armorPerLayer = new[] { new StatusModifier(StatusModifierTarget.Armor, 5f) };

            for (int i = 0; i < 4; i++)
            {
                hero.Statuses.TryApply(new StatusApplication(StatusType.ReactivePlating, null, 5f, armorPerLayer, 3));
            }

            // Toughness 10 gives 5 Armor, plus 3 layers of 5.
            Assert.That(hero.Statuses.Layers(StatusType.ReactivePlating), Is.EqualTo(3));
            Assert.That(hero.Defenses.Armor, Is.EqualTo(20f).Within(Tolerance));
        }

        [Test]
        public void Independent_EachApplicationRunsOnItsOwnTimer()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            goon.Statuses.TryApply(Apply(StatusType.Burn, 2f));
            Tick(goon, 1f);
            goon.Statuses.TryApply(Apply(StatusType.Burn, 2f));

            Assert.That(goon.Statuses.Layers(StatusType.Burn), Is.EqualTo(2));
            Tick(goon, 1.5f);
            Assert.That(goon.Statuses.Layers(StatusType.Burn), Is.EqualTo(1));
            Tick(goon, 1f);
            Assert.That(goon.Statuses.Has(StatusType.Burn), Is.False);
        }

        [Test]
        public void NoStack_ReapplyingWhileActive_DoesNothing()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            var tripleStrength = new StatusModifier(StatusModifierTarget.StatBoost, 2f, Stat.Strength);

            bool first = hero.Statuses.TryApply(Apply(StatusType.DeathlyRage, StatusApplication.UntilRemoved, tripleStrength));
            bool second = hero.Statuses.TryApply(Apply(StatusType.DeathlyRage, StatusApplication.UntilRemoved, tripleStrength));

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(hero.Stats.Get(Stat.Strength), Is.EqualTo(30f).Within(Tolerance));
        }

        [TestCase(Team.Player, null, 2f, TestName = "CrowdControl_OnThePlayer_IsHalfLength")]
        [TestCase(Team.Enemy, RankTier.Leader, 2f, TestName = "CrowdControl_OnALeader_IsHalfLength")]
        [TestCase(Team.Enemy, RankTier.Goon, 4f, TestName = "CrowdControl_OnAGoon_IsFullLength")]
        [TestCase(Team.Enemy, RankTier.Champion, 4f, TestName = "CrowdControl_OnAChampion_IsFullLength")]
        public void CrowdControl_Length(Team team, RankTier? rankTier, float expectedSeconds)
        {
            Entity entity = CombatTestSetup.CreateEntity(team, rankTier);

            entity.Statuses.TryApply(Apply(StatusType.Stun, 4f));

            Assert.That(entity.Statuses.RemainingSeconds(StatusType.Stun), Is.EqualTo(expectedSeconds).Within(Tolerance));
        }

        [Test]
        public void Debuff_OnThePlayer_IsFullLength()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);

            hero.Statuses.TryApply(Apply(StatusType.Marked, 4f));

            Assert.That(hero.Statuses.RemainingSeconds(StatusType.Marked), Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void WhileInside_OnThePlayer_PulsesHalfASecondOnAndOff()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            hero.Statuses.SetInsideArea(Apply(StatusType.Stun, StatusApplication.UntilRemoved));

            Assert.That(hero.Statuses.Has(StatusType.Stun), Is.True, "At 0 s");
            Tick(hero, 0.7f);
            Assert.That(hero.Statuses.Has(StatusType.Stun), Is.False, "At 0.7 s");
            Tick(hero, 0.5f);
            Assert.That(hero.Statuses.Has(StatusType.Stun), Is.True, "At 1.2 s");
        }

        [Test]
        public void WhileInside_OnAGoon_HoldsUntilItLeaves()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            goon.Statuses.SetInsideArea(Apply(StatusType.Stun, StatusApplication.UntilRemoved));

            Tick(goon, 0.7f);
            Assert.That(goon.Statuses.Has(StatusType.Stun), Is.True, "Still inside");
            goon.Statuses.LeaveArea(StatusType.Stun, null);
            Assert.That(goon.Statuses.Has(StatusType.Stun), Is.False, "Left");
        }

        [Test]
        public void Stun_StopsMovingAndActing_RootOnlyMoving()
        {
            Entity stunned = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            Entity rooted = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);

            stunned.Statuses.TryApply(Apply(StatusType.Stun, 3f));
            rooted.Statuses.TryApply(Apply(StatusType.Root, 3f));

            Assert.That(stunned.Statuses.CanMove, Is.False);
            Assert.That(stunned.Statuses.CanAct, Is.False);
            Assert.That(rooted.Statuses.CanMove, Is.False);
            Assert.That(rooted.Statuses.CanAct, Is.True);
        }

        [Test]
        public void Invulnerable_BlocksNewHostileStatuses_ButNotBuffs()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            hero.AddInvulnerableHold();

            bool stunned = hero.Statuses.TryApply(Apply(StatusType.Stun, 3f));
            bool plated = hero.Statuses.TryApply(Apply(StatusType.ReactivePlating, 3f));

            Assert.That(stunned, Is.False);
            Assert.That(plated, Is.True);
        }

        [Test]
        public void DefianceProc_CleansesDebuffs_KeepsCarryingTreasureAndHiddenMarkers()
        {
            // Will 2130: a 213% Defiance chance, so the lethal hit procs.
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, new StatBlock(10f, 2130f, 10f, 10f, 10f));
            goon.Statuses.TryApply(Apply(StatusType.Stun, 3f));
            goon.Statuses.TryApply(Apply(StatusType.Marked, 3f));
            goon.Statuses.TryApply(Apply(StatusType.CarryingTreasure, StatusApplication.UntilRemoved));
            goon.Statuses.TryApply(Apply(StatusType.Charmed, StatusApplication.UntilRemoved));

            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(10000f), goon);

            Assert.That(goon.Health.Current, Is.EqualTo(1f).Within(Tolerance), "Defiance procced");
            Assert.That(goon.Statuses.Has(StatusType.Stun), Is.False);
            Assert.That(goon.Statuses.Has(StatusType.Marked), Is.False);
            Assert.That(goon.Statuses.Has(StatusType.CarryingTreasure), Is.True);
            Assert.That(goon.Statuses.Has(StatusType.Charmed), Is.True);
        }

        [Test]
        public void Durations_TimeStopFreezesThem_TimeSlowStretchesThem()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            var clock = new GameClock();
            goon.Statuses.TryApply(Apply(StatusType.Stun, 3f));

            clock.SetTimePowerScale(0f);
            goon.Tick(clock, 10f);
            Assert.That(goon.Statuses.RemainingSeconds(StatusType.Stun), Is.EqualTo(3f).Within(Tolerance), "Time Stop");

            clock.SetTimePowerScale(0.5f);
            goon.Tick(clock, 4f);
            Assert.That(goon.Statuses.RemainingSeconds(StatusType.Stun), Is.EqualTo(1f).Within(Tolerance), "Time Slow");
        }

        [Test]
        public void Durations_OnThePlayer_IgnoreTimeStop()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            var clock = new GameClock();
            hero.Statuses.TryApply(Apply(StatusType.Marked, 3f));

            clock.SetTimePowerScale(0f);
            hero.Tick(clock, 3.1f);

            Assert.That(hero.Statuses.Has(StatusType.Marked), Is.False);
        }

        [Test]
        public void Modifiers_LastAsLongAsTheStatus()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            goon.Statuses.TryApply(Apply(StatusType.Marked, 4f,
                new StatusModifier(StatusModifierTarget.DamageDealt, -0.3f),
                new StatusModifier(StatusModifierTarget.DamageTaken, 0.25f)));

            Assert.That(goon.DamageModifiers.DealtMultiplier, Is.EqualTo(0.7f).Within(Tolerance));
            Assert.That(goon.DamageModifiers.TakenMultiplier, Is.EqualTo(1.25f).Within(Tolerance));
            Tick(goon, 4f);
            Assert.That(goon.DamageModifiers.DealtMultiplier, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(goon.DamageModifiers.TakenMultiplier, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Slows_FromTwoStatuses_Multiply()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);

            goon.Statuses.TryApply(Apply(StatusType.Gooed, 3f, new StatusModifier(StatusModifierTarget.Slow, 0.5f)));
            goon.Statuses.TryApply(Apply(StatusType.BrokenLimb, 3f, new StatusModifier(StatusModifierTarget.Slow, 0.5f)));

            // 6 m/s x 0.5 x 0.5.
            Assert.That(goon.MoveSpeed.Current, Is.EqualTo(1.5f).Within(Tolerance));
        }

        [Test]
        public void SlotDisable_SkipsCannotBeDisabled_AndEndsWithTheStatus()
        {
            ModuleData suit = TestModules.Gear("Suit");
            ModuleData battery = TestModules.Gear("Battery", ModuleTag.CannotBeDisabled);
            Entity hero = CombatTestSetup.CreateEntityWithModules(Team.Player, 4, suit, battery);

            bool first = hero.Statuses.TryApply(Apply(StatusType.SlotDisable, 3f));
            bool second = hero.Statuses.TryApply(Apply(StatusType.SlotDisable, 3f));

            Assert.That(first, Is.True);
            Assert.That(second, Is.False, "No slot left that can be disabled");
            Assert.That(hero.Slots.IsDisabled(SlotType.Gear, 0), Is.True, "Suit");
            Assert.That(hero.Slots.IsDisabled(SlotType.Gear, 1), Is.False, "Battery");
            Tick(hero, 3f);
            Assert.That(hero.Slots.IsDisabled(SlotType.Gear, 0), Is.False, "Ended");
        }

        [Test]
        public void Silenced_StopsActivesOnly()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);

            hero.Statuses.TryApply(Apply(StatusType.Silenced, 3f));

            Assert.That(hero.Statuses.IsSilenced, Is.True);
            Assert.That(hero.Statuses.CanAct, Is.True, "Kick and Drag still work");
        }
    }
}
