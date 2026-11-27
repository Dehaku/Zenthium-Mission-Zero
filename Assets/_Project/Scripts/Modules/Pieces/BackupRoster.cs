using System;
using System.Collections.Generic;
using Zenthium.Core;
using Zenthium.Data;

namespace Zenthium.Modules
{
    /// <summary>
    /// Who Call for Backup brings (modules.md): its <c>goons</c> and <c>champions</c> counts, drawn at random from the
    /// summoner's faction. Only entities that spawn at the mission's rank can come (the user's decision, Milestone 7);
    /// the same one can come more than once.
    /// </summary>
    public static class BackupRoster
    {
        public const string GoonsKey = "goons";
        public const string ChampionsKey = "champions";

        /// <summary>A count from a number, or a range rolled evenly, such as <c>goons=3-5</c> for 3, 4 or 5.</summary>
        public static int RollCount(DataValue value, IRandomSource random)
        {
            if (value == null)
            {
                return 0;
            }

            switch (value.Kind)
            {
                case DataValueKind.Number:
                    return Math.Max(0, (int)Math.Round(value.Number));
                case DataValueKind.Range:
                    int min = (int)Math.Round(value.RangeMin);
                    int max = (int)Math.Round(value.RangeMax);
                    int rolled = min + (int)Math.Floor(random.Value01() * (max - min + 1));
                    return Math.Max(0, Math.Min(rolled, max));
                default:
                    return 0;
            }
        }

        /// <summary>Adds <paramref name="count"/> random picks among the faction's entities of the tier that spawn at the level.</summary>
        /// <param name="eligible">Reused for the candidates, so a cast allocates nothing new.</param>
        public static void Pick(IReadOnlyList<EntityData> entities, FactionData faction, RankTier tier,
            int difficultyLevel, int count, IRandomSource random, List<EntityData> eligible, List<EntityData> results)
        {
            eligible.Clear();
            foreach (EntityData entity in entities)
            {
                if (entity != null && entity.Faction == faction && entity.RankTier == tier && entity.SpawnsAt(difficultyLevel))
                {
                    eligible.Add(entity);
                }
            }

            if (eligible.Count == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int index = Math.Min((int)Math.Floor(random.Value01() * eligible.Count), eligible.Count - 1);
                results.Add(eligible[index]);
            }
        }
    }
}
