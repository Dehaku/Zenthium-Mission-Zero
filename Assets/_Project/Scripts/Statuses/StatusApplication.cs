using System.Collections.Generic;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Statuses
{
    /// <summary>What a status changes on its entity while active.</summary>
    public enum StatusModifierTarget
    {
        /// <summary>A slow, multiplied with other slows.</summary>
        Slow,
        MoveSpeedBonus,
        DamageDealt,
        DamageTaken,
        Armor,
        DamageReduction,
        StatBoost,
    }

    /// <summary>
    /// One change a status makes while active. Intensity statuses apply it once per layer, scaled by the layer count.
    /// </summary>
    public readonly struct StatusModifier
    {
        public StatusModifier(StatusModifierTarget target, float value, Stat stat = default)
        {
            Target = target;
            Value = value;
            Stat = stat;
        }

        public StatusModifierTarget Target { get; }
        public float Value { get; }

        /// <summary>The boosted stat, for <see cref="StatusModifierTarget.StatBoost"/>.</summary>
        public Stat Stat { get; }
    }

    /// <summary>
    /// One application of a status. The applying module supplies the amounts and the duration (statuses.md:
    /// "Module durations and amounts live with the module").
    /// </summary>
    public readonly struct StatusApplication
    {
        /// <summary>A duration for statuses that last until their source removes them.</summary>
        public const float UntilRemoved = float.PositiveInfinity;

        /// <param name="source">Who applied it; null for the environment.</param>
        /// <param name="seconds">Game seconds, before the half-length crowd control rule, or <see cref="UntilRemoved"/>.</param>
        /// <param name="maxLayers">The cap for Intensity statuses; 0 is no cap.</param>
        public StatusApplication(StatusType type, Entity source, float seconds,
            IReadOnlyList<StatusModifier> modifiers = null, int maxLayers = 0)
        {
            Type = type;
            Source = source;
            Seconds = seconds;
            Modifiers = modifiers;
            MaxLayers = maxLayers;
        }

        public StatusType Type { get; }
        public Entity Source { get; }
        public float Seconds { get; }
        public IReadOnlyList<StatusModifier> Modifiers { get; }
        public int MaxLayers { get; }
    }
}
