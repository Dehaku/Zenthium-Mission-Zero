using NUnit.Framework;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Statuses;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// statuses.md (Resists): chance = defender's resist stat / (defender's + attacker's stat); the attacker's stat
    /// is the module's own, or else the attacker's value of the resist stat.
    /// </summary>
    public class ResistRulesTests
    {
        private const float Tolerance = 0.0001f;

        [TestCase(10f, 10f, 0.5f, TestName = "Chance_EqualStats_Is50Percent")]
        [TestCase(30f, 10f, 0.75f, TestName = "Chance_DefenderThreeTimesStronger_Is75Percent")]
        [TestCase(10f, 30f, 0.25f, TestName = "Chance_AttackerThreeTimesStronger_Is25Percent")]
        [TestCase(0f, 20f, 0f, TestName = "Chance_DefenderHasNone_Is0")]
        public void Chance(float defender, float attacker, float expected)
        {
            Assert.That(ResistRules.Chance(defender, attacker), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void AttackerStat_ModuleWithAStat_UsesItsStat()
        {
            ModuleData module = TestModules.Create("Spell", ModuleCategory.Active, stat: Stat.Intelligence);

            Assert.That(ResistRules.AttackerStat(module, Stat.Will), Is.EqualTo(Stat.Intelligence));
        }

        [Test]
        public void AttackerStat_ModuleWithNoStat_UsesTheResistStat()
        {
            ModuleData megaphone = TestModules.Create("Horn", ModuleCategory.GearActive);

            Assert.That(ResistRules.AttackerStat(megaphone, Stat.Toughness), Is.EqualTo(Stat.Toughness));
        }

        [TestCase(0.49f, true, TestName = "Resists_EqualStats_RollBelowHalf_Resists")]
        [TestCase(0.51f, false, TestName = "Resists_EqualStats_RollAboveHalf_Lands")]
        public void Resists_RollsAgainstTheChance(float roll, bool expected)
        {
            ModuleData megaphone = TestModules.Create("Horn", ModuleCategory.GearActive);
            Entity attacker = CombatTestSetup.CreateEntity(Team.Player);
            Entity defender = CombatTestSetup.CreateEntity(Team.Enemy);
            IRandomSource random = new FixedRandom(roll);

            Assert.That(ResistRules.Resists(megaphone, Stat.Toughness, attacker, defender, random), Is.EqualTo(expected));
        }
    }
}
