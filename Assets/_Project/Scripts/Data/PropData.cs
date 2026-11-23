using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// A prop or vehicle, from a row of props.csv. Blank CSV cells are 0.
    /// </summary>
    public sealed class PropData : DataEntry
    {
        [SerializeField] private PropSize size;
        [SerializeField] private float strengthNeeded;
        [SerializeField] private float hp;
        [SerializeField] private float thrownDamageXKick;
        [SerializeField] private float armor;
        [SerializeField] private float damageReduction;
        [SerializeField] private float explosionDamage;
        [SerializeField] private float explosionRadius;

        public PropSize Size => size;

        /// <summary>Strength needed to Drag or throw it.</summary>
        public float StrengthNeeded => strengthNeeded;

        public float Hp => hp;

        /// <summary>Thrown damage, as a multiple of Kick damage.</summary>
        public float ThrownDamageXKick => thrownDamageXKick;

        public float Armor => armor;
        public float DamageReduction => damageReduction;
        public float ExplosionDamage => explosionDamage;

        /// <summary>In meters.</summary>
        public float ExplosionRadius => explosionRadius;

        internal void Populate(
            PropSize newSize,
            float newStrengthNeeded,
            float newHp,
            float newThrownDamageXKick,
            float newArmor,
            float newDamageReduction,
            float newExplosionDamage,
            float newExplosionRadius)
        {
            size = newSize;
            strengthNeeded = newStrengthNeeded;
            hp = newHp;
            thrownDamageXKick = newThrownDamageXKick;
            armor = newArmor;
            damageReduction = newDamageReduction;
            explosionDamage = newExplosionDamage;
            explosionRadius = newExplosionRadius;
        }
    }
}
