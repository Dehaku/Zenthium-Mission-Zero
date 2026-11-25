using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Statuses
{
    /// <summary>
    /// One tick of a damage-over-time status, raised by <see cref="StatusEffects"/> for the damage pipeline to deal.
    /// </summary>
    public readonly struct DamageOverTimeTick
    {
        public DamageOverTimeTick(StatusType status, Entity source, float damage, DamageType damageType)
        {
            Status = status;
            Source = source;
            Damage = damage;
            DamageType = damageType;
        }

        public StatusType Status { get; }

        /// <summary>Null for the environment, such as a burning building.</summary>
        public Entity Source { get; }

        public float Damage { get; }
        public DamageType DamageType { get; }
    }
}
