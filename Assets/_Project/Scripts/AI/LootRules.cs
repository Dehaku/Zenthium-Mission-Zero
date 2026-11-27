using Zenthium.Entities;

namespace Zenthium.AI
{
    /// <summary>Where a carrier takes the Valuables or citizens it holds (world.md, Goon behavior).</summary>
    public enum LootDelivery
    {
        /// <summary>It keeps them and carries on as normal.</summary>
        None,

        /// <summary>To its Leader, who takes them.</summary>
        Leader,

        /// <summary>Off the map, where they're lost.</summary>
        MapEdge,

        /// <summary>To the nearest crop circle, where they're lost (Factions/GalacticRepoAgency.md).</summary>
        CropCircle,
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
        /// its attack range, so melee goons often loot while ranged goons fight. Snatching citizens works the same
        /// (the user's decision, Milestone 7).
        /// </summary>
        /// <param name="nearestOpponentDistance">Meters; positive infinity if it sees none.</param>
        /// <param name="attackRange">The longest reach among its attack modules, in meters.</param>
        public static bool IsFreeToLoot(float nearestOpponentDistance, float attackRange)
        {
            return nearestOpponentDistance > attackRange;
        }

        /// <summary>Valuables, with no crop circle in play (see the full overload).</summary>
        public static LootDelivery Delivery(int carried, bool isLeader, bool hasMapEdgeTag, bool hasLeader)
        {
            return Delivery(carried, isLeader, hasMapEdgeTag, hasLeader, false);
        }

        /// <summary>
        /// Where carried Valuables go:
        /// <list type="number">
        /// <item>A <c>LootDestination:CropCircle</c> tag sends them to a crop circle, the Leader's too ("GleebPeep's
        /// loot goes to crop circles like everyone else's"). With no crop circle on the map, the tag does nothing
        /// and the rules below apply (a decision for 7a).</item>
        /// <item>A <c>LootDestination:MapEdge</c> tag sends them off the map.</item>
        /// <item>Otherwise they go to the Leader, or off the map with no Leader on the mission (a decision for 6b:
        /// world.md's "try to leave the map" wins). A Leader keeps what it holds; its fleeing comes with the
        /// Valuables goal (Milestone 9).</item>
        /// </list>
        /// </summary>
        /// <param name="hasCropCircle">It has the crop circle tag and the map has a crop circle.</param>
        public static LootDelivery Delivery(int carried, bool isLeader, bool hasMapEdgeTag, bool hasLeader,
            bool hasCropCircle)
        {
            if (carried <= 0)
            {
                return LootDelivery.None;
            }

            if (hasCropCircle)
            {
                return LootDelivery.CropCircle;
            }

            if (isLeader)
            {
                return LootDelivery.None;
            }

            if (hasMapEdgeTag || !hasLeader)
            {
                return LootDelivery.MapEdge;
            }

            return LootDelivery.Leader;
        }

        /// <summary>
        /// Seeing both a Valuable and a citizen it could drag, it goes for the nearer one (the user's decision,
        /// Milestone 7); the citizen on a tie.
        /// </summary>
        /// <param name="citizenDistance">Meters; positive infinity if it sees none.</param>
        /// <param name="valuableDistance">Meters; positive infinity if it sees none.</param>
        public static bool PrefersCitizen(float citizenDistance, float valuableDistance)
        {
            return !float.IsPositiveInfinity(citizenDistance) && citizenDistance <= valuableDistance;
        }

        /// <summary>
        /// Where a Tow Beam user takes what it tows: a crop circle if it has the tag and the map has one, otherwise
        /// the map edge (world.md: something that needs a destination, such as Tow Beam, uses the map edge).
        /// </summary>
        public static LootDelivery TowDelivery(bool hasCropCircle)
        {
            return hasCropCircle ? LootDelivery.CropCircle : LootDelivery.MapEdge;
        }

        /// <summary>
        /// Where an entity with Drag takes the citizens it drags: only one with a <c>LootDestination</c> tag drags
        /// them at all (enemies.md, Dragging citizens), to a crop circle or off the map. A crop circle tag with no
        /// crop circle on the map drags none.
        /// </summary>
        public static LootDelivery CitizenDelivery(bool hasMapEdgeTag, bool hasCropCircle)
        {
            if (hasCropCircle)
            {
                return LootDelivery.CropCircle;
            }

            return hasMapEdgeTag ? LootDelivery.MapEdge : LootDelivery.None;
        }
    }
}
