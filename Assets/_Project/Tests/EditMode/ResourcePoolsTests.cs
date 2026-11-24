using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Timing;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// combat.md (Resources): grants add to one pool, pools start full, +1 regen per real second,
    /// and <c>NoRegen</c> stops it.
    /// </summary>
    public class ResourcePoolsTests
    {
        private const float Tolerance = 0.0001f;

        private static ResourcePools CreatePools()
        {
            return CombatTestSetup.CreateEntity(Team.Enemy).Resources;
        }

        [Test]
        public void Grant_FromSeveralModules_AddsToOneFullPool()
        {
            ResourcePools pools = CreatePools();

            pools.Grant(ResourceType.Stamina, 15f);
            pools.Grant(ResourceType.Stamina, 50f);

            Assert.That(pools.Max(ResourceType.Stamina), Is.EqualTo(65f).Within(Tolerance));
            Assert.That(pools.Current(ResourceType.Stamina), Is.EqualTo(65f).Within(Tolerance));
        }

        [Test]
        public void TrySpend_NotEnough_SpendsNothing()
        {
            ResourcePools pools = CreatePools();
            pools.Grant(ResourceType.Mana, 15f);

            bool spent = pools.TrySpend(ResourceType.Mana, 20f);

            Assert.That(spent, Is.False);
            Assert.That(pools.Current(ResourceType.Mana), Is.EqualTo(15f).Within(Tolerance));
        }

        [TestCase(10f, 25f, TestName = "Tick_Regains1PerSecond")]
        [TestCase(100f, 50f, TestName = "Tick_RegenStopsAtMax")]
        public void Tick_Regenerates(float seconds, float expected)
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Enemy);
            entity.Resources.Grant(ResourceType.Stamina, 50f);
            entity.Resources.TrySpend(ResourceType.Stamina, 35f);

            entity.Tick(new GameClock(), seconds);

            Assert.That(entity.Resources.Current(ResourceType.Stamina), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void Tick_NoRegen_StopsTheBaselineRegen()
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Enemy);
            entity.Resources.Grant(ResourceType.Charge, 50f);
            entity.Resources.AddNoRegen(ResourceType.Charge);
            entity.Resources.TrySpend(ResourceType.Charge, 50f);

            entity.Tick(new GameClock(), 10f);

            Assert.That(entity.Resources.Current(ResourceType.Charge), Is.EqualTo(0f));
        }

        [Test]
        public void Tick_RegenRunsInRealSeconds_EvenDuringTimeStop()
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Enemy);
            entity.Resources.Grant(ResourceType.Ammo, 50f);
            entity.Resources.TrySpend(ResourceType.Ammo, 50f);
            var clock = new GameClock();
            clock.SetGlobalScale(0.5f);
            clock.SetTimePowerScale(0f);

            entity.Tick(clock, 10f);

            Assert.That(entity.Resources.Current(ResourceType.Ammo), Is.EqualTo(10f).Within(Tolerance));
        }

        [Test]
        public void Tick_Paused_NoRegen()
        {
            Entity entity = CombatTestSetup.CreateEntity(Team.Enemy);
            entity.Resources.Grant(ResourceType.Ammo, 50f);
            entity.Resources.TrySpend(ResourceType.Ammo, 50f);
            var clock = new GameClock();
            clock.SetPaused(true);

            entity.Tick(clock, 10f);

            Assert.That(entity.Resources.Current(ResourceType.Ammo), Is.EqualTo(0f));
        }

        [Test]
        public void ResourcePassive_AddsCapacity_AndRegenOnTopOfTheBaseline()
        {
            // Sprinter: stamina=100; stamina_regen=10.
            ModuleData sprinter = TestModules.Create("Sprinter", ModuleCategory.Passive,
                values: new[] { ("stamina", 100f), ("stamina_regen", 10f) });

            Entity hero = CombatTestSetup.CreateEntityWithModules(Team.Player, 1, sprinter);

            Assert.That(hero.Resources.Max(ResourceType.Stamina), Is.EqualTo(100f));
            Assert.That(hero.Resources.RegenPerSecond(ResourceType.Stamina), Is.EqualTo(11f));
        }

        [Test]
        public void NoRegenTag_StopsAllRegen_EvenWithARegenValue()
        {
            // Reaper Battery: charge=100; charge_regen=0, tagged NoRegen:Charge.
            ModuleData reaper = TestModules.Create("Reaper", ModuleCategory.Passive,
                noRegen: new[] { ResourceType.Charge }, values: new[] { ("charge", 100f), ("charge_regen", 0f) });

            Entity hero = CombatTestSetup.CreateEntityWithModules(Team.Player, 1, reaper);

            Assert.That(hero.Resources.Max(ResourceType.Charge), Is.EqualTo(100f));
            Assert.That(hero.Resources.RegenPerSecond(ResourceType.Charge), Is.EqualTo(0f));
        }
    }
}
