using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Statuses;
using Zenthium.Timing;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// The gate every activated module shares (modules.md, Schema): cooldowns in real seconds that ignore the
    /// timescale and stop while paused (time.md), resource costs, and Stun stopping powers (statuses.md).
    /// </summary>
    public class ModuleActivationTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Cooldown_UnderTimeStop_StillRunsInRealSeconds()
        {
            var clock = new GameClock();
            clock.SetTimePowerScale(0f);
            var activation = new ModuleActivation(3f, null, 0f);
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            activation.TryBegin(hero, true);

            activation.Tick(clock.CooldownDelta(3f));

            Assert.That(activation.Check(hero, true), Is.EqualTo(ActivationBlock.None));
        }

        [Test]
        public void Cooldown_WhilePaused_DoesNotRun()
        {
            var clock = new GameClock();
            clock.SetPaused(true);
            var activation = new ModuleActivation(3f, null, 0f);
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            activation.TryBegin(hero, true);

            activation.Tick(clock.CooldownDelta(10f));

            Assert.That(activation.Check(hero, true), Is.EqualTo(ActivationBlock.Cooldown));
        }

        [Test]
        public void TryBegin_SpendsTheCost_ThenCantAffordAnother()
        {
            Entity hero = CombatTestSetup.CreateEntityWithModules(Team.Player, 1,
                TestModules.Active("Uppercut", ResourceType.Stamina, 15f));
            var activation = new ModuleActivation(null, ResourceType.Stamina, 15f);

            bool first = activation.TryBegin(hero, true);

            Assert.That(first, Is.True);
            Assert.That(hero.Resources.Current(ResourceType.Stamina), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(activation.Check(hero, true), Is.EqualTo(ActivationBlock.CantAfford));
        }

        [Test]
        public void Check_Stunned_CantAct()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            hero.Statuses.TryApply(new StatusApplication(StatusType.Stun, null, 3f));

            Assert.That(new ModuleActivation(null, null, 0f).Check(hero, true), Is.EqualTo(ActivationBlock.CantAct));
        }

        [Test]
        public void Check_SlotNotUsable_IsBlocked_AndStartsNoCooldown()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            var activation = new ModuleActivation(3f, null, 0f);

            bool used = activation.TryBegin(hero, false);

            Assert.That(used, Is.False);
            Assert.That(activation.Cooldown.IsReady, Is.True);
        }
    }
}
