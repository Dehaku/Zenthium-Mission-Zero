using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Zenthium.Data;

namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// Parses a <c>values</c> cell of <c>key=value; key=value</c> pairs. A value is a number,
    /// a range such as <c>3-5</c>, or, for the keys listed here, a tier word or a module id.
    /// </summary>
    internal static class ValueParser
    {
        // The values column can't say which scale a tier word belongs to ("Short" is a duration,
        // a cooldown and a range), so keys whose values are tier words are listed with their scale.
        private static readonly Dictionary<string, TierScale> TierKeys = new Dictionary<string, TierScale>(StringComparer.Ordinal)
        {
            { "tower_stun", TierScale.Duration },
            { "lovers_hypnotize", TierScale.Duration },
            { "hanged_man_root", TierScale.Duration },
            { "death_bonus", TierScale.Damage },
        };

        // Keys whose values name another module in modules.csv.
        private static readonly HashSet<string> ModuleKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "summon_attack",
        };

        // Checked after plain numbers, so a negative number such as -0.5 isn't read as a range.
        private static readonly Regex RangePattern = new Regex(@"^(\d+(?:\.\d+)?)\s*-\s*(\d+(?:\.\d+)?)$");

        private const char PairSeparator = ';';
        private const char KeyValueSeparator = '=';

        public static List<ValueRecord> Parse(CsvRow row, string column, TierTables tiers, List<string> errors)
        {
            var values = new List<ValueRecord>();
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string part in row.Get(column).Split(PairSeparator))
            {
                string pair = part.Trim();
                if (pair.Length == 0)
                {
                    continue;
                }

                int separator = pair.IndexOf(KeyValueSeparator);
                string key = separator > 0 ? pair.Substring(0, separator).Trim() : string.Empty;
                string text = separator > 0 ? pair.Substring(separator + 1).Trim() : string.Empty;
                if (key.Length == 0 || text.Length == 0)
                {
                    errors.Add($"{row.Location}: '{pair}' in {column} isn't a key=value pair.");
                    continue;
                }

                if (!seenKeys.Add(key))
                {
                    errors.Add($"{row.Location}: the key '{key}' appears twice in {column}.");
                    continue;
                }

                var value = new ValueRecord { Key = key, Location = row.Location };
                if (ReadValue(row, key, text, value, tiers, errors))
                {
                    values.Add(value);
                }
            }

            return values;
        }

        private static bool ReadValue(
            CsvRow row, string key, string text, ValueRecord value, TierTables tiers, List<string> errors)
        {
            if (TierKeys.TryGetValue(key, out TierScale scale))
            {
                if (!tiers.TryGet(scale, text, out TierValue tier))
                {
                    errors.Add($"{row.Location}: {key} '{text}' isn't a {scale} tier in {DesignDataSource.ConstantsFile}.");
                    return false;
                }

                value.Kind = DataValueKind.Tier;
                value.Tier = tier;
                return true;
            }

            if (ModuleKeys.Contains(key))
            {
                // Whether the module exists is checked once every module has been read.
                value.Kind = DataValueKind.Module;
                value.ModuleId = text;
                return true;
            }

            if (CellReader.TryParseFloat(text, out float number))
            {
                value.Kind = DataValueKind.Number;
                value.Number = number;
                return true;
            }

            Match range = RangePattern.Match(text);
            if (range.Success
                && CellReader.TryParseFloat(range.Groups[1].Value, out float min)
                && CellReader.TryParseFloat(range.Groups[2].Value, out float max)
                && min <= max)
            {
                value.Kind = DataValueKind.Range;
                value.RangeMin = min;
                value.RangeMax = max;
                return true;
            }

            errors.Add($"{row.Location}: {key} '{text}' isn't a number or a low-high range. "
                + $"If it's a tier word or a module, add the key to {nameof(ValueParser)}.");
            return false;
        }
    }
}
