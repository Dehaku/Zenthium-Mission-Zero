using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// The <c>economy</c> rows of constants.csv (balance.md section 7).
    /// </summary>
    [Serializable]
    public sealed class EconomyConstants
    {
        [Serializable]
        public struct IncomeMix
        {
            [SerializeField] private MissionType missionType;
            [SerializeField] private ZennyFamily family;
            [SerializeField] private float multiplier;

            public IncomeMix(MissionType missionType, ZennyFamily family, float multiplier)
            {
                this.missionType = missionType;
                this.family = family;
                this.multiplier = multiplier;
            }

            public MissionType MissionType => missionType;
            public ZennyFamily Family => family;
            public float Multiplier => multiplier;
        }

        [SerializeField] private float incomeBasic;
        [SerializeField] private float incomeGem;
        [SerializeField] private float incomeMetallic;
        [SerializeField] private float statPriceIncreasePerPoint;
        [SerializeField] private List<IncomeMix> incomeMixes = new List<IncomeMix>();

        // Unity's serializer needs a parameterless constructor.
        public EconomyConstants()
        {
        }

        public EconomyConstants(
            float incomeBasic,
            float incomeGem,
            float incomeMetallic,
            float statPriceIncreasePerPoint,
            List<IncomeMix> incomeMixes)
        {
            this.incomeBasic = incomeBasic;
            this.incomeGem = incomeGem;
            this.incomeMetallic = incomeMetallic;
            this.statPriceIncreasePerPoint = statPriceIncreasePerPoint;
            this.incomeMixes = incomeMixes;
        }

        /// <summary>Added to a stat's price, as a fraction of its base price, per point already bought in it.</summary>
        public float StatPriceIncreasePerPoint => statPriceIncreasePerPoint;

        /// <summary>Income per rank D mission before bonuses.</summary>
        public float Income(ZennyFamily family)
        {
            switch (family)
            {
                case ZennyFamily.Basic: return incomeBasic;
                case ZennyFamily.Gem: return incomeGem;
                case ZennyFamily.Metallic: return incomeMetallic;
                default: throw new ArgumentOutOfRangeException(nameof(family), family, null);
            }
        }

        /// <summary>
        /// How much a mission type shifts one family's income. Only the leaning families have rows;
        /// the rest stay even (balance.md section 7: Arson Riot is even).
        /// </summary>
        public float IncomeMultiplier(MissionType missionType, ZennyFamily family)
        {
            foreach (IncomeMix mix in incomeMixes)
            {
                if (mix.MissionType == missionType && mix.Family == family)
                {
                    return mix.Multiplier;
                }
            }

            return 1f;
        }
    }
}
