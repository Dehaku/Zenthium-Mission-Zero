using Zenthium.Data;

namespace Zenthium.Entities
{
    public enum DamageOutcome
    {
        /// <summary>The damage reached the target's HP (Defiance may still have saved it).</summary>
        Dealt,

        /// <summary>The target dodged the hit.</summary>
        Dodged,

        /// <summary>The target was Invulnerable, or being dragged.</summary>
        Blocked,

        /// <summary>Friendly fire rules forbid it, or the target is Incapacitated. No event is raised.</summary>
        NotAllowed,
    }

    /// <summary>
    /// The result of one hit or DoT tick, as raised by <see cref="Entity.OnDamaged"/>.
    /// </summary>
    public readonly struct DamageEvent
    {
        public DamageEvent(
            Entity source, Entity target, DamageType type, bool isDamageOverTime, DamageOutcome outcome, float amount)
        {
            Source = source;
            Target = target;
            Type = type;
            IsDamageOverTime = isDamageOverTime;
            Outcome = outcome;
            Amount = amount;
        }

        /// <summary>Who dealt it. Null for environmental damage, such as a vehicle explosion.</summary>
        public Entity Source { get; }

        public Entity Target { get; }
        public DamageType Type { get; }
        public bool IsDamageOverTime { get; }
        public DamageOutcome Outcome { get; }

        /// <summary>The damage after Armor and Damage Reduction. 0 unless <see cref="Outcome"/> is Dealt.</summary>
        public float Amount { get; }
    }
}
