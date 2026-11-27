using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Statuses;

namespace Zenthium.Modules
{
    /// <summary>
    /// Notarized (modules.md): entities on the Notary's side within the radius tier, not the Notary itself, are
    /// Notarized, taking less damage (<c>damage_reduction</c>). The status lasts while they're in range
    /// (statuses.md): it's refreshed a few times a second with a short duration, so it ends soon after they leave.
    /// </summary>
    public sealed class NotarizedPassive : ModuleBehaviour
    {
        public const string DamageReductionKey = "damage_reduction";

        // Game seconds between refreshes, and how long each lasts: longer than the gap, so it never flickers off in range.
        private const float RefreshSeconds = 0.25f;
        private const float StatusSeconds = 0.5f;
        private const string NumberFormat = "0.##";

        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private readonly StatusModifier[] modifiers;
        private float untilRefresh;

        public NotarizedPassive(ModuleData data, float damageReduction) : base(data)
        {
            modifiers = new[] { new StatusModifier(StatusModifierTarget.DamageReduction, damageReduction) };
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Allies within ")
                .Append(Data.RadiusTier.Value.ToString(NumberFormat, CultureInfo.InvariantCulture))
                .Append(" m take ").Append((modifiers[0].Value * 100f).ToString(NumberFormat, CultureInfo.InvariantCulture))
                .Append("% less damage\n");
        }

        protected internal override void Tick(float gameDelta)
        {
            if (!IsEnabled || Owner.IsIncapacitated)
            {
                return;
            }

            untilRefresh -= gameDelta;
            if (untilRefresh > 0f)
            {
                return;
            }

            untilRefresh = RefreshSeconds;
            Context.World.CollectEntities(Context.Position, Data.RadiusTier.Value, reached);
            foreach (EntityBehaviour body in reached)
            {
                if (SideRules.IsOnUsersSide(Owner, body.Entity))
                {
                    body.Entity.Statuses.TryApply(new StatusApplication(StatusType.Notarized, Owner, StatusSeconds,
                        modifiers));
                }
            }
        }
    }
}
