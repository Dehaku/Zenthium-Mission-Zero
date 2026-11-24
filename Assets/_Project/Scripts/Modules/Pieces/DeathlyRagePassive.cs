using System.Globalization;
using System.Text;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Statuses;

namespace Zenthium.Modules
{
    /// <summary>
    /// Deathly Rage (modules.md): while equipped, adds <c>defiance_chance_bonus</c> to Defiance, enough to guarantee
    /// one proc. On a proc, grants the Deathly Rage status for the rest of the mission: Strength x<c>strength_x</c>
    /// and Toughness x<c>toughness_x</c> (the extra HP is added to current HP by the Toughness rule). It doesn't stack.
    /// </summary>
    public sealed class DeathlyRagePassive : ModuleBehaviour
    {
        public const string StrengthKey = "strength_x";
        public const string ToughnessKey = "toughness_x";
        public const string DefianceBonusKey = "defiance_chance_bonus";

        private readonly StatusModifier[] rage;
        private readonly float defianceBonus;
        private Entity bonused;
        private ModifierHandle bonusHandle;

        /// <param name="strengthTimes">3 triples Strength, a +200% boost.</param>
        public DeathlyRagePassive(ModuleData data, float strengthTimes, float toughnessTimes, float defianceBonus)
            : base(data)
        {
            rage = new[]
            {
                new StatusModifier(StatusModifierTarget.StatBoost, strengthTimes - 1f, Stat.Strength),
                new StatusModifier(StatusModifierTarget.StatBoost, toughnessTimes - 1f, Stat.Toughness),
            };
            this.defianceBonus = defianceBonus;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Defiance +").Append((defianceBonus * 100f).ToString("0", CultureInfo.InvariantCulture))
                .Append("%; on Defiance: Strength and Toughness x")
                .Append((rage[0].Value + 1f).ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" for the mission\n");
        }

        protected override void OnEnabled()
        {
            bonused = Owner;
            bonusHandle = bonused.Defiance.AddChanceBonus(defianceBonus);
        }

        protected override void OnDisabled()
        {
            if (bonused != null)
            {
                bonused.Defiance.RemoveChanceBonus(bonusHandle);
                bonused = null;
            }
        }

        protected internal override void OnDefianceProc()
        {
            Owner.Statuses.TryApply(new StatusApplication(StatusType.DeathlyRage, Owner, StatusApplication.UntilRemoved,
                rage));
        }
    }
}
