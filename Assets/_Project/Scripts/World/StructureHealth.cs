using System;

namespace Zenthium.World
{
    /// <summary>
    /// A wall's, door's or prop's HP (world.md). It breaks once at 0 and never comes back.
    /// </summary>
    public sealed class StructureHealth
    {
        public StructureHealth(float max)
        {
            if (max <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "A structure needs some HP.");
            }

            Max = max;
            Current = max;
        }

        public float Max { get; }
        public float Current { get; private set; }
        public bool IsBroken => Current <= 0f;

        /// <summary>Raised when HP changes, before <see cref="OnBroken"/>.</summary>
        public event Action OnChanged;

        /// <summary>Raised once, when HP first reaches 0.</summary>
        public event Action OnBroken;

        /// <returns>HP actually lost: no more than was left, and nothing once broken.</returns>
        public float TakeDamage(float amount)
        {
            if (IsBroken || amount <= 0f)
            {
                return 0f;
            }

            float lost = Math.Min(amount, Current);
            Current -= lost;
            OnChanged?.Invoke();
            if (IsBroken)
            {
                OnBroken?.Invoke();
            }

            return lost;
        }

        /// <summary>Breaks it whatever its HP, such as a Small prop that's been thrown.</summary>
        public void Break()
        {
            TakeDamage(Current);
        }
    }
}
