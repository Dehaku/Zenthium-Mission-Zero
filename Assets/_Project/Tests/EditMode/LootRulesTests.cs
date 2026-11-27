using NUnit.Framework;
using Zenthium.AI;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// world.md, Valuables: enemies loot only while no opponent is in their attack range, and take loot to their
    /// Leader, to a <c>LootDestination:MapEdge</c>, or off the map with no Leader (the 6b decision).
    /// </summary>
    public class LootRulesTests
    {
        [TestCase(Team.Enemy, false, true)]
        [TestCase(Team.Enemy, true, false)]
        [TestCase(Team.Ally, false, false)]
        [TestCase(Team.Neutral, false, false)]
        [TestCase(Team.Civilian, false, false)]
        public void CanLoot_OnlyMobileEnemies(Team team, bool isImmobile, bool expected)
        {
            Assert.That(LootRules.CanLoot(team, isImmobile), Is.EqualTo(expected));
        }

        [TestCase(2f, 3f, false)]
        [TestCase(3f, 3f, false)]
        [TestCase(3.5f, 3f, true)]
        [TestCase(10f, 20f, false)]
        [TestCase(float.PositiveInfinity, 20f, true)]
        public void IsFreeToLoot_OnlyWithNoOpponentInAttackRange(float opponentDistance, float attackRange, bool expected)
        {
            Assert.That(LootRules.IsFreeToLoot(opponentDistance, attackRange), Is.EqualTo(expected));
        }

        [TestCase(3, false, false, true, LootDelivery.Leader)]
        [TestCase(3, false, false, false, LootDelivery.MapEdge)]
        [TestCase(3, false, true, true, LootDelivery.MapEdge)]
        [TestCase(0, false, false, true, LootDelivery.None)]
        [TestCase(0, false, true, false, LootDelivery.None)]
        [TestCase(3, true, false, false, LootDelivery.None)]
        public void Delivery_PicksWhereTheLootGoes(int carried, bool isLeader, bool hasMapEdgeTag, bool hasLeader,
            LootDelivery expected)
        {
            Assert.That(LootRules.Delivery(carried, isLeader, hasMapEdgeTag, hasLeader), Is.EqualTo(expected));
        }

        [TestCase(3, false, false, true, true, LootDelivery.CropCircle)]
        [TestCase(3, true, false, false, true, LootDelivery.CropCircle)]
        [TestCase(3, false, false, true, false, LootDelivery.Leader)]
        [TestCase(3, true, false, true, false, LootDelivery.None)]
        [TestCase(0, false, false, true, true, LootDelivery.None)]
        public void Delivery_CropCircle_TakesEveryonesLootIncludingTheLeaders(int carried, bool isLeader,
            bool hasMapEdgeTag, bool hasLeader, bool hasCropCircle, LootDelivery expected)
        {
            Assert.That(LootRules.Delivery(carried, isLeader, hasMapEdgeTag, hasLeader, hasCropCircle),
                Is.EqualTo(expected));
        }

        [TestCase(false, true, LootDelivery.CropCircle)]
        [TestCase(true, true, LootDelivery.CropCircle)]
        [TestCase(true, false, LootDelivery.MapEdge)]
        [TestCase(false, false, LootDelivery.None)]
        public void CitizenDelivery_OnlyWithALootDestination(bool hasMapEdgeTag, bool hasCropCircle,
            LootDelivery expected)
        {
            Assert.That(LootRules.CitizenDelivery(hasMapEdgeTag, hasCropCircle), Is.EqualTo(expected));
        }

        [TestCase(4f, 6f, true)]
        [TestCase(6f, 4f, false)]
        [TestCase(5f, 5f, true)]
        [TestCase(5f, float.PositiveInfinity, true)]
        [TestCase(float.PositiveInfinity, float.PositiveInfinity, false)]
        public void PrefersCitizen_WhenItsNearer(float citizenDistance, float valuableDistance, bool expected)
        {
            Assert.That(LootRules.PrefersCitizen(citizenDistance, valuableDistance), Is.EqualTo(expected));
        }
    }
}
