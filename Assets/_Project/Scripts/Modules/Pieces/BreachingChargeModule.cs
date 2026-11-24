using System.Globalization;
using System.Text;
using UnityEngine;
using Zenthium.Data;

namespace Zenthium.Modules
{
    /// <summary>
    /// Breaching Charge (modules.md): places a charge in front of the user, as far as the range tier. After
    /// <c>fuse_s</c> of game time it explodes for its damage and knockback within the radius tier, under the user's
    /// friendly fire rules, and blows open what's breachable (Milestone 5).
    /// </summary>
    public sealed class BreachingChargeModule : ModuleBehaviour
    {
        public const string FuseKey = "fuse_s";

        private readonly HitEffects effects;
        private readonly float fuseSeconds;

        public BreachingChargeModule(ModuleData data, HitEffects effects, float fuseSeconds) : base(data)
        {
            this.effects = effects;
            this.fuseSeconds = fuseSeconds;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            effects.Describe(text, Context);
            text.Append("Fuse ").Append(fuseSeconds.ToString("0.#", CultureInfo.InvariantCulture)).Append(" s, radius ")
                .Append(Data.RadiusTier.Value.ToString("0.#", CultureInfo.InvariantCulture)).Append(" m\n");
        }

        protected override void Activate(ModuleAim aim)
        {
            Vector3 point = aim.PlacementPoint(Context.Position, Data.RangeTier.Value);
            TimedCharge charge = Context.World.SpawnCharge(point);
            if (charge != null)
            {
                charge.Arm(Context, effects, Data.RadiusTier.Value, fuseSeconds);
            }
        }
    }
}
