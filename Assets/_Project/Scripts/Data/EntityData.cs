using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>A module an entity can't use below a mission rank (factions.md, Locked modules).</summary>
    [Serializable]
    public struct ModuleLock
    {
        [SerializeField] private ModuleData module;
        [SerializeField] private Rank rank;

        public ModuleLock(ModuleData module, Rank rank)
        {
            this.module = module;
            this.rank = rank;
        }

        public ModuleData Module => module;

        /// <summary>The lowest mission rank it's unlocked at.</summary>
        public Rank Rank => rank;
    }

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
        [SerializeField] private List<ModuleData> modules = new List<ModuleData>();
        [SerializeField] private List<ModuleLock> locks = new List<ModuleLock>();
        [SerializeField] private List<EntityTag> tags = new List<EntityTag>();

        public FactionData Faction => faction;
        public RankTier RankTier => rankTier;
        public DataValueList Values => values;

        /// <summary>Its modules in AI priority order, base actions such as Drag included.</summary>
        public IReadOnlyList<ModuleData> Modules => modules;

        /// <summary>Its own tags; the faction's tags are in <see cref="FactionData.Tags"/>.</summary>
        public IReadOnlyList<EntityTag> Tags => tags;

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

        /// <summary>Whether it spawns at the mission's rank: one with no minimum rank always does.</summary>
        /// <param name="difficultyLevel">Mission ranks above D (0 at D, 4 at S, 5 at S2).</param>
        public bool SpawnsAt(int difficultyLevel)
        {
            return !hasMinRank || RankRules.HasReached(difficultyLevel, minRank);
        }

        /// <summary>Whether a module of its list is unlocked at the mission's rank (factions.md, Locked modules).</summary>
        public bool IsUnlocked(ModuleData module, int difficultyLevel)
        {
            foreach (ModuleLock moduleLock in locks)
            {
                if (moduleLock.Module == module)
                {
                    return RankRules.HasReached(difficultyLevel, moduleLock.Rank);
                }
            }

            return true;
        }

        /// <summary>Whether it lists the module, such as Drag, which a base action's owner needs to know.</summary>
        public bool Lists(ModuleData module)
        {
            return modules.Contains(module);
        }

        internal void Populate(
            FactionData newFaction, RankTier newRankTier, Rank? newMinRank, int? newBudgetCost, DataValueList newValues,
            List<ModuleData> newModules, List<ModuleLock> newLocks, List<EntityTag> newTags)
        {
            faction = newFaction;
            rankTier = newRankTier;
            hasMinRank = newMinRank.HasValue;
            minRank = newMinRank.GetValueOrDefault();
            hasBudgetCost = newBudgetCost.HasValue;
            budgetCost = newBudgetCost.GetValueOrDefault();
            values = newValues;
            modules = newModules;
            locks = newLocks;
            tags = newTags;
        }
    }
}
