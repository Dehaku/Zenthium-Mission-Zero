using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Builds faction and entity data in memory through the importer's own Populate, as <see cref="TestModules"/>
    /// does for modules.
    /// </summary>
    internal static class TestFactions
    {
        public static FactionData Faction(string id, params EntityTag[] tags)
        {
            var faction = ScriptableObject.CreateInstance<FactionData>();
            faction.SetId(id);
            faction.Populate(new List<EntityTag>(tags), null, new DataValueList(), new DataValueList(),
                new DataValueList());
            return faction;
        }

        public static FactionData FactionWithValues(string id, params (string Key, float Value)[] values)
        {
            var faction = ScriptableObject.CreateInstance<FactionData>();
            faction.SetId(id);
            var list = new List<DataValue>();
            foreach ((string key, float value) in values)
            {
                list.Add(DataValue.FromNumber(key, value));
            }

            faction.Populate(new List<EntityTag>(), null, new DataValueList(), new DataValueList(),
                new DataValueList(list));
            return faction;
        }

        /// <param name="missionWeights">Mission Weights, keyed as in factions.csv, such as <c>rescue</c>.</param>
        /// <param name="locationWeights">Location Weights, keyed as in factions.csv, such as <c>bank</c>.</param>
        public static FactionData FactionWithWeights(string id, (string Key, float Value)[] missionWeights,
            (string Key, float Value)[] locationWeights)
        {
            var faction = ScriptableObject.CreateInstance<FactionData>();
            faction.SetId(id);
            faction.Populate(new List<EntityTag>(), null, Values(missionWeights), Values(locationWeights),
                new DataValueList());
            return faction;
        }

        public static EntityData Entity(string id, FactionData faction, RankTier tier, Rank? minRank = Rank.D,
            ModuleData[] modules = null, ModuleLock[] locks = null, EntityTag[] tags = null, int? budgetCost = null)
        {
            var entity = ScriptableObject.CreateInstance<EntityData>();
            entity.SetId(id);
            entity.Populate(faction, tier, minRank, budgetCost, new DataValueList(),
                new List<ModuleData>(modules ?? new ModuleData[0]), new List<ModuleLock>(locks ?? new ModuleLock[0]),
                new List<EntityTag>(tags ?? new EntityTag[0]));
            return entity;
        }

        private static DataValueList Values((string Key, float Value)[] values)
        {
            var list = new List<DataValue>();
            foreach ((string key, float value) in values)
            {
                list.Add(DataValue.FromNumber(key, value));
            }

            return new DataValueList(list);
        }
    }
}
