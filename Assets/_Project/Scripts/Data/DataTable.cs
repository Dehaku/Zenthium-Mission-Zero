using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// Direct references to every generated asset of one type, with lookup by id.
    /// </summary>
    [Serializable]
    public sealed class DataTable<T> where T : DataEntry
    {
        [SerializeField] private List<T> entries = new List<T>();

        [NonSerialized] private Dictionary<string, T> byId;

        public IReadOnlyList<T> Entries => entries;

        public bool TryGet(string id, out T entry)
        {
            if (byId == null)
            {
                BuildIndex();
            }

            return byId.TryGetValue(id, out entry) && entry != null;
        }

        internal void SetEntries(List<T> newEntries)
        {
            entries = newEntries;
            byId = null;
        }

        private void BuildIndex()
        {
            byId = new Dictionary<string, T>(entries.Count, StringComparer.Ordinal);
            foreach (T entry in entries)
            {
                if (entry != null)
                {
                    byId[entry.Id] = entry;
                }
            }
        }
    }
}
