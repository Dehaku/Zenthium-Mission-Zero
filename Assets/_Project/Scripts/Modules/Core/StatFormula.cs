using System.Globalization;
using System.Text;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// A module's damage from its stat and damage tier: Normal damage (base + per-stat x stat) times the tier
    /// (balance.md, sections 1 and 4). Also writes itself out with live numbers for the MoreInfo display
    /// (modules.md, Module info).
    /// </summary>
    public sealed class StatFormula
    {
        private const string NumberFormat = "0.##";

        public StatFormula(Stat stat, TierValue damageTier)
        {
            Stat = stat;
            DamageTier = damageTier;
        }

        public Stat Stat { get; }
        public TierValue DamageTier { get; }

        /// <returns>False if the module has no stat or no damage tier.</returns>
        public static bool TryCreate(ModuleData module, out StatFormula formula)
        {
            formula = null;
            if (module == null || !module.DamageTier.IsSet || !module.TryGetStat(out Stat stat))
            {
                return false;
            }

            formula = new StatFormula(stat, module.DamageTier);
            return true;
        }

        /// <summary>The damage before the user's damage bonuses and the target's defenses.</summary>
        public float Damage(Entity user, BalanceFormulas formulas)
        {
            return formulas.TierDamage(user.Stats.Get(Stat), DamageTier);
        }

        /// <summary>
        /// Appends, for example, <c>Damage = (10 + Strength(20) x 2) x Normal(1) = 50</c>, plus the user's damage
        /// bonuses when they change it.
        /// </summary>
        public void Describe(StringBuilder text, Entity user, FormulaConstants constants, BalanceFormulas formulas)
        {
            float damage = Damage(user, formulas);
            text.Append("Damage = (").Append(Format(constants.NormalDamageBase)).Append(" + ")
                .Append(Stat).Append('(').Append(Format(user.Stats.Get(Stat))).Append(") x ")
                .Append(Format(constants.NormalDamagePerStat)).Append(") x ")
                .Append(DamageTier.Word).Append('(').Append(Format(DamageTier.Value)).Append(") = ")
                .Append(Format(damage));

            float bonus = user.OutgoingDamageMultiplier * user.DamageModifiers.DealtMultiplier;
            if (bonus != 1f)
            {
                text.Append(", x Bonuses(").Append(Format(bonus)).Append(") = ").Append(Format(damage * bonus));
            }

            text.Append('\n');
        }

        private static string Format(float value)
        {
            return value.ToString(NumberFormat, CultureInfo.InvariantCulture);
        }
    }
}
