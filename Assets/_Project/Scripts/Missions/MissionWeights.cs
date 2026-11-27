using System;
using System.Text;
using Zenthium.Data;

namespace Zenthium.Missions
{
    /// <summary>
    /// A faction's Mission and Location Weights (factions.md, Weights), looked up by the keys factions.csv uses:
    /// lower case with underscores, such as <c>arson_riot</c> or <c>records_office</c>. Unlisted is 1.
    /// </summary>
    public static class MissionWeights
    {
        public static float ForMission(FactionData faction, MissionType type)
        {
            return Weight(faction.MissionWeights, type);
        }

        public static float ForLocation(FactionData faction, LocationType location)
        {
            return Weight(faction.LocationWeights, location);
        }

        /// <summary>An enum value as a data key: <c>ArsonRiot</c> becomes <c>arson_riot</c>.</summary>
        public static string Key(Enum value)
        {
            string name = value.ToString();
            var key = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char letter = name[i];
                if (char.IsUpper(letter) && i > 0)
                {
                    key.Append('_');
                }

                key.Append(char.ToLowerInvariant(letter));
            }

            return key.ToString();
        }

        private static float Weight(DataValueList weights, Enum value)
        {
            return weights != null && weights.TryGetNumber(Key(value), out float weight)
                ? weight
                : FactionData.UnlistedWeight;
        }
    }
}
