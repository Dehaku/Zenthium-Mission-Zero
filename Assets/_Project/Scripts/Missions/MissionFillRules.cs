using System;
using Zenthium.AI;
using Zenthium.Core;
using Zenthium.Data;

namespace Zenthium.Missions
{
    /// <summary>
    /// The rolls for what a mission puts on the map: Valuables per building (world.md, Placement), a civilian's
    /// Valuable, and a citizen's kind by its share (missions.md, Citizens).
    /// </summary>
    public static class MissionFillRules
    {
        /// <summary>A bank's vault and a store hold a random number in their ranges; any other building may hold 1.</summary>
        public static int BuildingValuables(LocationType location, MissionFillConstants constants, IRandomSource random)
        {
            switch (location)
            {
                case LocationType.Bank:
                    return InRange(constants.BankValuablesMin, constants.BankValuablesMax, random.Value01());
                case LocationType.Store:
                    return InRange(constants.StoreValuablesMin, constants.StoreValuablesMax, random.Value01());
                default:
                    return random.Value01() < constants.BuildingValuableChance ? 1 : 0;
            }
        }

        /// <summary>A civilian carries 1 Valuable on a successful roll.</summary>
        public static int CivilianValuables(MissionFillConstants constants, IRandomSource random)
        {
            return random.Value01() < constants.CivilianValuableChance ? 1 : 0;
        }

        /// <summary>A citizen's kind, each kind's chance its share of the three shares' total.</summary>
        public static CitizenKind CitizenKindFor(MissionFillConstants constants, float roll)
        {
            float scared = Math.Max(0f, constants.ScaredShare);
            float incapacitated = Math.Max(0f, constants.IncapacitatedShare);
            float stubborn = Math.Max(0f, constants.StubbornShare);
            float total = scared + incapacitated + stubborn;
            if (total <= 0f)
            {
                return CitizenKind.Scared;
            }

            float target = roll * total;
            if (target < scared)
            {
                return CitizenKind.Scared;
            }

            if (target < scared + incapacitated)
            {
                return CitizenKind.Incapacitated;
            }

            // A roll of exactly 1 lands on the top end: the last kind with a share.
            if (stubborn > 0f)
            {
                return CitizenKind.Stubborn;
            }

            return incapacitated > 0f ? CitizenKind.Incapacitated : CitizenKind.Scared;
        }

        /// <summary>A whole number from min to max, both included.</summary>
        private static int InRange(int min, int max, float roll)
        {
            if (max <= min)
            {
                return min;
            }

            return min + Math.Min((int)(roll * (max - min + 1)), max - min);
        }
    }
}
