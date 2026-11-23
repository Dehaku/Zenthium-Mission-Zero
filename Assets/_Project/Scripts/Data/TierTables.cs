using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// The standard tier values from the <c>tier</c> rows of constants.csv (balance.md section 4).
    /// </summary>
    [Serializable]
    public sealed class TierTables
    {
        [Serializable]
        private struct Entry
        {
            [SerializeField] private TierScale scale;
            [SerializeField] private TierValue tier;

            public Entry(TierScale scale, TierValue tier)
            {
                this.scale = scale;
                this.tier = tier;
            }

            public TierScale Scale => scale;
            public TierValue Tier => tier;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public bool TryGet(TierScale scale, string word, out TierValue tier)
        {
            foreach (Entry entry in entries)
            {
                if (entry.Scale == scale && string.Equals(entry.Tier.Word, word, StringComparison.Ordinal))
                {
                    tier = entry.Tier;
                    return true;
                }
            }

            tier = default;
            return false;
        }

        /// <returns>False if the scale already has this word.</returns>
        internal bool TryAdd(TierScale scale, string word, float value)
        {
            if (TryGet(scale, word, out _))
            {
                return false;
            }

            entries.Add(new Entry(scale, new TierValue(word, value)));
            return true;
        }
    }
}
