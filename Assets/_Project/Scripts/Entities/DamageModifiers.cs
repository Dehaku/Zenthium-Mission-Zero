using System;

namespace Zenthium.Entities
{
    /// <summary>
    /// Percentage changes to the damage an entity deals and takes, from statuses and modules (Marked, Overclocked).
    /// Sources multiply, like stat boosts. The damage pipeline applies both before Armor.
    /// </summary>
    public sealed class DamageModifiers
    {
        private readonly ModifierStack dealt = new ModifierStack();
        private readonly ModifierStack taken = new ModifierStack();

        internal DamageModifiers()
        {
        }

        /// <summary>The factor on everything this entity deals: 1 with no modifiers.</summary>
        public float DealtMultiplier => dealt.BoostProduct();

        /// <summary>The factor on everything this entity takes: 1 with no modifiers.</summary>
        public float TakenMultiplier => taken.BoostProduct();

        /// <param name="fraction">-0.3 deals 30% less; 0.5 deals 50% more.</param>
        public ModifierHandle AddDealt(float fraction)
        {
            return dealt.Add(fraction);
        }

        public bool RemoveDealt(ModifierHandle handle)
        {
            return dealt.Remove(handle);
        }

        /// <param name="fraction">0.25 takes 25% more.</param>
        public ModifierHandle AddTaken(float fraction)
        {
            return taken.Add(fraction);
        }

        public bool RemoveTaken(ModifierHandle handle)
        {
            return taken.Remove(handle);
        }
    }
}
