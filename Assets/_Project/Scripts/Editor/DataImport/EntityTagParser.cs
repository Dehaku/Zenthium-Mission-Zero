using System;
using System.Collections.Generic;
using Zenthium.Data;

namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// Parses an entity <c>tags</c> cell, such as <c>Coward; Hackable</c> or <c>LootDestination:CropCircle</c>. A
    /// parameterized tag is one <see cref="EntityTag"/> value per parameter, so it matches with the colon left out.
    /// Unknown tags are errors.
    /// </summary>
    internal static class EntityTagParser
    {
        public static List<EntityTag> Parse(CsvRow row, string column, List<string> errors)
        {
            var tags = new List<EntityTag>();
            foreach (string text in CellReader.ListItems(row, column))
            {
                if (!CellReader.TryParseEnum(text, out EntityTag tag))
                {
                    errors.Add($"{row.Location}: {column} '{text}' isn't one of: "
                        + $"{string.Join(", ", Enum.GetNames(typeof(EntityTag)))}.");
                }
                else if (!tags.Contains(tag))
                {
                    tags.Add(tag);
                }
            }

            return tags;
        }
    }
}
