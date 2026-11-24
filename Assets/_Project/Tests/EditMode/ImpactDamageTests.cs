using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Movement;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// world.md (Walls): a knocked-back entity deals Heavy damage to the wall it hits and takes Light damage, both
    /// from the stat of whoever knocked it; Strength when the module has no stat.
    /// </summary>
    public class ImpactDamageTests
    {
        private const float Tolerance = 0.0001f;

        private static ImpactDamage CreateImpactDamage()
        {
            var constants = new WorldConstants(
                materialHp: new[] { 10f, 100f, 300f, 600f, 1000f },
                impactStructureDamageTier: new TierValue("Heavy", 2f),
                impactEntityDamageTier: new TierValue("Light", 0.5f),
                buildingImpactMultiplier: 10f,
                buildingThrowMultiplier: 2f,
                demolitionMultiplier: 10f,
                vehicleExplosionDelay: 1f,
                buildingNeighborRadius: 20f);
            return new ImpactDamage(CombatTestSetup.CreateRules().Formulas, constants);
        }

        [Test]
        public void ToStructure_Strength20_DealsHeavy100()
        {
            // (10 + 20 x 2) x Heavy(2).
            Assert.That(CreateImpactDamage().ToStructure(20f), Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void ToEntity_Strength20_TakesLight25()
        {
            // (10 + 20 x 2) x Light(0.5).
            Assert.That(CreateImpactDamage().ToEntity(20f), Is.EqualTo(25f).Within(Tolerance));
        }

        [Test]
        public void Cause_ModuleWithAStat_UsesThatStat()
        {
            Entity source = CombatTestSetup.CreateEntity(Team.Player, new StatBlock(20f, 40f, 10f, 10f, 10f));
            ModuleData module = TestModules.Create("Mind Push", ModuleCategory.Active, stat: Stat.Will);

            KnockbackCause cause = ImpactDamage.Cause(source, module);

            Assert.That(cause.Source, Is.SameAs(source));
            Assert.That(cause.StatValue, Is.EqualTo(40f));
        }

        [Test]
        public void Cause_ModuleWithoutAStat_UsesStrength()
        {
            Entity source = CombatTestSetup.CreateEntity(Team.Player, new StatBlock(20f, 40f, 10f, 10f, 10f));
            ModuleData module = TestModules.Create("Energy Shield", ModuleCategory.Active);

            Assert.That(ImpactDamage.Cause(source, module).StatValue, Is.EqualTo(20f));
        }

        [Test]
        public void Cause_SummonUsingItsSummonersStat_UsesTheSummoners()
        {
            Entity summoner = CombatTestSetup.CreateEntity(Team.Player, new StatBlock(10f, 30f, 10f, 10f, 10f));
            Entity summon = CombatTestSetup.CreateEntity(Team.Ally);
            ModuleData lash = TestModules.Create("Ink Lash", ModuleCategory.Active, stat: Stat.Will);

            KnockbackCause cause = ImpactDamage.Cause(summon, lash, summoner);

            Assert.That(cause.Source, Is.SameAs(summon));
            Assert.That(cause.StatValue, Is.EqualTo(30f));
        }

        [Test]
        public void Cause_NoSource_DealsNoImpact()
        {
            Assert.That(ImpactDamage.Cause(null, null).HasSource, Is.False);
        }
    }
}
