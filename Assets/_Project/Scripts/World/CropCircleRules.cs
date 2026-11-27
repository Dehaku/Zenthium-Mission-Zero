using System;
using Zenthium.Data;

namespace Zenthium.World
{
    /// <summary>
    /// The Repo Agency's crop circles (Factions/GalacticRepoAgency.md, Faction rules), from the faction's factions.csv
    /// values: how many a mission gets, and their size.
    /// </summary>
    public sealed class CropCircleRules
    {
        public const string BaseCountKey = "crop_circles_base";
        public const string PerRankKey = "crop_circles_per_rank";
        public const string MaxCountKey = "crop_circles_max";
        public const string RadiusKey = "crop_circle_radius_m";

        public CropCircleRules(float baseCount, float perRank, float maxCount, float radius)
        {
            BaseCount = baseCount;
            PerRank = perRank;
            MaxCount = maxCount;
            Radius = radius;
        }

        public float BaseCount { get; }
        public float PerRank { get; }
        public float MaxCount { get; }

        /// <summary>Meters.</summary>
        public float Radius { get; }

        /// <returns>False if the faction lacks one of the values, named in <paramref name="problem"/>.</returns>
        public static bool TryCreate(FactionData faction, out CropCircleRules rules, out string problem)
        {
            rules = null;
            problem = null;
            if (faction == null)
            {
                problem = "no faction";
                return false;
            }

            DataValueList values = faction.Values;
            if (!values.TryGetNumber(BaseCountKey, out float baseCount) || !values.TryGetNumber(PerRankKey, out float perRank)
                || !values.TryGetNumber(MaxCountKey, out float maxCount) || !values.TryGetNumber(RadiusKey, out float radius))
            {
                problem = $"{faction.Id} needs {BaseCountKey}, {PerRankKey}, {MaxCountKey} and {RadiusKey} in factions.csv";
                return false;
            }

            rules = new CropCircleRules(baseCount, perRank, maxCount, radius);
            return true;
        }

        /// <summary>1 at rank D plus 1 per rank, up to 5 at S, and no more after that (with the current data).</summary>
        /// <param name="difficultyLevel">Mission ranks above D (0 at D, 4 at S, 5 at S2).</param>
        public int Count(int difficultyLevel)
        {
            if (difficultyLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(difficultyLevel), difficultyLevel, "Rank D is 0.");
            }

            // Cast before rounding: Mono may keep the sum at higher precision (BuildLog, 6b test run).
            float count = (float)(BaseCount + PerRank * difficultyLevel);
            return (int)Math.Round(Math.Min(count, MaxCount));
        }
    }
}
