using System;
using Zenthium.Data;

namespace Zenthium.Entities
{
    /// <summary>
    /// An entity's SWIFT stats and Detectability, with stacking as in stats.md: percentage boosts multiply,
    /// except the difficulty bonus, which adds to the innate rank boost.
    /// A stat is base x (1 + innate + difficulty) x (1 + boost) x (1 + boost)...
    /// </summary>
    public sealed class EntityStats
    {
        private static readonly int StatCount = Enum.GetValues(typeof(Stat)).Length;

        private readonly float[] baseValues = new float[StatCount];
        private readonly ModifierStack[] boosts = new ModifierStack[StatCount];
        private readonly ModifierStack detectabilityModifiers = new ModifierStack();

        /// <param name="rankBoost">The innate rank boost plus the difficulty bonus, as a fraction (2 is +200%).</param>
        public EntityStats(StatBlock baseStats, float rankBoost)
        {
            RankBoost = rankBoost;
            for (int i = 0; i < StatCount; i++)
            {
                baseValues[i] = baseStats.Get((Stat)i);
                boosts[i] = new ModifierStack();
                boosts[i].OnChanged += RaiseStatsChanged;
            }

            detectabilityModifiers.OnChanged += RaiseStatsChanged;
        }

        public event Action OnStatsChanged;

        /// <summary>The innate rank boost plus the difficulty bonus, as a fraction.</summary>
        public float RankBoost { get; }

        /// <summary>
        /// How fast enemies notice this entity, as a multiplier: 1 is the default Detectability (stats.md).
        /// Modifiers multiply, like stat boosts.
        /// </summary>
        public float Detectability => detectabilityModifiers.BoostProduct();

        /// <summary>The stat before any boost: a hero's own stat, or the base stat for everyone else.</summary>
        public float GetBase(Stat stat)
        {
            return baseValues[(int)stat];
        }

        public void SetBase(Stat stat, float value)
        {
            baseValues[(int)stat] = value;
            RaiseStatsChanged();
        }

        /// <summary>The stat after every boost.</summary>
        public float Get(Stat stat)
        {
            int index = (int)stat;
            return baseValues[index] * (1f + RankBoost) * boosts[index].BoostProduct();
        }

        /// <param name="fraction">The boost as a fraction: 2 is +200%, -0.5 is -50%.</param>
        public ModifierHandle AddBoost(Stat stat, float fraction)
        {
            return boosts[(int)stat].Add(fraction);
        }

        public bool RemoveBoost(Stat stat, ModifierHandle handle)
        {
            return boosts[(int)stat].Remove(handle);
        }

        public void ClearBoosts()
        {
            foreach (ModifierStack stack in boosts)
            {
                stack.Clear();
            }
        }

        /// <param name="fraction">The change as a fraction: -0.5 halves Detectability.</param>
        public ModifierHandle AddDetectabilityModifier(float fraction)
        {
            return detectabilityModifiers.Add(fraction);
        }

        public bool RemoveDetectabilityModifier(ModifierHandle handle)
        {
            return detectabilityModifiers.Remove(handle);
        }

        private void RaiseStatsChanged()
        {
            OnStatsChanged?.Invoke();
        }
    }
}
