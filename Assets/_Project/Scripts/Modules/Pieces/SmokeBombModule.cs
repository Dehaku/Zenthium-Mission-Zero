using System.Globalization;
using System.Text;
using Zenthium.Data;

namespace Zenthium.Modules
{
    /// <summary>
    /// Smoke Bomb (modules.md): throws a cloud to the aimed point, as far as the range tier. For the duration tier,
    /// opponents inside are Stunned while they stay in it (pulsing on Leaders and the player); each rolls its
    /// Toughness resist once on entering. The cloud also blocks line of sight and empties awareness bars
    /// (<see cref="SmokeCloud"/>).
    /// </summary>
    public sealed class SmokeBombModule : ModuleBehaviour
    {
        public SmokeBombModule(ModuleData data) : base(data)
        {
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Cloud ").Append(Data.RadiusTier.Value.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" m for ").Append(Data.DurationTier.Value.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" s: enemies inside are Stunned\n");
        }

        protected override void Activate(ModuleAim aim)
        {
            SmokeCloud cloud = Context.World.SpawnSmoke(aim.PlacementPoint(Context.Position, Data.RangeTier.Value));
            if (cloud != null)
            {
                cloud.Spread(Context, Data, Data.RadiusTier.Value, Data.DurationTier.Value);
            }
        }
    }
}
