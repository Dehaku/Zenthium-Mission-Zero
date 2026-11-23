using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// The parsed <c>values</c> column of one CSV row.
    /// </summary>
    [Serializable]
    public sealed class DataValueList
    {
        [SerializeField] private List<DataValue> values = new List<DataValue>();

        public DataValueList()
        {
        }

        internal DataValueList(List<DataValue> values)
        {
            this.values = values;
        }

        public IReadOnlyList<DataValue> All => values;

        public bool TryGet(string key, out DataValue value)
        {
            foreach (DataValue candidate in values)
            {
                if (string.Equals(candidate.Key, key, StringComparison.Ordinal))
                {
                    value = candidate;
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <returns>False if the key is missing or its value isn't a plain number.</returns>
        public bool TryGetNumber(string key, out float number)
        {
            if (TryGet(key, out DataValue value) && value.Kind == DataValueKind.Number)
            {
                number = value.Number;
                return true;
            }

            number = 0f;
            return false;
        }
    }
}
