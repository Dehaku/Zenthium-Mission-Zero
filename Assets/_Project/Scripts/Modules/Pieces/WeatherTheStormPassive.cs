using System.Globalization;
using System.Text;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Weather the Storm (modules.md): when hit, spends its cost (if the user has it) for no knockback and its
    /// <c>damage_reduction</c> off that hit, multiplied with the user's own Damage Reduction.
    /// </summary>
    public sealed class WeatherTheStormPassive : ModuleBehaviour, IIncomingHitHandler
    {
        public const string DamageReductionKey = "damage_reduction";

        private const string NumberFormat = "0.##";

        private readonly ResourceType resource;
        private readonly float cost;
        private readonly float damageReduction;
        private Entity hitUser;

        public WeatherTheStormPassive(ModuleData data, ResourceType resource, float cost, float damageReduction)
            : base(data)
        {
            this.resource = resource;
            this.cost = cost;
            this.damageReduction = damageReduction;
        }

        public void OnIncomingHit(ref IncomingHit hit)
        {
            if (hitUser == null || !hitUser.Resources.TrySpend(resource, cost))
            {
                return;
            }

            hit.ExtraDamageReduction = 1f - (1f - hit.ExtraDamageReduction) * (1f - damageReduction);
            hit.PreventKnockback = true;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("When hit: ").Append(cost.ToString(NumberFormat, CultureInfo.InvariantCulture)).Append(' ')
                .Append(resource).Append(" for no knockback and ")
                .Append((damageReduction * 100f).ToString(NumberFormat, CultureInfo.InvariantCulture))
                .Append("% less damage\n");
        }

        protected override void OnEnabled()
        {
            hitUser = Owner;
            hitUser.AddIncomingHitHandler(this);
        }

        protected override void OnDisabled()
        {
            if (hitUser != null)
            {
                hitUser.RemoveIncomingHitHandler(this);
                hitUser = null;
            }
        }
    }
}
