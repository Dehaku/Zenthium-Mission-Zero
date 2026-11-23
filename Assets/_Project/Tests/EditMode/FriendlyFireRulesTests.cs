using NUnit.Framework;
using Zenthium.Combat;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Who can hurt whom (combat.md, enemies.md; the matrix approved for Milestone 2).
    /// </summary>
    public class FriendlyFireRulesTests
    {
        [TestCase(Team.Player, Team.Enemy, false, true)]
        [TestCase(Team.Player, Team.Neutral, false, true)]
        [TestCase(Team.Player, Team.Ally, false, false)]
        [TestCase(Team.Player, Team.Ally, true, true)]
        [TestCase(Team.Player, Team.Civilian, false, false)]
        [TestCase(Team.Player, Team.Civilian, true, true)]
        [TestCase(Team.Ally, Team.Enemy, true, true)]
        [TestCase(Team.Ally, Team.Neutral, false, true)]
        [TestCase(Team.Ally, Team.Player, true, false)]
        [TestCase(Team.Ally, Team.Civilian, true, false)]
        [TestCase(Team.Enemy, Team.Player, false, true)]
        [TestCase(Team.Enemy, Team.Civilian, false, true)]
        [TestCase(Team.Enemy, Team.Neutral, false, true)]
        [TestCase(Team.Enemy, Team.Enemy, false, true)]
        [TestCase(Team.Neutral, Team.Player, false, true)]
        [TestCase(Team.Neutral, Team.Enemy, false, true)]
        [TestCase(Team.Civilian, Team.Enemy, false, false)]
        public void CanHurt_ByTeam(Team attacker, Team target, bool friendlyFireHeld, bool expected)
        {
            var rules = new FriendlyFireRules { IsPlayerFriendlyFireHeld = friendlyFireHeld };

            bool canHurt = rules.CanHurt(CombatTestSetup.CreateEntity(attacker), CombatTestSetup.CreateEntity(target));

            Assert.That(canHurt, Is.EqualTo(expected));
        }

        [Test]
        public void CanHurt_PlayerNeverHurtsItself_EvenWithFriendlyFireHeld()
        {
            var rules = new FriendlyFireRules { IsPlayerFriendlyFireHeld = true };
            Entity player = CombatTestSetup.CreateEntity(Team.Player);

            Assert.That(rules.CanHurt(player, player), Is.False);
        }

        [Test]
        public void CanHurt_EnemyFriendlyFireToggledOff_EnemiesCantHurtEnemies()
        {
            var rules = new FriendlyFireRules { IsEnemyFriendlyFireEnabled = false };

            bool canHurt = rules.CanHurt(
                CombatTestSetup.CreateEntity(Team.Enemy), CombatTestSetup.CreateEntity(Team.Enemy));

            Assert.That(canHurt, Is.False);
        }

        [TestCase(Team.Player)]
        [TestCase(Team.Ally)]
        [TestCase(Team.Civilian)]
        public void CanHurt_EnvironmentalDamage_HurtsEveryone(Team target)
        {
            Assert.That(new FriendlyFireRules().CanHurt(null, CombatTestSetup.CreateEntity(target)), Is.True);
        }
    }
}
