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
    }
}
