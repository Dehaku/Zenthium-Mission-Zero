using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Zenthium.Data;

namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// Reads typed values from CSV cells. Problems are added to the error list and a default is
    /// returned, so a file reports all its errors in one import.
    /// </summary>
    internal static class CellReader
    {
        private const char ListSeparator = ';';

        /// <returns>The id, or null if the cell is blank.</returns>
        public static string Id(CsvRow row, string column, List<string> errors)
        {
            string text = row.Get(column);
            if (text.Length == 0)
            {
                errors.Add($"{row.Location}: '{column}' is blank.");
                return null;
            }

            return text;
        }

        /// <returns>Null if the cell is blank or not a number.</returns>
        public static float? Float(CsvRow row, string column, bool required, List<string> errors)
        {
            string text = row.Get(column);
            if (text.Length == 0)
            {
                ReportIfRequired(row, column, required, errors);
                return null;
            }

            if (TryParseFloat(text, out float value))
            {
                return value;
            }

            errors.Add($"{row.Location}: {column} '{text}' isn't a number.");
            return null;
        }

        /// <returns>Null if the cell is blank or not a whole number.</returns>
        public static int? Int(CsvRow row, string column, bool required, List<string> errors)
        {
            string text = row.Get(column);
            if (text.Length == 0)
            {
                ReportIfRequired(row, column, required, errors);
                return null;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }

            errors.Add($"{row.Location}: {column} '{text}' isn't a whole number.");
            return null;
        }

        /// <returns>Null if the cell is blank or not one of the enum's words.</returns>
        public static T? EnumValue<T>(CsvRow row, string column, bool required, List<string> errors) where T : struct, Enum
        {
            string text = row.Get(column);
            if (text.Length == 0)
            {
                ReportIfRequired(row, column, required, errors);
                return null;
            }

            if (TryParseEnum(text, out T value))
            {
                return value;
            }

            errors.Add($"{row.Location}: {column} '{text}' isn't one of: {string.Join(", ", System.Enum.GetNames(typeof(T)))}.");
            return null;
        }

        /// <returns>An unset tier if the cell is blank or the word is unknown.</returns>
        public static TierValue Tier(CsvRow row, string column, TierScale scale, TierTables tiers, List<string> errors)
        {
            string word = row.Get(column);
            if (word.Length == 0)
            {
                return default;
            }

            if (tiers.TryGet(scale, word, out TierValue tier))
            {
                return tier;
            }

            errors.Add($"{row.Location}: {column} '{word}' isn't a {scale} tier in {DesignDataSource.ConstantsFile}.");
            return default;
        }

        /// <summary>The items of a <c>;</c>-separated list cell, trimmed, with blanks left out.</summary>
        public static List<string> ListItems(CsvRow row, string column)
        {
            var items = new List<string>();
            foreach (string part in row.Get(column).Split(ListSeparator))
            {
                string item = part.Trim();
                if (item.Length > 0)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        /// <summary>Invariant culture, so a comma decimal separator on this PC can't change the numbers.</summary>
        public static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.IsNaN(value)
                && !float.IsInfinity(value);
        }

        /// <summary>Matches ignoring case, spaces and punctuation, so "Gear (active)" is GearActive.</summary>
        public static bool TryParseEnum<T>(string text, out T value) where T : struct, Enum
        {
            string wanted = LettersAndDigits(text);
            if (wanted.Length > 0)
            {
                foreach (T candidate in (T[])System.Enum.GetValues(typeof(T)))
                {
                    if (string.Equals(LettersAndDigits(candidate.ToString()), wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        value = candidate;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static void ReportIfRequired(CsvRow row, string column, bool required, List<string> errors)
        {
            if (required)
            {
                errors.Add($"{row.Location}: '{column}' is blank.");
            }
        }

        private static string LettersAndDigits(string text)
        {
            var result = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}
