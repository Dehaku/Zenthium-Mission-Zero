using System.Globalization;
using System.Text;
using UnityEngine;
using Zenthium.Data;

namespace Zenthium.Modules
{
    /// <summary>
    /// Energy Shield and Barrier Shield (modules.md): a crescent in front of the user for the duration tier. It
    /// stops hostile projectiles and knocks back enemies that touch it with the module's knockback. A new cast
    /// replaces the old shield.
    /// </summary>
    public sealed class ShieldModule : ModuleBehaviour
    {
        public const string ArcKey = "arc_degrees";

        private readonly float seconds;
        private readonly float arcDegrees;
        private ShieldZone shield;

        public ShieldModule(ModuleData data, float seconds, float arcDegrees) : base(data)
        {
            this.seconds = seconds;
            this.arcDegrees = arcDegrees;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Shield: ").Append(arcDegrees.ToString("0", CultureInfo.InvariantCulture))
                .Append(" degrees in front, ").Append(seconds.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" s; blocks shots, pushes enemies\n");
        }

        protected override void Activate(ModuleAim aim)
        {
            RemoveShield();
            shield = Context.World.SpawnShield(Context.Position);
            if (shield != null)
            {
                shield.Raise(Context, Data.KnockbackTier, seconds, arcDegrees * 0.5f);
            }
        }

        protected override void OnDisabled()
        {
            RemoveShield();
        }

        private void RemoveShield()
        {
            if (shield != null)
            {
                Object.Destroy(shield.gameObject);
            }

            shield = null;
        }
    }
}
