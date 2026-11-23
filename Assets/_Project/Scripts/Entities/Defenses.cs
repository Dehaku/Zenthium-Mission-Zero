using System;
using Zenthium.Data;

namespace Zenthium.Entities
{
    /// <summary>
    /// An entity's Armor, Damage Reduction and dodge chance (combat.md, balance.md section 1).
    /// The damage pipeline applies them.
    /// </summary>
    public sealed class Defenses
    {
        private readonly EntityStats stats;
        private readonly BalanceFormulas formulas;
        private readonly ModifierStack armorBonuses = new ModifierStack();
        private readonly ModifierStack damageReductions = new ModifierStack();

        internal Defenses(EntityStats stats, BalanceFormulas formulas)
        {
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            this.formulas = formulas ?? throw new ArgumentNullException(nameof(formulas));
            armorBonuses.OnChanged += RaiseDefensesChanged;
            damageReductions.OnChanged += RaiseDefensesChanged;
        }

        /// <summary>Raised when a module's Armor or Damage Reduction changes. Stat changes raise <see cref="EntityStats.OnStatsChanged"/>.</summary>
        public event Action OnDefensesChanged;

        /// <summary>Flat damage subtracted from every hit: from Toughness, plus module bonuses.</summary>
        public float Armor => formulas.Armor(stats.Get(Stat.Toughness)) + armorBonuses.Sum();

        /// <summary>The fraction cut from every hit after Armor. Sources multiply, so it never reaches 1.</summary>
        public float DamageReduction => 1f - damageReductions.RemainingAfterReductions();

        /// <summary>The chance from 0 to 1 that a direct hit misses.</summary>
        public float DodgeChance => formulas.DodgeChance(stats.Get(Stat.Finesse));

        public ModifierHandle AddArmor(float amount)
        {
            return armorBonuses.Add(amount);
        }

        public bool RemoveArmor(ModifierHandle handle)
        {
            return armorBonuses.Remove(handle);
        }

        /// <param name="fraction">The reduction as a fraction: 0.3 is 30%.</param>
        public ModifierHandle AddDamageReduction(float fraction)
        {
            return damageReductions.Add(fraction);
        }

        public bool RemoveDamageReduction(ModifierHandle handle)
        {
            return damageReductions.Remove(handle);
        }

        public void ClearModifiers()
        {
            armorBonuses.Clear();
            damageReductions.Clear();
        }

        private void RaiseDefensesChanged()
        {
            OnDefensesChanged?.Invoke();
        }
    }
}
