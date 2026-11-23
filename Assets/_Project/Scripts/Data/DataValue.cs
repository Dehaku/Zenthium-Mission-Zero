using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// One <c>key=value</c> pair from a CSV <c>values</c> column. Look keys up with the
    /// generated <c>DataIds.ValueKeys</c> constants.
    /// </summary>
    [Serializable]
    public sealed class DataValue
    {
        [SerializeField] private string key;
        [SerializeField] private DataValueKind kind;
        [SerializeField] private float number;
        [SerializeField] private float rangeMin;
        [SerializeField] private float rangeMax;
        [SerializeField] private TierValue tier;
        [SerializeField] private ModuleData module;

        public string Key => key;
        public DataValueKind Kind => kind;

        /// <summary>Set when <see cref="Kind"/> is Number.</summary>
        public float Number => number;

        /// <summary>Set when <see cref="Kind"/> is Range, such as <c>goons=3-5</c>.</summary>
        public float RangeMin => rangeMin;

        /// <inheritdoc cref="RangeMin"/>
        public float RangeMax => rangeMax;

        /// <summary>Set when <see cref="Kind"/> is Tier.</summary>
        public TierValue Tier => tier;

        /// <summary>Set when <see cref="Kind"/> is Module.</summary>
        public ModuleData Module => module;

        internal static DataValue FromNumber(string key, float number)
        {
            return new DataValue { key = key, kind = DataValueKind.Number, number = number };
        }

        internal static DataValue FromRange(string key, float min, float max)
        {
            return new DataValue { key = key, kind = DataValueKind.Range, rangeMin = min, rangeMax = max };
        }

        internal static DataValue FromTier(string key, TierValue tier)
        {
            return new DataValue { key = key, kind = DataValueKind.Tier, tier = tier };
        }

        internal static DataValue FromModule(string key, ModuleData module)
        {
            return new DataValue { key = key, kind = DataValueKind.Module, module = module };
        }
    }
}
