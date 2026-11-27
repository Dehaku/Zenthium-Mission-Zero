using System;
using System.Collections.Generic;
using Zenthium.Core;
using Zenthium.Data;

namespace Zenthium.Missions
{
    /// <summary>A squad to spawn: 3-5 goons, sometimes led by a Champion, or a Champion alone (enemies.md, Squads).</summary>
    public sealed class SpawnSquad
    {
        public SpawnSquad(EntityData champion)
        {
            Champion = champion;
        }

        /// <summary>Null for a squad of goons only.</summary>
        public EntityData Champion { get; internal set; }

        public List<EntityData> Goons { get; } = new List<EntityData>();

        /// <summary>What its members cost from the threat budget.</summary>
        public int Cost
        {
            get
            {
                int cost = Champion != null ? FactionRoster.Cost(Champion) : 0;
                foreach (EntityData goon in Goons)
                {
                    cost += FactionRoster.Cost(goon);
                }

                return cost;
            }
        }

        public int Count => Goons.Count + (Champion != null ? 1 : 0);
    }

    /// <summary>
    /// Spends a threat budget (enemies.md, Threat budget): Champions first, at random, up to their share of it, then
    /// goons at random with the rest, until nothing more is affordable. Then it groups the goons into squads and gives
    /// each Champion a squad to lead, or leaves it alone.
    /// </summary>
    public static class ThreatBudgetSpender
    {
        // A share times a budget, such as 0.1 x 30, can land a hair under the whole number it means.
        private const float RoundingSlack = 0.0001f;

        /// <param name="budget">What to spend: the whole mission's, or a part such as a wave's.</param>
        /// <param name="championShare">The most of the budget Champions can use, as a fraction.</param>
        /// <param name="results">Cleared, then filled with the squads.</param>
        public static void Spend(float budget, float championShare, FactionRoster roster, SpawnConstants constants,
            IRandomSource random, List<SpawnSquad> results)
        {
            results.Clear();
            int total = WholePoints(budget);
            int championBudget = Math.Min(total, WholePoints(budget * championShare));

            var champions = new List<EntityData>();
            int championSpend = Buy(roster.Champions, championBudget, random, champions);

            var goons = new List<EntityData>();
            Buy(roster.Goons, total - championSpend, random, goons);

            GroupGoons(goons, constants.SquadSizeMin, constants.SquadSizeMax, random, results);
            AssignChampions(champions, constants.ChampionSoloChance, random, results);
        }

        private static int WholePoints(float points)
        {
            return Math.Max(0, (int)Math.Floor((float)points + RoundingSlack));
        }

        /// <returns>What was spent.</returns>
        private static int Buy(IReadOnlyList<EntityData> options, int budget, IRandomSource random,
            List<EntityData> bought)
        {
            var affordable = new List<EntityData>(options.Count);
            int left = budget;
            while (true)
            {
                affordable.Clear();
                foreach (EntityData option in options)
                {
                    int cost = FactionRoster.Cost(option);
                    if (cost > 0 && cost <= left)
                    {
                        affordable.Add(option);
                    }
                }

                if (affordable.Count == 0)
                {
                    return budget - left;
                }

                EntityData pick = affordable[Index(random.Value01(), affordable.Count)];
                bought.Add(pick);
                left -= FactionRoster.Cost(pick);
            }
        }

        /// <summary>
        /// Squads of a random size from min to max. Near the end it sizes them so none is left under the minimum,
        /// unless there were too few goons for even one full squad.
        /// </summary>
        private static void GroupGoons(List<EntityData> goons, int min, int max, IRandomSource random,
            List<SpawnSquad> results)
        {
            min = Math.Max(1, min);
            max = Math.Max(min, max);
            int next = 0;
            while (next < goons.Count)
            {
                int left = goons.Count - next;
                int size = min + Index(random.Value01(), max - min + 1);
                if (size >= left)
                {
                    size = left;
                }
                else if (left - size < min)
                {
                    size = left <= max ? left : left - min;
                }

                var squad = new SpawnSquad(null);
                squad.Goons.AddRange(goons.GetRange(next, size));
                results.Add(squad);
                next += size;
            }
        }

        private static void AssignChampions(List<EntityData> champions, float soloChance, IRandomSource random,
            List<SpawnSquad> squads)
        {
            var leaderless = new List<SpawnSquad>(squads);
            foreach (EntityData champion in champions)
            {
                bool solo = random.Value01() < soloChance;
                if (solo || leaderless.Count == 0)
                {
                    squads.Add(new SpawnSquad(champion));
                    continue;
                }

                int index = Index(random.Value01(), leaderless.Count);
                leaderless[index].Champion = champion;
                leaderless.RemoveAt(index);
            }
        }

        private static int Index(float roll, int count)
        {
            return Math.Min((int)(roll * count), count - 1);
        }
    }
}
