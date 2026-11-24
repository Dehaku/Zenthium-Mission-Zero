using NUnit.Framework;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// combat.md and statuses.md: bonuses add as a percentage of the 6 m/s base, then slows multiply on top, so
    /// they never reach 100%.
    /// </summary>
    public class MoveSpeedTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void TwoHalfSlows_LeaveAQuarterSpeed()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy);

            goon.MoveSpeed.AddSlow(0.5f);
            goon.MoveSpeed.AddSlow(0.5f);

            Assert.That(goon.MoveSpeed.Current, Is.EqualTo(1.5f).Within(Tolerance));
        }

        [Test]
        public void Slows_MultiplyAfterBonusesAdd()
        {
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            hero.MoveSpeed.AddBonus(0.5f);

            hero.MoveSpeed.AddSlow(0.5f);

            // 6 x (1 + 0.5) x 0.5.
            Assert.That(hero.MoveSpeed.Current, Is.EqualTo(4.5f).Within(Tolerance));
        }

        [Test]
        public void RemovingASlow_RestoresTheSpeed()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy);
            ModifierHandle slow = goon.MoveSpeed.AddSlow(0.3f);

            goon.MoveSpeed.RemoveSlow(slow);

            Assert.That(goon.MoveSpeed.Current, Is.EqualTo(6f).Within(Tolerance));
        }
    }
}
