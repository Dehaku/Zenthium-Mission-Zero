using System.Text;
using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// balance.md sections 1 and 4: damage = (10 + 2 x stat) x tier; and the MoreInfo text shows the same formula
    /// with live numbers (modules.md, Module info).
    /// </summary>
    public class StatFormulaTests
    {
        private const float Tolerance = 0.0001f;

        private static readonly TierValue Normal = new TierValue("Normal", 1f);

        [Test]
        public void Damage_Strength20Normal_Is50()
        {
            Entity rushHour = CombatTestSetup.CreateEntity(Team.Player, new StatBlock(20f, 10f, 10f, 20f, 10f));
            var formula = new StatFormula(Stat.Strength, Normal);

            Assert.That(formula.Damage(rushHour, CombatTestSetup.CreateRules().Formulas), Is.EqualTo(50f).Within(Tolerance));
        }

        [Test]
        public void Describe_ShowsTheFormulaWithLiveNumbers()
        {
            Entity rushHour = CombatTestSetup.CreateEntity(Team.Player, new StatBlock(20f, 10f, 10f, 20f, 10f));
            EntityRules rules = CombatTestSetup.CreateRules();
            var text = new StringBuilder();

            new StatFormula(Stat.Strength, Normal).Describe(text, rushHour, rules.Formula, rules.Formulas);

            Assert.That(text.ToString(), Does.Contain("Damage = (10 + Strength(20) x 2) x Normal(1) = 50"));
        }

        [Test]
        public void Describe_WithARankDamageBonus_ShowsTheBoostedTotal()
        {
            // Difficulty level 1: +10% damage, and +20% stats, so Strength 12.
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, Zenthium.Data.RankTier.Goon, 1);
            EntityRules rules = CombatTestSetup.CreateRules();
            var text = new StringBuilder();

            new StatFormula(Stat.Strength, Normal).Describe(text, goon, rules.Formula, rules.Formulas);

            Assert.That(text.ToString(), Does.Contain("Strength(12) x 2) x Normal(1) = 34, x Bonuses(1.1) = 37.4"));
        }
    }
}
