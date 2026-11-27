using System;
using System.Collections.Generic;
using Zenthium.Data;

namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// Turns the design CSVs into checked records. Any error (unknown tier word, duplicate id,
    /// missing reference, bad cell) fails the whole parse, so nothing partial is ever written.
    /// </summary>
    public static class DesignDataParser
    {
        private const string ModuleColumn = "module";
        private const string CategoryColumn = "category";
        private const string StatColumn = "stat";
        private const string DamageTierColumn = "damage_tier";
        private const string CooldownColumn = "cooldown_s";
        private const string ResourceColumn = "resource";
        private const string CostColumn = "cost";
        private const string UtilityColumn = "utility";
        private const string DurationTierColumn = "duration_tier";
        private const string RadiusTierColumn = "radius_tier";
        private const string KnockbackTierColumn = "knockback_tier";
        private const string RangeTierColumn = "range_tier";
        private const string ValuesColumn = "values";
        private const string DamageTypeColumn = "damage_type";
        private const string TagsColumn = "tags";

        private const string FactionColumn = "faction";
        private const string EntityColumn = "entity";
        private const string RankTierColumn = "rank_tier";
        private const string MinRankColumn = "min_rank";
        private const string BudgetCostColumn = "budget_cost";
        private const string ModulesColumn = "modules";
        private const string LockedUntilColumn = "locked_until";
        private const char LockSeparator = ':';

        private const string ArrivalColumn = "arrival";
        private const string MissionWeightsColumn = "mission_weights";
        private const string LocationWeightsColumn = "location_weights";

        private const string HeroColumn = "hero";
        private const string StrengthColumn = "strength";
        private const string WillColumn = "will";
        private const string IntelligenceColumn = "intelligence";
        private const string FinesseColumn = "finesse";
        private const string ToughnessColumn = "toughness";
        private const string StarterColumn = "starter";
        private const string StarterYes = "yes";
        private const string ActivesColumn = "actives";
        private const string PassivesColumn = "passives";
        private const string GearColumn = "gear";

        private const string PropNameColumn = "name";
        private const string SizeColumn = "size";
        private const string StrengthNeededColumn = "strength_needed";
        private const string HpColumn = "hp";
        private const string ThrownXKickColumn = "thrown_x_kick";
        private const string ArmorColumn = "armor";
        private const string DamageReductionColumn = "damage_reduction";
        private const string ExplosionDamageColumn = "explosion_damage";
        private const string ExplosionRadiusColumn = "explosion_radius_m";

        private const string PurchaseColumn = "purchase";
        private const string MissionsColumn = "missions";
        private const string CurrencyColumn = "currency";

        public static DesignDataParseResult Parse(DesignDataSource source)
        {
            var errors = new List<string>();
            var data = new ParsedDesignData();

            CsvTable constants = CsvTable.Parse(DesignDataSource.ConstantsFile, source.Constants, errors);
            if (constants != null)
            {
                ConstantsParser.Parse(constants, data, errors);
            }

            TierTables tiers = data.Tiers ?? new TierTables();
            ReadModules(CsvTable.Parse(DesignDataSource.ModulesFile, source.Modules, errors), tiers, data, errors);
            ReadEntities(CsvTable.Parse(DesignDataSource.EntitiesFile, source.Entities, errors), tiers, data, errors);
            ReadHeroes(CsvTable.Parse(DesignDataSource.HeroesFile, source.Heroes, errors), data, errors);
            ReadProps(CsvTable.Parse(DesignDataSource.PropsFile, source.Props, errors), data, errors);
            ReadPrices(CsvTable.Parse(DesignDataSource.PricesFile, source.Prices, errors), data, errors);
            ReadFactions(CsvTable.Parse(DesignDataSource.FactionsFile, source.Factions, errors), tiers, data, errors);
            CheckModuleReferences(data, errors);

            return new DesignDataParseResult(errors.Count == 0 ? data : null, errors);
        }

        private static void ReadModules(CsvTable table, TierTables tiers, ParsedDesignData data, List<string> errors)
        {
            if (table == null || !table.RequireColumns(errors, ModuleColumn, CategoryColumn, StatColumn, DamageTierColumn,
                    CooldownColumn, ResourceColumn, CostColumn, UtilityColumn, DurationTierColumn, RadiusTierColumn,
                    KnockbackTierColumn, RangeTierColumn, ValuesColumn, DamageTypeColumn, TagsColumn))
            {
                return;
            }

            var ids = new IdRegistry(errors);
            foreach (CsvRow row in table.Rows)
            {
                string id = CellReader.Id(row, ModuleColumn, errors);
                if (id == null || !ids.Claim(row, id))
                {
                    continue;
                }

                if ((row.Get(ResourceColumn).Length == 0) != (row.Get(CostColumn).Length == 0))
                {
                    errors.Add($"{row.Location}: {ResourceColumn} and {CostColumn} must both be set or both be blank.");
                }

                var record = new ModuleRecord
                {
                    Id = id,
                    Category = CellReader.EnumValue<ModuleCategory>(row, CategoryColumn, true, errors).GetValueOrDefault(),
                    Stat = CellReader.EnumValue<Stat>(row, StatColumn, false, errors),
                    CooldownSeconds = CellReader.Float(row, CooldownColumn, false, errors),
                    Resource = CellReader.EnumValue<ResourceType>(row, ResourceColumn, false, errors),
                    ResourceCost = CellReader.Float(row, CostColumn, false, errors).GetValueOrDefault(),
                    DamageTier = CellReader.Tier(row, DamageTierColumn, TierScale.Damage, tiers, errors),
                    UtilityTier = CellReader.Tier(row, UtilityColumn, TierScale.Utility, tiers, errors),
                    DurationTier = CellReader.Tier(row, DurationTierColumn, TierScale.Duration, tiers, errors),
                    RadiusTier = CellReader.Tier(row, RadiusTierColumn, TierScale.Radius, tiers, errors),
                    KnockbackTier = CellReader.Tier(row, KnockbackTierColumn, TierScale.Knockback, tiers, errors),
                    RangeTier = CellReader.Tier(row, RangeTierColumn, TierScale.Range, tiers, errors),
                    Values = ValueParser.Parse(row, ValuesColumn, tiers, errors),
                    DamageType = CellReader.EnumValue<DamageType>(row, DamageTypeColumn, false, errors),
                };
                ModuleTagParser.Parse(row, TagsColumn, record, errors);
                data.Modules.Add(record);
            }
        }

        private static void ReadEntities(CsvTable table, TierTables tiers, ParsedDesignData data, List<string> errors)
        {
            if (table == null || !table.RequireColumns(errors, FactionColumn, EntityColumn, RankTierColumn, MinRankColumn,
                    BudgetCostColumn, ValuesColumn, ModulesColumn, LockedUntilColumn, TagsColumn))
            {
                return;
            }

            var ids = new IdRegistry(errors);
            var factions = new HashSet<string>(StringComparer.Ordinal);
            foreach (CsvRow row in table.Rows)
            {
                string id = CellReader.Id(row, EntityColumn, errors);
                string faction = CellReader.Id(row, FactionColumn, errors);
                if (id == null || faction == null || !ids.Claim(row, id))
                {
                    continue;
                }

                if (factions.Add(faction))
                {
                    data.FactionIds.Add(faction);
                }

                data.Entities.Add(new EntityRecord
                {
                    Id = id,
                    FactionId = faction,
                    RankTier = CellReader.EnumValue<RankTier>(row, RankTierColumn, true, errors).GetValueOrDefault(),
                    MinRank = CellReader.EnumValue<Rank>(row, MinRankColumn, false, errors),
                    BudgetCost = CellReader.Int(row, BudgetCostColumn, false, errors),
                    Values = ValueParser.Parse(row, ValuesColumn, tiers, errors),
                    ModuleIds = CellReader.ListItems(row, ModulesColumn),
                    Locks = ReadLocks(row, errors),
                    Tags = EntityTagParser.Parse(row, TagsColumn, errors),
                    Location = row.Location,
                });
            }
        }

        /// <summary>A <c>locked_until</c> cell such as <c>Final Notice:C; Call for Backup:B</c>.</summary>
        private static List<ModuleLockRecord> ReadLocks(CsvRow row, List<string> errors)
        {
            var locks = new List<ModuleLockRecord>();
            foreach (string item in CellReader.ListItems(row, LockedUntilColumn))
            {
                int separator = item.LastIndexOf(LockSeparator);
                string moduleId = separator > 0 ? item.Substring(0, separator).Trim() : string.Empty;
                string rankText = separator > 0 ? item.Substring(separator + 1).Trim() : string.Empty;
                if (moduleId.Length == 0 || !CellReader.TryParseEnum(rankText, out Rank rank))
                {
                    errors.Add($"{row.Location}: {LockedUntilColumn} '{item}' should be <Module>{LockSeparator}<Rank>, "
                        + $"with a rank of {string.Join(", ", Enum.GetNames(typeof(Rank)))}.");
                    continue;
                }

                locks.Add(new ModuleLockRecord { ModuleId = moduleId, Rank = rank });
            }

            return locks;
        }

        private static void ReadFactions(CsvTable table, TierTables tiers, ParsedDesignData data, List<string> errors)
        {
            if (table == null || !table.RequireColumns(errors, FactionColumn, TagsColumn, ArrivalColumn,
                    MissionWeightsColumn, LocationWeightsColumn, ValuesColumn))
            {
                return;
            }

            var known = new HashSet<string>(data.FactionIds, StringComparer.Ordinal);
            var ids = new IdRegistry(errors);
            foreach (CsvRow row in table.Rows)
            {
                string id = CellReader.Id(row, FactionColumn, errors);
                if (id == null || !ids.Claim(row, id))
                {
                    continue;
                }

                if (!known.Contains(id))
                {
                    errors.Add($"{row.Location}: the faction '{id}' has no entities in {DesignDataSource.EntitiesFile}.");
                    continue;
                }

                data.Factions.Add(new FactionRecord
                {
                    Id = id,
                    Tags = EntityTagParser.Parse(row, TagsColumn, errors),
                    Arrival = CellReader.EnumValue<ArrivalTag>(row, ArrivalColumn, false, errors),
                    MissionWeights = ReadWeights(row, MissionWeightsColumn, tiers, errors),
                    LocationWeights = ReadWeights(row, LocationWeightsColumn, tiers, errors),
                    Values = ValueParser.Parse(row, ValuesColumn, tiers, errors),
                    Location = row.Location,
                });
            }
        }

        /// <summary>Relative weights are plain numbers, 0 or more (factions.md, Weights).</summary>
        private static List<ValueRecord> ReadWeights(CsvRow row, string column, TierTables tiers, List<string> errors)
        {
            List<ValueRecord> weights = ValueParser.Parse(row, column, tiers, errors);
            foreach (ValueRecord weight in weights)
            {
                if (weight.Kind != DataValueKind.Number || weight.Number < 0f)
                {
                    errors.Add($"{row.Location}: {column} {weight.Key} should be a number of 0 or more.");
                }
            }

            return weights;
        }

        private static void ReadHeroes(CsvTable table, ParsedDesignData data, List<string> errors)
        {
            if (table == null || !table.RequireColumns(errors, HeroColumn, StrengthColumn, WillColumn,
                    IntelligenceColumn, FinesseColumn, ToughnessColumn, StarterColumn, ActivesColumn, PassivesColumn,
                    GearColumn))
            {
                return;
            }

            var ids = new IdRegistry(errors);
            foreach (CsvRow row in table.Rows)
            {
                string id = CellReader.Id(row, HeroColumn, errors);
                if (id == null || !ids.Claim(row, id))
                {
                    continue;
                }

                string starter = row.Get(StarterColumn);
                bool isStarter = string.Equals(starter, StarterYes, StringComparison.OrdinalIgnoreCase);
                if (starter.Length > 0 && !isStarter)
                {
                    errors.Add($"{row.Location}: {StarterColumn} '{starter}' should be '{StarterYes}' or blank.");
                }

                data.Heroes.Add(new HeroRecord
                {
                    Id = id,
                    Strength = CellReader.Int(row, StrengthColumn, true, errors).GetValueOrDefault(),
                    Will = CellReader.Int(row, WillColumn, true, errors).GetValueOrDefault(),
                    Intelligence = CellReader.Int(row, IntelligenceColumn, true, errors).GetValueOrDefault(),
                    Finesse = CellReader.Int(row, FinesseColumn, true, errors).GetValueOrDefault(),
                    Toughness = CellReader.Int(row, ToughnessColumn, true, errors).GetValueOrDefault(),
                    IsStarter = isStarter,
                    ActiveIds = CellReader.ListItems(row, ActivesColumn),
                    PassiveIds = CellReader.ListItems(row, PassivesColumn),
                    GearIds = CellReader.ListItems(row, GearColumn),
                    Location = row.Location,
                });
            }
        }

        private static void ReadProps(CsvTable table, ParsedDesignData data, List<string> errors)
        {
            if (table == null || !table.RequireColumns(errors, PropNameColumn, SizeColumn, StrengthNeededColumn, HpColumn,
                    ThrownXKickColumn, ArmorColumn, DamageReductionColumn, ExplosionDamageColumn, ExplosionRadiusColumn))
            {
                return;
            }

            var ids = new IdRegistry(errors);
            foreach (CsvRow row in table.Rows)
            {
                string id = CellReader.Id(row, PropNameColumn, errors);
                if (id == null || !ids.Claim(row, id))
                {
                    continue;
                }

                data.Props.Add(new PropRecord
                {
                    Id = id,
                    Size = CellReader.EnumValue<PropSize>(row, SizeColumn, true, errors).GetValueOrDefault(),
                    StrengthNeeded = CellReader.Float(row, StrengthNeededColumn, true, errors).GetValueOrDefault(),
                    Hp = CellReader.Float(row, HpColumn, true, errors).GetValueOrDefault(),
                    ThrownDamageXKick = CellReader.Float(row, ThrownXKickColumn, true, errors).GetValueOrDefault(),
                    Armor = CellReader.Float(row, ArmorColumn, false, errors).GetValueOrDefault(),
                    DamageReduction = CellReader.Float(row, DamageReductionColumn, false, errors).GetValueOrDefault(),
                    ExplosionDamage = CellReader.Float(row, ExplosionDamageColumn, false, errors).GetValueOrDefault(),
                    ExplosionRadius = CellReader.Float(row, ExplosionRadiusColumn, false, errors).GetValueOrDefault(),
                });
            }
        }

        private static void ReadPrices(CsvTable table, ParsedDesignData data, List<string> errors)
        {
            if (table == null || !table.RequireColumns(errors, PurchaseColumn, MissionsColumn, CurrencyColumn))
            {
                return;
            }

            var ids = new IdRegistry(errors);
            foreach (CsvRow row in table.Rows)
            {
                string id = CellReader.Id(row, PurchaseColumn, errors);
                if (id == null || !ids.Claim(row, id))
                {
                    continue;
                }

                data.Prices.Add(new PriceRecord
                {
                    Id = id,
                    Missions = CellReader.Float(row, MissionsColumn, true, errors).GetValueOrDefault(),
                    Currency = CellReader.EnumValue<ZennyFamily>(row, CurrencyColumn, true, errors).GetValueOrDefault(),
                });
            }
        }

        private static void CheckModuleReferences(ParsedDesignData data, List<string> errors)
        {
            var modules = new Dictionary<string, ModuleRecord>(StringComparer.Ordinal);
            foreach (ModuleRecord module in data.Modules)
            {
                modules[module.Id] = module;
            }

            var moduleIds = new HashSet<string>(modules.Keys, StringComparer.Ordinal);
            foreach (ModuleRecord module in data.Modules)
            {
                CheckValues(module.Values, moduleIds, errors);
            }

            foreach (EntityRecord entity in data.Entities)
            {
                CheckValues(entity.Values, moduleIds, errors);
                CheckEntityModules(entity, moduleIds, errors);
            }

            foreach (FactionRecord faction in data.Factions)
            {
                CheckValues(faction.Values, moduleIds, errors);
            }

            // Without a valid slot count (its own error), only the count check is skipped.
            int? slotsPerType = data.ModuleConstants != null && data.ModuleConstants.SlotsPerType > 0
                ? data.ModuleConstants.SlotsPerType
                : (int?)null;
            foreach (HeroRecord hero in data.Heroes)
            {
                var kit = new HashSet<string>(StringComparer.Ordinal);
                CheckKit(hero, ActivesColumn, hero.ActiveIds, SlotType.Active, modules, kit, slotsPerType, errors);
                CheckKit(hero, PassivesColumn, hero.PassiveIds, SlotType.Passive, modules, kit, slotsPerType, errors);
                CheckKit(hero, GearColumn, hero.GearIds, SlotType.Gear, modules, kit, slotsPerType, errors);
            }
        }

        /// <summary>Each kit module must exist, fit the column's slot type, and appear once per hero (heroes.md, Slots).</summary>
        private static void CheckKit(HeroRecord hero, string column, List<string> ids, SlotType slotType,
            Dictionary<string, ModuleRecord> modules, HashSet<string> kit, int? slotsPerType, List<string> errors)
        {
            if (slotsPerType.HasValue && ids.Count > slotsPerType.Value)
            {
                errors.Add($"{hero.Location}: {column} lists {ids.Count} modules, but there are only "
                    + $"{slotsPerType.Value} slots per type.");
            }

            foreach (string id in ids)
            {
                if (!modules.TryGetValue(id, out ModuleRecord module))
                {
                    errors.Add($"{hero.Location}: {column} names the module '{id}', "
                        + $"which isn't in {DesignDataSource.ModulesFile}.");
                }
                else if (!ModuleCategories.TryGetSlotType(module.Category, out SlotType fits) || fits != slotType)
                {
                    errors.Add($"{hero.Location}: {column} names '{id}', a {module.Category} module, "
                        + $"which doesn't go in a {slotType} slot.");
                }

                if (!kit.Add(id))
                {
                    errors.Add($"{hero.Location}: the kit lists '{id}' twice; a hero can't equip two copies.");
                }
            }
        }

        /// <summary>Each listed module exists once, and each lock names one of them (factions.md, Locked modules).</summary>
        private static void CheckEntityModules(EntityRecord entity, HashSet<string> moduleIds, List<string> errors)
        {
            var listed = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in entity.ModuleIds)
            {
                if (!moduleIds.Contains(id))
                {
                    errors.Add($"{entity.Location}: {ModulesColumn} names the module '{id}', "
                        + $"which isn't in {DesignDataSource.ModulesFile}.");
                }

                if (!listed.Add(id))
                {
                    errors.Add($"{entity.Location}: {ModulesColumn} lists '{id}' twice.");
                }
            }

            var locked = new HashSet<string>(StringComparer.Ordinal);
            foreach (ModuleLockRecord moduleLock in entity.Locks)
            {
                if (!listed.Contains(moduleLock.ModuleId))
                {
                    errors.Add($"{entity.Location}: {LockedUntilColumn} locks '{moduleLock.ModuleId}', "
                        + $"which isn't in its {ModulesColumn}.");
                }

                if (!locked.Add(moduleLock.ModuleId))
                {
                    errors.Add($"{entity.Location}: {LockedUntilColumn} locks '{moduleLock.ModuleId}' twice.");
                }
            }
        }

        private static void CheckValues(List<ValueRecord> values, HashSet<string> moduleIds, List<string> errors)
        {
            foreach (ValueRecord value in values)
            {
                if (value.Kind == DataValueKind.Module && !moduleIds.Contains(value.ModuleId))
                {
                    errors.Add($"{value.Location}: {value.Key} names the module '{value.ModuleId}', "
                        + $"which isn't in {DesignDataSource.ModulesFile}.");
                }
            }
        }

        /// <summary>
        /// Reports ids used twice in one file.
        /// </summary>
        private sealed class IdRegistry
        {
            private readonly Dictionary<string, CsvRow> claimed = new Dictionary<string, CsvRow>(StringComparer.Ordinal);
            private readonly List<string> errors;

            public IdRegistry(List<string> errors)
            {
                this.errors = errors;
            }

            /// <returns>False if the id was already used.</returns>
            public bool Claim(CsvRow row, string id)
            {
                if (claimed.TryGetValue(id, out CsvRow first))
                {
                    errors.Add($"{row.Location}: duplicate id '{id}' (first on line {first.LineNumber}).");
                    return false;
                }

                claimed.Add(id, row);
                return true;
            }
        }
    }
}
