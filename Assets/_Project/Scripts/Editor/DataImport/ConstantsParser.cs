using System;
using System.Collections.Generic;
using Zenthium.Data;

namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// Reads constants.csv into the formula, entity, rank scaling and economy constants and the tier tables.
    /// Every constant the game uses must be present, and every name must be one it knows,
    /// so a typo can't go unnoticed.
    /// </summary>
    internal static class ConstantsParser
    {
        private const string GroupColumn = "group";
        private const string NameColumn = "name";
        private const string ValueColumn = "value";

        private const string FormulaGroup = "formula";
        private const string MirrorGroup = "mirror";
        private const string ReferenceGroup = "reference";
        private const string EconomyGroup = "economy";
        private const string TierGroup = "tier";

        private const string IncomeMixPrefix = "income_mix.";
        private const char NamePartSeparator = '.';

        public static void Parse(CsvTable table, ParsedDesignData data, List<string> errors)
        {
            var tiers = new TierTables();
            data.Tiers = tiers;
            if (!table.RequireColumns(errors, GroupColumn, NameColumn, ValueColumn))
            {
                return;
            }

            var named = new Dictionary<string, CsvRow>(StringComparer.Ordinal);
            var incomeMixes = new List<EconomyConstants.IncomeMix>();
            foreach (CsvRow row in table.Rows)
            {
                string group = row.Get(GroupColumn);
                string name = CellReader.Id(row, NameColumn, errors);
                if (name == null)
                {
                    continue;
                }

                switch (group)
                {
                    case TierGroup:
                        ReadTier(row, name, tiers, errors);
                        break;
                    case ReferenceGroup:
                        // Starting-hero ranges for balance.py's checks; the game doesn't use them.
                        break;
                    case FormulaGroup:
                    case MirrorGroup:
                    case EconomyGroup:
                        if (name.StartsWith(IncomeMixPrefix, StringComparison.Ordinal))
                        {
                            ReadIncomeMix(row, name, incomeMixes, errors);
                        }
                        else if (named.ContainsKey(name))
                        {
                            errors.Add($"{row.Location}: duplicate constant '{name}' (also on line {named[name].LineNumber}).");
                        }
                        else
                        {
                            named.Add(name, row);
                        }

                        break;
                    default:
                        errors.Add($"{row.Location}: unknown group '{group}'.");
                        break;
                }
            }

            var reader = new NamedConstants(table.FileName, named, errors);
            data.Formula = new FormulaConstants(
                hpPerToughness: reader.Float("hp_per_toughness"),
                leaderHpMultiplier: reader.Float("leader_hp_multiplier"),
                armorPerToughness: reader.Float("armor_per_toughness"),
                normalDamageBase: reader.Float("normal_damage_base"),
                normalDamagePerStat: reader.Float("normal_damage_per_stat"),
                kickCooldown: reader.Float("kick_cooldown"),
                minDamageFraction: reader.Float("min_damage_fraction"),
                dodgePerFinesse: reader.Float("dodge_per_finesse"),
                dodgeCap: reader.Float("dodge_cap"),
                visionRange: reader.Float("vision_range"),
                awarenessInstantRange: reader.Float("awareness_instant_range"),
                awarenessMaxTime: reader.Float("awareness_max_time"),
                resourceSecondsPerPoint: reader.Float("resource_seconds_per_point"),
                enemyDefaultDamageTier: reader.Tier("enemy_default_damage_tier", TierScale.Damage, tiers));
            data.Entity = new EntityConstants(
                baseStat: reader.Float("base_stat"),
                baseMoveSpeed: reader.Float("base_move_speed"),
                defianceChancePerWill: reader.Float("defiance_chance_per_will"),
                defianceRollCost: reader.Float("defiance_roll_cost"),
                defianceInvulnerablePlayer: reader.Float("defiance_invulnerable_player"),
                defianceInvulnerableOther: reader.Float("defiance_invulnerable_other"),
                resourceRegenPerSecond: reader.Float("resource_regen_per_second"));
            data.RankScaling = new RankScalingConstants(
                innateBoostGoon: reader.Float("innate_boost_goon"),
                innateBoostChampion: reader.Float("innate_boost_champion"),
                innateBoostLeader: reader.Float("innate_boost_leader"),
                statBonusPerRank: reader.Float("stat_bonus_per_rank"),
                hpDamageBonusPerRank: reader.Float("hp_damage_bonus_per_rank"));
            data.Economy = new EconomyConstants(
                incomeBasic: reader.Float("income_basic"),
                incomeGem: reader.Float("income_gem"),
                incomeMetallic: reader.Float("income_metallic"),
                statPriceIncreasePerPoint: reader.Float("stat_price_increase_per_point"),
                incomeMixes: incomeMixes);
            reader.ReportUnused();
        }

        /// <summary>A tier row's name is <c>scale.Word</c>, such as <c>damage.Light</c>.</summary>
        private static void ReadTier(CsvRow row, string name, TierTables tiers, List<string> errors)
        {
            int separator = name.IndexOf(NamePartSeparator);
            string scaleText = separator > 0 ? name.Substring(0, separator) : string.Empty;
            string word = separator > 0 ? name.Substring(separator + 1) : string.Empty;
            if (word.Length == 0 || !CellReader.TryParseEnum(scaleText, out TierScale scale))
            {
                errors.Add($"{row.Location}: tier '{name}' should be scale.Word, with a scale from: "
                    + string.Join(", ", Enum.GetNames(typeof(TierScale))) + ".");
                return;
            }

            float? value = CellReader.Float(row, ValueColumn, true, errors);
            if (value.HasValue && !tiers.TryAdd(scale, word, value.Value))
            {
                errors.Add($"{row.Location}: duplicate tier '{name}'.");
            }
        }

        /// <summary>An income mix row's name is <c>income_mix.MissionType.family</c>.</summary>
        private static void ReadIncomeMix(
            CsvRow row, string name, List<EconomyConstants.IncomeMix> incomeMixes, List<string> errors)
        {
            string[] parts = name.Substring(IncomeMixPrefix.Length).Split(NamePartSeparator);
            if (parts.Length != 2
                || !CellReader.TryParseEnum(parts[0], out MissionType missionType)
                || !CellReader.TryParseEnum(parts[1], out ZennyFamily family))
            {
                errors.Add($"{row.Location}: '{name}' should be {IncomeMixPrefix}MissionType.family, with a mission type from: "
                    + string.Join(", ", Enum.GetNames(typeof(MissionType))) + ", and a family from: "
                    + string.Join(", ", Enum.GetNames(typeof(ZennyFamily))) + ".");
                return;
            }

            foreach (EconomyConstants.IncomeMix existing in incomeMixes)
            {
                if (existing.MissionType == missionType && existing.Family == family)
                {
                    errors.Add($"{row.Location}: duplicate income mix '{name}'.");
                    return;
                }
            }

            float? multiplier = CellReader.Float(row, ValueColumn, true, errors);
            if (multiplier.HasValue)
            {
                incomeMixes.Add(new EconomyConstants.IncomeMix(missionType, family, multiplier.Value));
            }
        }

        /// <summary>
        /// Hands out named constants and remembers which were used, to report missing and unknown names.
        /// </summary>
        private sealed class NamedConstants
        {
            private readonly string fileName;
            private readonly Dictionary<string, CsvRow> rows;
            private readonly List<string> errors;
            private readonly HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);

            public NamedConstants(string fileName, Dictionary<string, CsvRow> rows, List<string> errors)
            {
                this.fileName = fileName;
                this.rows = rows;
                this.errors = errors;
            }

            public float Float(string name)
            {
                return TryTake(name, out CsvRow row)
                    ? CellReader.Float(row, ValueColumn, true, errors).GetValueOrDefault()
                    : 0f;
            }

            public TierValue Tier(string name, TierScale scale, TierTables tiers)
            {
                if (!TryTake(name, out CsvRow row))
                {
                    return default;
                }

                if (row.Get(ValueColumn).Length == 0)
                {
                    errors.Add($"{row.Location}: '{name}' is blank.");
                    return default;
                }

                return CellReader.Tier(row, ValueColumn, scale, tiers, errors);
            }

            public void ReportUnused()
            {
                foreach (KeyValuePair<string, CsvRow> pair in rows)
                {
                    if (!used.Contains(pair.Key))
                    {
                        errors.Add($"{pair.Value.Location}: unknown constant '{pair.Key}'. "
                            + $"If the game needs it, add it to {nameof(ConstantsParser)}.");
                    }
                }
            }

            private bool TryTake(string name, out CsvRow row)
            {
                used.Add(name);
                if (rows.TryGetValue(name, out row))
                {
                    return true;
                }

                errors.Add($"{fileName}: missing the constant '{name}'.");
                return false;
            }
        }
    }
}
