using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// A purchase price in missions of work, from a row of prices.csv (balance.md section 7).
    /// </summary>
    public sealed class PriceData : DataEntry
    {
        [SerializeField] private float missions;
        [SerializeField] private ZennyFamily currency;

        /// <summary>How many missions' worth of income it costs.</summary>
        public float Missions => missions;

        public ZennyFamily Currency => currency;

        internal void Populate(float newMissions, ZennyFamily newCurrency)
        {
            missions = newMissions;
            currency = newCurrency;
        }
    }
}
