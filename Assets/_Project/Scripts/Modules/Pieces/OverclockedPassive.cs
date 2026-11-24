using System.Globalization;
using System.Text;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Overclocked (modules.md): +<c>damage</c> to everything the user deals while their Charge is at least
    /// <c>charge_above</c> of its max.
    /// </summary>
    public sealed class OverclockedPassive : ModuleBehaviour
    {
        public const string DamageKey = "damage";
        public const string ChargeAboveKey = "charge_above";

        private readonly float damageBonus;
        private readonly float chargeAbove;
        private Entity boosted;
        private ModifierHandle handle;

        public OverclockedPassive(ModuleData data, float damageBonus, float chargeAbove) : base(data)
        {
            this.damageBonus = damageBonus;
            this.chargeAbove = chargeAbove;
        }

        /// <summary>Charge at or above the threshold fraction of a pool that exists.</summary>
        public static bool IsCharged(float current, float max, float threshold)
        {
            return max > 0f && current >= max * threshold;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("+").Append((damageBonus * 100f).ToString("0", CultureInfo.InvariantCulture))
                .Append("% damage while Charge is at least ")
                .Append((chargeAbove * 100f).ToString("0", CultureInfo.InvariantCulture)).Append("% (")
                .Append(boosted != null ? "on" : "off").Append(")\n");
        }

        protected internal override void Tick(float gameDelta)
        {
            ResourcePools resources = Owner.Resources;
            bool charged = IsCharged(resources.Current(ResourceType.Charge), resources.Max(ResourceType.Charge),
                chargeAbove);
            if (charged && boosted == null)
            {
                boosted = Owner;
                handle = boosted.DamageModifiers.AddDealt(damageBonus);
            }
            else if (!charged)
            {
                RemoveBoost();
            }
        }

        protected override void OnDisabled()
        {
            RemoveBoost();
        }

        private void RemoveBoost()
        {
            if (boosted != null)
            {
                boosted.DamageModifiers.RemoveDealt(handle);
                boosted = null;
            }
        }
    }
}
