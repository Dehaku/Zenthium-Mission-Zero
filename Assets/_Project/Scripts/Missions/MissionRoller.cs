using System;
using System.Collections.Generic;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.World;

namespace Zenthium.Missions
{
    /// <summary>
    /// Rolls a random mission (factions.md, Picking a mission): the type at random, then a faction weighted by its
    /// Mission Weight for that type, then a location weighted by that faction's Location Weight. The rank sets the
    /// map size and the chance of a Leader (difficulty.md). Each step is public, so a launcher can choose some parts
    /// and roll the rest in the same order.
    /// </summary>
    public static class MissionRoller
    {
        private static readonly MissionType[] Types = (MissionType[])Enum.GetValues(typeof(MissionType));

        /// <param name="factions">The factions that can run missions.</param>
        /// <param name="locations">The locations a mission can be at, such as those the block library holds.</param>
        /// <param name="entities">Every entity in the game data, to find the faction's Leader.</param>
        /// <param name="seed">Stored in the setup, for the map and spawns.</param>
        /// <returns>False if there's no faction or location to pick.</returns>
        public static bool TryRoll(IRandomSource random, IReadOnlyList<FactionData> factions,
            IReadOnlyList<LocationType> locations, IReadOnlyList<EntityData> entities, int difficultyLevel,
            MissionConstants constants, int seed, out MissionSetup setup)
        {
            setup = null;
            MissionType type = RollType(random);
            FactionData faction = PickFaction(random, factions, type);
            if (faction == null || !TryPickLocation(random, faction, locations, out LocationType location))
            {
                return false;
            }

            bool hasLeader = RollsLeader(random, faction, entities, difficultyLevel, constants);
            setup = new MissionSetup(type, faction, location, difficultyLevel, constants.MapSizeFor(difficultyLevel),
                hasLeader, seed);
            return true;
        }

        public static MissionType RollType(IRandomSource random)
        {
            return Types[Math.Min((int)(random.Value01() * Types.Length), Types.Length - 1)];
        }

        /// <summary>A faction weighted by its Mission Weight for the type.</summary>
        /// <returns>Null if no faction has a weight above 0.</returns>
        public static FactionData PickFaction(IRandomSource random, IReadOnlyList<FactionData> factions,
            MissionType type)
        {
            var weights = new List<float>(factions.Count);
            foreach (FactionData faction in factions)
            {
                weights.Add(faction != null ? MissionWeights.ForMission(faction, type) : 0f);
            }

            int index = SocketPicker.Pick(weights, random.Value01());
            return index >= 0 ? factions[index] : null;
        }

        /// <summary>A location weighted by the faction's Location Weight.</summary>
        /// <returns>False if no location has a weight above 0.</returns>
        public static bool TryPickLocation(IRandomSource random, FactionData faction,
            IReadOnlyList<LocationType> locations, out LocationType location)
        {
            var weights = new List<float>(locations.Count);
            foreach (LocationType candidate in locations)
            {
                weights.Add(MissionWeights.ForLocation(faction, candidate));
            }

            int index = SocketPicker.Pick(weights, random.Value01());
            location = index >= 0 ? locations[index] : default;
            return index >= 0;
        }

        /// <summary>The Leader roll: only for a faction with a Leader at the rank.</summary>
        public static bool RollsLeader(IRandomSource random, FactionData faction, IReadOnlyList<EntityData> entities,
            int difficultyLevel, MissionConstants constants)
        {
            // Rolled even without a Leader, so the same seed rolls the same afterwards either way.
            float roll = random.Value01();
            float chance = constants.LeaderChance(difficultyLevel);

            // A chance of 1 is guaranteed (rank S), even on a roll of exactly 1.
            return new FactionRoster(faction, entities, difficultyLevel).Leader != null
                && (chance >= 1f || roll < chance);
        }
    }
}
