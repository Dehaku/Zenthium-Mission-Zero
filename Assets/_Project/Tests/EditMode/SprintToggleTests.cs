using NUnit.Framework;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// modules.md (Sprint row) and combat.md (Movement): a toggle, with a bonus that's a percentage of the 6 m/s base.
    /// </summary>
    public class SprintToggleTests
    {
        private const float Tolerance = 0.0001f;

        // modules.csv: speed_bonus=0.5.
        private const float SprintBonus = 0.5f;

        [Test]
        public void Toggle_On_AddsTheBonus()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            var sprint = new SprintToggle(hero.MoveSpeed, SprintBonus);

            sprint.Toggle();

            Assert.That(sprint.IsOn, Is.True);
            Assert.That(hero.MoveSpeed.Current, Is.EqualTo(9f).Within(Tolerance));
        }

        [Test]
        public void Toggle_Twice_ReturnsToBaseSpeed()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            var sprint = new SprintToggle(hero.MoveSpeed, SprintBonus);

            sprint.Toggle();
            sprint.Toggle();

            Assert.That(sprint.IsOn, Is.False);
            Assert.That(hero.MoveSpeed.Current, Is.EqualTo(6f).Within(Tolerance));
        }

        [Test]
        public void SetOn_Twice_AddsTheBonusOnce()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            var sprint = new SprintToggle(hero.MoveSpeed, SprintBonus);

            sprint.SetOn(true);
            sprint.SetOn(true);

            Assert.That(hero.MoveSpeed.Current, Is.EqualTo(9f).Within(Tolerance));
        }

        [Test]
        public void Toggle_Off_KeepsOtherBonuses()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            hero.MoveSpeed.AddBonus(0.25f);
            var sprint = new SprintToggle(hero.MoveSpeed, SprintBonus);

            sprint.Toggle();
            sprint.Toggle();

            Assert.That(hero.MoveSpeed.Current, Is.EqualTo(7.5f).Within(Tolerance));
        }
    }
}
