using Zenthium.Entities;

namespace Zenthium.Movement
{
    /// <summary>
    /// Who knocked an entity back, and their stat for it, so slamming into a wall can deal damage from that stat
    /// (world.md, Walls). The stat value is taken when the knockback starts.
    /// </summary>
    public readonly struct KnockbackCause
    {
        /// <param name="source">Who caused it. Null for no one, which deals no impact damage.</param>
        /// <param name="statValue">The source's value in the stat of the module that caused it.</param>
        public KnockbackCause(Entity source, float statValue)
        {
            Source = source;
            StatValue = statValue;
        }

        public Entity Source { get; }
        public float StatValue { get; }
        public bool HasSource => Source != null;
    }
}
