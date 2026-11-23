using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// The <c>formula</c> rows of constants.csv for rules every entity shares: base stats, move speed,
    /// Defiance and resource regen (stats.md, combat.md).
    /// </summary>
    [Serializable]
    public sealed class EntityConstants
    {
        [SerializeField] private float baseStat;
        [SerializeField] private float baseMoveSpeed;
        [SerializeField] private float defianceChancePerWill;
        [SerializeField] private float defianceRollCost;
        [SerializeField] private float defianceInvulnerablePlayer;
        [SerializeField] private float defianceInvulnerableOther;
        [SerializeField] private float resourceRegenPerSecond;

        // Unity's serializer needs a parameterless constructor.
        public EntityConstants()
        {
        }

        public EntityConstants(
            float baseStat,
            float baseMoveSpeed,
            float defianceChancePerWill,
            float defianceRollCost,
            float defianceInvulnerablePlayer,
            float defianceInvulnerableOther,
            float resourceRegenPerSecond)
        {
            this.baseStat = baseStat;
            this.baseMoveSpeed = baseMoveSpeed;
            this.defianceChancePerWill = defianceChancePerWill;
            this.defianceRollCost = defianceRollCost;
            this.defianceInvulnerablePlayer = defianceInvulnerablePlayer;
            this.defianceInvulnerableOther = defianceInvulnerableOther;
            this.resourceRegenPerSecond = resourceRegenPerSecond;
        }

        /// <summary>Every entity's value in every stat before modifications. Heroes use their own stats instead.</summary>
        public float BaseStat => baseStat;

        /// <summary>Meters per second.</summary>
        public float BaseMoveSpeed => baseMoveSpeed;

        /// <summary>Defiance proc chance per point of Will, as a fraction (1 is 100%).</summary>
        public float DefianceChancePerWill => defianceChancePerWill;

        /// <summary>Proc chance each Defiance Roll stack removes, as a fraction.</summary>
        public float DefianceRollCost => defianceRollCost;

        /// <summary>Seconds of Invulnerable after a Defiance proc, for the player.</summary>
        public float DefianceInvulnerablePlayer => defianceInvulnerablePlayer;

        /// <summary>Seconds of Invulnerable after a Defiance proc, for everyone except the player.</summary>
        public float DefianceInvulnerableOther => defianceInvulnerableOther;

        /// <summary>Points per second every resource pool regains, unless a module stops it.</summary>
        public float ResourceRegenPerSecond => resourceRegenPerSecond;
    }
}
