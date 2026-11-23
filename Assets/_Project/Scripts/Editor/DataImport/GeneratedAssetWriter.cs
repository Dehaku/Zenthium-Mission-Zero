using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Zenthium.Data;

namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// Creates or updates the generated data assets. Existing assets are found by their stored id and
    /// updated in place, so references to them in prefabs and other assets survive a re-import.
    /// </summary>
    internal static class GeneratedAssetWriter
    {
        private const string RootFolder = "Assets/_Project/Data/Generated";
        private const string DatabasePath = RootFolder + "/GameDatabase.asset";
        private const string ModulesFolder = RootFolder + "/Modules";
        private const string FactionsFolder = RootFolder + "/Factions";
        private const string EntitiesFolder = RootFolder + "/Entities";
        private const string HeroesFolder = RootFolder + "/Heroes";
        private const string PropsFolder = RootFolder + "/Props";
        private const string PricesFolder = RootFolder + "/Prices";
        private const string AssetExtension = ".asset";

        /// <param name="warnings">Assets left out of the database, such as ones whose row was removed.</param>
        public static void Write(ParsedDesignData data, List<string> warnings)
        {
            Dictionary<string, ModuleData> modules = SyncAssets<ModuleData>(ModulesFolder, Ids(data.Modules, m => m.Id), warnings);
            Dictionary<string, FactionData> factions = SyncAssets<FactionData>(FactionsFolder, data.FactionIds, warnings);
            Dictionary<string, EntityData> entities = SyncAssets<EntityData>(EntitiesFolder, Ids(data.Entities, e => e.Id), warnings);
            Dictionary<string, HeroData> heroes = SyncAssets<HeroData>(HeroesFolder, Ids(data.Heroes, h => h.Id), warnings);
            Dictionary<string, PropData> props = SyncAssets<PropData>(PropsFolder, Ids(data.Props, p => p.Id), warnings);
            Dictionary<string, PriceData> prices = SyncAssets<PriceData>(PricesFolder, Ids(data.Prices, p => p.Id), warnings);

            foreach (ModuleRecord record in data.Modules)
            {
                ModuleData asset = modules[record.Id];
                asset.Populate(
                    record.Category,
                    record.Stat,
                    record.CooldownSeconds,
                    record.Resource,
                    record.ResourceCost,
                    record.DamageTier,
                    record.UtilityTier,
                    record.DurationTier,
                    record.RadiusTier,
                    record.KnockbackTier,
                    record.RangeTier,
                    BuildValues(record.Values, modules));
                EditorUtility.SetDirty(asset);
            }

            foreach (EntityRecord record in data.Entities)
            {
                EntityData asset = entities[record.Id];
                asset.Populate(factions[record.FactionId], record.RankTier, record.MinRank, record.BudgetCost,
                    BuildValues(record.Values, modules));
                EditorUtility.SetDirty(asset);
            }

            foreach (HeroRecord record in data.Heroes)
            {
                HeroData asset = heroes[record.Id];
                asset.Populate(record.Strength, record.Will, record.Intelligence, record.Finesse, record.Toughness,
                    record.IsStarter);
                EditorUtility.SetDirty(asset);
            }

            foreach (PropRecord record in data.Props)
            {
                PropData asset = props[record.Id];
                asset.Populate(record.Size, record.StrengthNeeded, record.Hp, record.ThrownDamageXKick, record.Armor,
                    record.DamageReduction, record.ExplosionDamage, record.ExplosionRadius);
                EditorUtility.SetDirty(asset);
            }

            foreach (PriceRecord record in data.Prices)
            {
                PriceData asset = prices[record.Id];
                asset.Populate(record.Missions, record.Currency);
                EditorUtility.SetDirty(asset);
            }

            GameDatabase database = LoadOrCreateDatabase();
            database.Populate(
                data.Formula,
                data.RankScaling,
                data.Economy,
                data.Tiers,
                InFileOrder(data.Modules, m => m.Id, modules),
                InFileOrder(data.FactionIds, id => id, factions),
                InFileOrder(data.Entities, e => e.Id, entities),
                InFileOrder(data.Heroes, h => h.Id, heroes),
                InFileOrder(data.Props, p => p.Id, props),
                InFileOrder(data.Prices, p => p.Id, prices));
            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();
        }

        /// <summary>Finds or creates one asset per id in the folder, and warns about assets whose id is gone.</summary>
        private static Dictionary<string, T> SyncAssets<T>(string folder, IEnumerable<string> ids, List<string> warnings)
            where T : DataEntry
        {
            EnsureFolder(folder);

            var existing = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(asset.Id))
                {
                    warnings.Add($"{path} has no id, so it was left out. Generated assets are made by the importer only.");
                }
                else if (existing.ContainsKey(asset.Id))
                {
                    warnings.Add($"{path} has the same id '{asset.Id}' as {AssetDatabase.GetAssetPath(existing[asset.Id])}, so it was left out.");
                }
                else
                {
                    existing.Add(asset.Id, asset);
                }
            }

            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (string id in ids)
            {
                if (!existing.TryGetValue(id, out T asset))
                {
                    asset = ScriptableObject.CreateInstance<T>();
                    asset.SetId(id);
                    IdentifierFormatter.TryFormat(id, out string fileName);
                    AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{fileName}{AssetExtension}"));
                }

                result.Add(id, asset);
            }

            foreach (KeyValuePair<string, T> pair in existing)
            {
                if (!result.ContainsKey(pair.Key))
                {
                    warnings.Add($"{AssetDatabase.GetAssetPath(pair.Value)}: '{pair.Key}' is no longer in the CSVs. "
                        + "The asset was kept but left out of the database; delete it if nothing uses it.");
                }
            }

            return result;
        }

        private static GameDatabase LoadOrCreateDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<GameDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            return database;
        }

        private static DataValueList BuildValues(List<ValueRecord> records, Dictionary<string, ModuleData> modules)
        {
            var values = new List<DataValue>(records.Count);
            foreach (ValueRecord record in records)
            {
                switch (record.Kind)
                {
                    case DataValueKind.Number:
                        values.Add(DataValue.FromNumber(record.Key, record.Number));
                        break;
                    case DataValueKind.Range:
                        values.Add(DataValue.FromRange(record.Key, record.RangeMin, record.RangeMax));
                        break;
                    case DataValueKind.Tier:
                        values.Add(DataValue.FromTier(record.Key, record.Tier));
                        break;
                    case DataValueKind.Module:
                        values.Add(DataValue.FromModule(record.Key, modules[record.ModuleId]));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(records), record.Kind, null);
                }
            }

            return new DataValueList(values);
        }

        private static List<T> InFileOrder<TRecord, T>(List<TRecord> records, Func<TRecord, string> getId, Dictionary<string, T> assets)
        {
            var ordered = new List<T>(records.Count);
            foreach (TRecord record in records)
            {
                ordered.Add(assets[getId(record)]);
            }

            return ordered;
        }

        private static IEnumerable<string> Ids<TRecord>(List<TRecord> records, Func<TRecord, string> getId)
        {
            foreach (TRecord record in records)
            {
                yield return getId(record);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            int slash = folder.LastIndexOf('/');
            string parent = folder.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(slash + 1));
        }
    }
}
