using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// heroes.md (Slots): 4 slots of each type, filled in order; base actions stay outside the slots; no duplicates;
    /// locked slots don't count as equipped; only equipped modules give their tags and resources.
    /// </summary>
    public class ModuleSlotsTests
    {
        [Test]
        public void Add_FillsTheNextSlotOfTheModulesType()
        {
            var slots = new ModuleSlots(4);
            ModuleData punch = TestModules.Active("Punch");
            ModuleData suit = TestModules.Gear("Suit");
            ModuleData dash = TestModules.Active("Dash");

            slots.Add(punch, true);
            slots.Add(suit, true);
            slots.Add(dash, true);

            Assert.That(slots.Get(SlotType.Active, 0), Is.SameAs(punch));
            Assert.That(slots.Get(SlotType.Active, 1), Is.SameAs(dash));
            Assert.That(slots.Get(SlotType.Gear, 0), Is.SameAs(suit));
        }

        [Test]
        public void Add_BaseAction_IsNotSlottable()
        {
            var slots = new ModuleSlots(4);

            SlotAddResult result = slots.Add(TestModules.Create("Kick", ModuleCategory.BaseAction), true);

            Assert.That(result, Is.EqualTo(SlotAddResult.NotSlottable));
        }

        [Test]
        public void Add_SameModuleTwice_IsADuplicate()
        {
            var slots = new ModuleSlots(4);
            ModuleData punch = TestModules.Active("Punch");
            slots.Add(punch, true);

            Assert.That(slots.Add(punch, true), Is.EqualTo(SlotAddResult.Duplicate));
        }

        [Test]
        public void Add_FifthActive_HasNoFreeSlot()
        {
            var slots = new ModuleSlots(4);
            for (int i = 0; i < 4; i++)
            {
                slots.Add(TestModules.Active("Active " + i), true);
            }

            Assert.That(slots.Add(TestModules.Active("Active 4"), true), Is.EqualTo(SlotAddResult.NoFreeSlot));
        }

        [Test]
        public void LockedSlot_IsNotEquipped_AndItsTagsDontCount()
        {
            var slots = new ModuleSlots(4);
            slots.Add(TestModules.Passive("Immovable", ModuleTag.KnockbackResistant), false);

            Assert.That(slots.IsEquipped(SlotType.Passive, 0), Is.False);
            Assert.That(slots.HasTag(ModuleTag.KnockbackResistant), Is.False);
        }

        [Test]
        public void Entity_FirstSlotOfEachTypeUnlocked_GrantsResourcesOnlyFromUnlockedModules()
        {
            // Like a preset hero's start: only the first slot of each type is unlocked.
            Entity hero = CombatTestSetup.CreateEntityWithModules(Team.Player, 1,
                TestModules.Active("Uppercut", ResourceType.Stamina, 15f),
                TestModules.Active("Dash", ResourceType.Stamina, 20f));

            Assert.That(hero.Slots.IsUnlocked(SlotType.Active, 0), Is.True);
            Assert.That(hero.Slots.IsUnlocked(SlotType.Active, 1), Is.False);
            Assert.That(hero.Resources.Max(ResourceType.Stamina), Is.EqualTo(15f));
        }

        [Test]
        public void Entity_EquippedTag_IsTheEntitysModuleTag()
        {
            Entity tank = CombatTestSetup.CreateEntityWithModules(Team.Enemy, EntitySetup.AllSlotsUnlocked,
                TestModules.Passive("Immovable", ModuleTag.KnockbackResistant));

            Assert.That(tank.HasModuleTag(ModuleTag.KnockbackResistant), Is.True);
        }
    }
}
