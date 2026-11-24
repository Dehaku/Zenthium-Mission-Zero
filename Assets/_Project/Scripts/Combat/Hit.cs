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
        /// <param name="canBeDodged">False for hits nobody can sidestep, such as slamming into a wall.</param>
        public Hit(Entity source, float baseDamage, DamageType type, bool isDamageOverTime = false,
            bool canBeDodged = true)
        {
            Source = source;
            BaseDamage = baseDamage;
            Type = type;
            IsDamageOverTime = isDamageOverTime;
            CanBeDodged = canBeDodged;
        }

        public Entity Source { get; }
        public float BaseDamage { get; }
        public DamageType Type { get; }

        /// <summary>DoT ticks ignore Armor and Damage Reduction, and can't be dodged.</summary>
        public bool IsDamageOverTime { get; }

        /// <summary>DoT ticks and environmental damage can't be dodged either, whatever this says.</summary>
        public bool CanBeDodged { get; }

        public bool IsEnvironmental => Source == null;
    }
}
