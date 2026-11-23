using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// One faction entity, from a row of entities.csv.
    /// </summary>
    public sealed class EntityData : DataEntry
    {
        [SerializeField] private FactionData faction;
        [SerializeField] private RankTier rankTier;
        [SerializeField] private bool hasMinRank;
        [SerializeField] private Rank minRank;
        [SerializeField] private bool hasBudgetCost;
        [SerializeField] private int budgetCost;
        [SerializeField] private DataValueList values = new DataValueList();

        public FactionData Faction => faction;
        public RankTier RankTier => rankTier;
        public DataValueList Values => values;

        /// <summary>The lowest mission rank it appears at. Summons have none.</summary>
        public bool TryGetMinRank(out Rank rank)
        {
            rank = minRank;
            return hasMinRank;
        }

        /// <summary>Its cost from the threat budget. Leaders and Fissure spawns are outside the budget.</summary>
        public bool TryGetBudgetCost(out int cost)
        {
            cost = budgetCost;
            return hasBudgetCost;
        }

        internal void Populate(
            FactionData newFaction, RankTier newRankTier, Rank? newMinRank, int? newBudgetCost, DataValueList newValues)
        {
            faction = newFaction;
            rankTier = newRankTier;
            hasMinRank = newMinRank.HasValue;
            minRank = newMinRank.GetValueOrDefault();
            hasBudgetCost = newBudgetCost.HasValue;
            budgetCost = newBudgetCost.GetValueOrDefault();
            values = newValues;
        }
    }
}
