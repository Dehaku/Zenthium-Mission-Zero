using Zenthium.Entities;

namespace Zenthium.AI
{
    /// <summary>Where a carrier takes the Valuables it holds (world.md, Goon behavior).</summary>
    public enum LootDelivery
    {
        /// <summary>It keeps them and carries on as normal.</summary>
        None,

        /// <summary>To its Leader, who takes them.</summary>
        Leader,

        /// <summary>Off the map, where they're lost.</summary>
        MapEdge,
    }

    /// <summary>Who loots, when, and where the loot goes (world.md, Valuables: Picking up and Goon behavior).</summary>
    public static class LootRules
    {
        /// <summary>
        /// Enemies loot, every faction (Zenthium creatures are neutral, so they don't). Allies, civilians and
        /// anything that can't move don't.
        /// </summary>
        public static bool CanLoot(Team team, bool isImmobile)
        {
            return team == Team.Enemy && !isImmobile;
        }

        /// <summary>
        /// It loots, and carries loot away, only while no opponent it can see (the player or an ally) is within
        /// its attack range, so melee goons often loot while ranged goons fight.
        /// </summary>
        /// <param name="nearestOpponentDistance">Meters; positive infinity if it sees none.</param>
        /// <param name="attackRange">The longest reach among its attack modules, in meters.</param>
        public static bool IsFreeToLoot(float nearestOpponentDistance, float attackRange)
        {
            return nearestOpponentDistance > attackRange;
        }

        /// <summary>
        /// A <c>LootDestination:MapEdge</c> tag sends it off the map. Otherwise it goes to its Leader, or off the map
        /// with no Leader on the mission (a decision for 6b: world.md's "try to leave the map" wins). A Leader keeps
        /// what it holds; its fleeing comes with the Valuables goal (Milestone 9).
        /// </summary>
        public static LootDelivery Delivery(int carried, bool isLeader, bool hasMapEdgeTag, bool hasLeader)
        {
            if (carried <= 0 || isLeader)
            {
                return LootDelivery.None;
            }

            if (hasMapEdgeTag || !hasLeader)
            {
                return LootDelivery.MapEdge;
            }

            return LootDelivery.Leader;
        }
    }
}
