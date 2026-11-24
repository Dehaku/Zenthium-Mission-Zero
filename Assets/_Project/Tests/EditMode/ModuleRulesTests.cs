using NUnit.Framework;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Rules Milestone 4b's modules read from data: placement within range, Living Ink's extra summons, Force Spell
    /// Tome's bonus on Magic hits and summons' hits, and Overclocked's charge threshold (modules.md).
    /// </summary>
    public class ModuleRulesTests
    {
        private const float Tolerance = 0.0001f;

        [TestCase(5f, 5f, TestName = "PlacementPoint_AimedPointInRange_IsThePoint")]
        [TestCase(40f, 15f, TestName = "PlacementPoint_AimedPointBeyondRange_StopsAtRange")]
        [TestCase(float.PositiveInfinity, 15f, TestName = "PlacementPoint_StickAim_UsesFullRange")]
        public void PlacementPoint(float pointDistance, float expectedDistance)
        {
            var aim = new ModuleAim(new Vector3(0f, 0f, 2f), pointDistance);

            Vector3 point = aim.PlacementPoint(new Vector3(1f, 1f, 1f), 15f);

            Assert.That(point.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(point.y, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(point.z, Is.EqualTo(1f + expectedDistance).Within(Tolerance));
        }

        // Living Ink: extra_beasts=1.
        [TestCase(1, 2, TestName = "SummonCount_LivingInkEquipped_AddsOne")]
        [TestCase(0, 1, TestName = "SummonCount_LivingInkLocked_StaysOne")]
        public void SummonCount(int unlockedSlots, int expected)
        {
            ModuleData livingInk = TestModules.Create("Living Ink", ModuleCategory.Passive,
                values: new[] { (SummonModule.ExtraSummonsKey, 1f) });
            Entity rune = CombatTestSetup.CreateEntityWithModules(Team.Player, unlockedSlots, livingInk);

            Assert.That(SummonModule.Count(1, rune.Slots), Is.EqualTo(expected));
        }

        // Force Spell Tome: magic_hit_damage=15.
        [TestCase(DamageType.Magic, 15f, TestName = "SpellBonus_OwnMagicHit_Adds15")]
        [TestCase(DamageType.Physical, 0f, TestName = "SpellBonus_OwnPhysicalHit_AddsNothing")]
        public void SpellBonus_OnTheUsersOwnHits(DamageType type, float expected)
        {
            Entity rune = CreateRuneWithTome();

            Assert.That(SpellDamageBonus.For(rune, type), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void SpellBonus_SummonsHit_AddsTheSummonersBonus_WhateverItsType()
        {
            Entity rune = CreateRuneWithTome();
            var setup = new EntitySetup(Team.Ally, StatBlock.Uniform(10f)) { Summoner = rune };
            var beast = new Entity(setup, CombatTestSetup.CreateRules(), new FixedRandom(CombatTestSetup.RollThatMisses));

            Assert.That(SpellDamageBonus.For(beast, DamageType.Physical), Is.EqualTo(15f).Within(Tolerance));
        }

        [TestCase(200f, 245f, true, TestName = "IsCharged_AtLeast80Percent_IsTrue")]
        [TestCase(190f, 245f, false, TestName = "IsCharged_Below80Percent_IsFalse")]
        [TestCase(0f, 0f, false, TestName = "IsCharged_NoChargePool_IsFalse")]
        public void IsCharged(float current, float max, bool expected)
        {
            // Overclocked: charge_above=0.8.
            Assert.That(OverclockedPassive.IsCharged(current, max, 0.8f), Is.EqualTo(expected));
        }

        private static Entity CreateRuneWithTome()
        {
            ModuleData tome = TestModules.Create("Force Spell Tome", ModuleCategory.GearPassive,
                values: new[] { (SpellDamageBonus.MagicHitDamageKey, 15f) });
            return CombatTestSetup.CreateEntityWithModules(Team.Player, 1, tome);
        }
    }
}
