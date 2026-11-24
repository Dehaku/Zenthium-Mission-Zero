using Zenthium.Data;

namespace Zenthium.Entities
{
    /// <summary>
    /// A direct hit about to land on an entity, after dodge and before Armor. Handlers such as Weather the Storm
    /// can cut it further or cancel its knockback.
    /// </summary>
    public struct IncomingHit
    {
        public IncomingHit(Entity source, DamageType type)
        {
            Source = source;
            Type = type;
            ExtraDamageReduction = 0f;
            PreventKnockback = false;
        }

        /// <summary>Null for environmental damage.</summary>
        public Entity Source { get; }

        public DamageType Type { get; }

        /// <summary>A Damage Reduction for this hit only, as a fraction. It multiplies with the target's own.</summary>
        public float ExtraDamageReduction { get; set; }

        public bool PreventKnockback { get; set; }
    }

    /// <summary>
    /// Reacts to direct hits on its entity before they're mitigated (the "on damage taken" trigger that can change
    /// the hit). DoT ticks don't reach it.
    /// </summary>
    public interface IIncomingHitHandler
    {
        void OnIncomingHit(ref IncomingHit hit);
    }
}
