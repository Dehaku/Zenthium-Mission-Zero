using System;
using System.Collections.Generic;
using Zenthium.Data;

namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// Parses modules.csv's <c>tags</c> cell, such as <c>Ranged; Debuff; Resist:Will</c>. Plain tags are
    /// <see cref="ModuleTag"/> words; <c>Resist:&lt;Stat&gt;</c> and <c>NoRegen:&lt;Resource&gt;</c> are read into
    /// their own lists. Unknown tags are errors.
    /// </summary>
    internal static class ModuleTagParser
    {
        private const string ResistPrefix = "Resist";
        private const string NoRegenPrefix = "NoRegen";
        private const char ParameterSeparator = ':';

        public static void Parse(CsvRow row, string column, ModuleRecord record, List<string> errors)
        {
            record.Tags = new List<ModuleTag>();
            record.ResistStats = new List<Stat>();
            record.NoRegenResources = new List<ResourceType>();

            foreach (string tag in CellReader.ListItems(row, column))
            {
                int separator = tag.IndexOf(ParameterSeparator);
                if (separator < 0)
                {
                    if (CellReader.TryParseEnum(tag, out ModuleTag plain))
                    {
                        AddOnce(record.Tags, plain);
                    }
                    else
                    {
                        errors.Add($"{row.Location}: {column} '{tag}' isn't one of: "
                            + $"{string.Join(", ", Enum.GetNames(typeof(ModuleTag)))}, "
                            + $"{ResistPrefix}{ParameterSeparator}<Stat>, {NoRegenPrefix}{ParameterSeparator}<Resource>.");
                    }

                    continue;
                }

                string prefix = tag.Substring(0, separator).Trim();
                string parameter = tag.Substring(separator + 1).Trim();
                if (string.Equals(prefix, ResistPrefix, StringComparison.Ordinal)
                    && CellReader.TryParseEnum(parameter, out Stat stat))
                {
                    AddOnce(record.ResistStats, stat);
                }
                else if (string.Equals(prefix, NoRegenPrefix, StringComparison.Ordinal)
                    && CellReader.TryParseEnum(parameter, out ResourceType resource))
                {
                    AddOnce(record.NoRegenResources, resource);
                }
                else
                {
                    errors.Add($"{row.Location}: {column} '{tag}' should be {ResistPrefix}{ParameterSeparator}<Stat> "
                        + $"or {NoRegenPrefix}{ParameterSeparator}<Resource>.");
                }
            }
        }

        private static void AddOnce<T>(List<T> list, T value)
        {
            if (!list.Contains(value))
            {
                list.Add(value);
            }
        }
    }
}
