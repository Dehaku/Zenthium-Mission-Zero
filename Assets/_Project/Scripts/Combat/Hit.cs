using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Combat
{
    /// <summary>
    /// One hit or DoT tick, before the damage pipeline applies the target's defenses.
    /// </summary>
    public readonly struct Hit
    {
        /// <param name="source">Who deals it. Null for environmental damage, such as a vehicle explosion.</param>
        /// <param name="baseDamage">The module's damage, before the source's rank damage bonus.</param>
        public Hit(Entity source, float baseDamage, DamageType type, bool isDamageOverTime = false)
        {
            Source = source;
            BaseDamage = baseDamage;
            Type = type;
            IsDamageOverTime = isDamageOverTime;
        }

        public Entity Source { get; }
        public float BaseDamage { get; }
        public DamageType Type { get; }

        /// <summary>DoT ticks ignore Armor and Damage Reduction, and can't be dodged.</summary>
        public bool IsDamageOverTime { get; }

        public bool IsEnvironmental => Source == null;
    }
}
