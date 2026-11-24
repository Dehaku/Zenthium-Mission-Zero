using System;

namespace Zenthium.Entities
{
    /// <summary>
    /// An entity's HP. At 0 HP it is Incapacitated. Only <see cref="Entity"/> changes it, so damage always
    /// goes through the pipeline and Defiance.
    /// </summary>
    public sealed class Health
    {
        // A revived entity must be above 0 HP, or it would still count as Incapacitated.
        private const float MinimumReviveHp = 1f;

        internal Health(float max)
        {
            Max = max;
            Current = max;
        }

        /// <summary>Current and max HP, after any change.</summary>
        public event Action<float, float> OnHealthChanged;

        public float Current { get; private set; }
        public float Max { get; private set; }
        public bool IsIncapacitated => Current <= 0f;

        /// <summary>
        /// Gaining max HP (from Toughness) adds the same amount to current HP; losing it doesn't lower current HP,
        /// except to keep it within the new max (stats.md). An Incapacitated entity stays at 0.
        /// </summary>
        internal void SetMax(float newMax)
        {
            float gained = newMax - Max;
            Max = newMax;
            if (!IsIncapacitated)
            {
                Current = gained > 0f ? Current + gained : Math.Min(Current, newMax);
            }

            RaiseHealthChanged();
        }

        internal void Lose(float amount)
        {
            Current = Math.Max(0f, Current - amount);
            RaiseHealthChanged();
        }

        internal void SetCurrent(float value)
        {
            Current = Math.Max(0f, Math.Min(value, Max));
            RaiseHealthChanged();
        }

        /// <summary>Gets an Incapacitated entity back up with the given HP, at least 1 and at most its max.</summary>
        /// <returns>False if it wasn't Incapacitated.</returns>
        internal bool Revive(float hp)
        {
            if (!IsIncapacitated)
            {
                return false;
            }

            Current = Math.Max(MinimumReviveHp, Math.Min(hp, Max));
            RaiseHealthChanged();
            return true;
        }

        /// <returns>The HP actually restored. Healing doesn't revive an Incapacitated entity.</returns>
        internal float Heal(float amount)
        {
            if (IsIncapacitated || amount <= 0f)
            {
                return 0f;
            }

            float before = Current;
            Current = Math.Min(Max, Current + amount);
            RaiseHealthChanged();
            return Current - before;
        }

        private void RaiseHealthChanged()
        {
            OnHealthChanged?.Invoke(Current, Max);
        }
    }
}
