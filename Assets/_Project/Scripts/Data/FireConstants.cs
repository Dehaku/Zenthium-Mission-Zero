using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>The fire rows of constants.csv's <c>world</c> group (world.md, Fire; statuses.md, Burn).</summary>
    [Serializable]
    public sealed class FireConstants
    {
        [SerializeField] private float tickSeconds;
        [SerializeField] private float breakIntervalSeconds;
        [SerializeField] private float burnDamage;
        [SerializeField] private float burnSeconds;
        [SerializeField] private float burnOutSeconds;
        [SerializeField] private float spreadIntervalSeconds;
        [SerializeField] private float spreadChance;
        [SerializeField] private float propBurnDurationMultiplier;

        // Unity's serializer needs a parameterless constructor.
        public FireConstants()
        {
        }

        public FireConstants(
            float tickSeconds,
            float breakIntervalSeconds,
            float burnDamage,
            float burnSeconds,
            float burnOutSeconds,
            float spreadIntervalSeconds,
            float spreadChance,
            float propBurnDurationMultiplier)
        {
            this.tickSeconds = tickSeconds;
            this.breakIntervalSeconds = breakIntervalSeconds;
            this.burnDamage = burnDamage;
            this.burnSeconds = burnSeconds;
            this.burnOutSeconds = burnOutSeconds;
            this.spreadIntervalSeconds = spreadIntervalSeconds;
            this.spreadChance = spreadChance;
            this.propBurnDurationMultiplier = propBurnDurationMultiplier;
        }

        /// <summary>Game seconds between a burning building's Burn ticks on everything inside.</summary>
        public float TickSeconds => tickSeconds;

        /// <summary>Game seconds between a burning building destroying one of its walls, windows or doors.</summary>
        public float BreakIntervalSeconds => breakIntervalSeconds;

        /// <summary>The total damage of the Burn a burning building gives each tick.</summary>
        public float BurnDamage => burnDamage;

        /// <summary>Game seconds that Burn lasts, on entities.</summary>
        public float BurnSeconds => burnSeconds;

        /// <summary>Game seconds from catching fire to burned out.</summary>
        public float BurnOutSeconds => burnOutSeconds;

        /// <summary>Game seconds between each burning building's spread rolls.</summary>
        public float SpreadIntervalSeconds => spreadIntervalSeconds;

        /// <summary>The chance per roll, from 0 to 1, to set each unburned neighbor on fire.</summary>
        public float SpreadChance => spreadChance;

        /// <summary>Burn lasts this many times longer on props, at the same damage per tick.</summary>
        public float PropBurnDurationMultiplier => propBurnDurationMultiplier;
    }
}
