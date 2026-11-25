using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>The Valuables rows of constants.csv's <c>world</c> group (world.md, Valuables).</summary>
    [Serializable]
    public sealed class ValuableConstants
    {
        [SerializeField] private float slowPerValuable;
        [SerializeField] private float slowCap;
        [SerializeField] private float iconGrowthPerValuable;
        [SerializeField] private float iconGrowthCap;
        [SerializeField] private float pickupSeconds;

        // Unity's serializer needs a parameterless constructor.
        public ValuableConstants()
        {
        }

        public ValuableConstants(
            float slowPerValuable,
            float slowCap,
            float iconGrowthPerValuable,
            float iconGrowthCap,
            float pickupSeconds)
        {
            this.slowPerValuable = slowPerValuable;
            this.slowCap = slowCap;
            this.iconGrowthPerValuable = iconGrowthPerValuable;
            this.iconGrowthCap = iconGrowthCap;
            this.pickupSeconds = pickupSeconds;
        }

        /// <summary>Carrying Treasure: move speed lost per Valuable, as a fraction.</summary>
        public float SlowPerValuable => slowPerValuable;

        /// <summary>Carrying Treasure: the most move speed Valuables can take, as a fraction.</summary>
        public float SlowCap => slowCap;

        /// <summary>The money bag icon's growth per Valuable, as a fraction of its size.</summary>
        public float IconGrowthPerValuable => iconGrowthPerValuable;

        /// <summary>The most the icon can grow, as a fraction of its size.</summary>
        public float IconGrowthCap => iconGrowthCap;

        /// <summary>Game seconds anyone but the player takes to pick up a Valuable.</summary>
        public float PickupSeconds => pickupSeconds;
    }
}
