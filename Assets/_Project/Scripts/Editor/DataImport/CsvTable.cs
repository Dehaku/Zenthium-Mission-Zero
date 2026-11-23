using System;
using System.Collections.Generic;
using System.Text;

namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// A CSV file read into a header and rows. Quoted fields may hold commas, doubled quotes and line breaks.
    /// </summary>
    public sealed class CsvTable
    {
        private readonly Dictionary<string, int> columns;
        private readonly List<CsvRow> rows = new List<CsvRow>();

        private CsvTable(string fileName, Dictionary<string, int> columns)
        {
            FileName = fileName;
            this.columns = columns;
        }

        public string FileName { get; }
        public IReadOnlyList<CsvRow> Rows => rows;

        /// <returns>Null if the file can't be read as a table; the reasons are added to <paramref name="errors"/>.</returns>
        public static CsvTable Parse(string fileName, string text, List<string> errors)
        {
            List<Record> records = ReadRecords(fileName, text ?? string.Empty, errors);
            if (records.Count == 0)
            {
                errors.Add($"{fileName}: the file has no header row.");
                return null;
            }

            var columns = new Dictionary<string, int>(StringComparer.Ordinal);
            List<string> header = records[0].Cells;
            for (int i = 0; i < header.Count; i++)
            {
                string name = header[i].Trim();
                if (name.Length > 0 && !columns.ContainsKey(name))
                {
                    columns.Add(name, i);
                }
                else if (name.Length > 0)
                {
                    errors.Add($"{fileName}: the column '{name}' appears twice in the header.");
                }
            }

            var table = new CsvTable(fileName, columns);
            for (int i = 1; i < records.Count; i++)
            {
                Record record = records[i];
                if (IsBlank(record.Cells))
                {
                    continue;
                }

                if (record.Cells.Count > header.Count)
                {
                    errors.Add($"{fileName} line {record.LineNumber}: {record.Cells.Count} cells, but the header has {header.Count}.");
                }

                table.rows.Add(new CsvRow(table, record.LineNumber, record.Cells));
            }

            return table;
        }

        /// <returns>False if any column is missing; each missing one is added to <paramref name="errors"/>.</returns>
        public bool RequireColumns(List<string> errors, params string[] names)
        {
            bool allPresent = true;
            foreach (string name in names)
            {
                if (!columns.ContainsKey(name))
                {
                    errors.Add($"{FileName}: missing the column '{name}'.");
                    allPresent = false;
                }
            }

            return allPresent;
        }

        internal bool TryGetColumnIndex(string column, out int index)
        {
            return columns.TryGetValue(column, out index);
        }

        private static bool IsBlank(List<string> cells)
        {
            foreach (string cell in cells)
            {
                if (cell.Trim().Length > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<Record> ReadRecords(string fileName, string text, List<string> errors)
        {
            var records = new List<Record>();
            var cells = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            int line = 1;
            int recordLine = 1;

            // A byte order mark isn't part of the first column's name.
            int start = text.Length > 0 && text[0] == '﻿' ? 1 : 0;
            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else if (c != '\r')
                    {
                        if (c == '\n')
                        {
                            line++;
                        }

                        field.Append(c);
                    }
                }
                else if (c == '"' && field.Length == 0)
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    cells.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\n')
                {
                    cells.Add(field.ToString());
                    field.Clear();
                    records.Add(new Record(recordLine, cells));
                    cells = new List<string>();
                    line++;
                    recordLine = line;
                }
                else if (c != '\r')
                {
                    field.Append(c);
                }
            }

            if (inQuotes)
            {
                errors.Add($"{fileName} line {recordLine}: a quoted field is never closed.");
            }

            if (field.Length > 0 || cells.Count > 0)
            {
                cells.Add(field.ToString());
                records.Add(new Record(recordLine, cells));
            }

            return records;
        }

        private readonly struct Record
        {
            public Record(int lineNumber, List<string> cells)
            {
                LineNumber = lineNumber;
                Cells = cells;
            }

            public int LineNumber { get; }
            public List<string> Cells { get; }
        }
    }

    /// <summary>
    /// One data row of a <see cref="CsvTable"/>.
    /// </summary>
    public sealed class CsvRow
    {
        private readonly CsvTable table;
        private readonly List<string> cells;

        internal CsvRow(CsvTable table, int lineNumber, List<string> cells)
        {
            this.table = table;
            LineNumber = lineNumber;
            this.cells = cells;
        }

        public int LineNumber { get; }

        /// <summary>The file and line, for error messages.</summary>
        public string Location => $"{table.FileName} line {LineNumber}";

        /// <returns>The trimmed cell, or an empty string if the row or the table lacks that column.</returns>
        public string Get(string column)
        {
            if (table.TryGetColumnIndex(column, out int index) && index < cells.Count)
            {
                return cells[index].Trim();
            }

            return string.Empty;
        }
    }
}
