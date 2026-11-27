using System.Collections.Generic;
using Zenthium.Data;

namespace Zenthium.Missions
{
    /// <summary>
    /// What a faction can field at a mission's rank (factions.md, Minimum ranks): the goons and Champions the threat
    /// budget can buy, and its Leader, who is outside the budget (enemies.md, Threat budget).
    /// </summary>
    public sealed class FactionRoster
    {
        private readonly List<EntityData> goons = new List<EntityData>();
        private readonly List<EntityData> champions = new List<EntityData>();

        /// <param name="entities">Every entity in the game data; only the faction's are kept.</param>
        public FactionRoster(FactionData faction, IReadOnlyList<EntityData> entities, int difficultyLevel)
        {
            foreach (EntityData entity in entities)
            {
                if (entity == null || entity.Faction != faction || !entity.SpawnsAt(difficultyLevel))
                {
                    continue;
                }

                if (entity.RankTier == RankTier.Leader)
                {
                    if (Leader == null)
                    {
                        Leader = entity;
                    }
                }
                else if (entity.TryGetBudgetCost(out int cost) && cost > 0)
                {
                    (entity.RankTier == RankTier.Champion ? champions : goons).Add(entity);
                }
            }
        }

        /// <summary>Goons with a budget cost that spawn at the rank.</summary>
        public IReadOnlyList<EntityData> Goons => goons;

        /// <summary>Champions with a budget cost that spawn at the rank.</summary>
        public IReadOnlyList<EntityData> Champions => champions;

        /// <summary>Null if the faction has no Leader at the rank.</summary>
        public EntityData Leader { get; }

        /// <summary>The cost of an entity from the roster.</summary>
        public static int Cost(EntityData entity)
        {
            return entity.TryGetBudgetCost(out int cost) ? cost : 0;
        }
    }
}
