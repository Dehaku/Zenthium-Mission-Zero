using System;
using System.Globalization;
using System.Text;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Statuses;

namespace Zenthium.Modules
{
    /// <summary>
    /// Reactive Plating (modules.md): each hit that lands adds a layer of the Reactive Plating status,
    /// <c>armor_per_hit</c> Armor each, up to <c>armor_max</c>. The layers end <c>reset_s</c> after the last hit.
    /// </summary>
    public sealed class ReactivePlatingPassive : ModuleBehaviour
    {
        public const string ArmorPerHitKey = "armor_per_hit";
        public const string ArmorMaxKey = "armor_max";
        public const string ResetKey = "reset_s";

        // Absorbs float error, so armor_max / armor_per_hit landing on a whole number isn't floored below it.
        private const float WholeNumberTolerance = 0.0001f;

        private readonly StatusModifier[] armorPerLayer;
        private readonly int maxLayers;
        private readonly float resetSeconds;

        public ReactivePlatingPassive(ModuleData data, float armorPerHit, float armorMax, float resetSeconds)
            : base(data)
        {
            armorPerLayer = new[] { new StatusModifier(StatusModifierTarget.Armor, armorPerHit) };
            maxLayers = armorPerHit > 0f ? (int)Math.Floor(armorMax / armorPerHit + WholeNumberTolerance) : 0;
            this.resetSeconds = resetSeconds;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Each hit: Armor +").Append(armorPerLayer[0].Value.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(", up to ").Append(maxLayers).Append(" hits (now ")
                .Append(Owner.Statuses.Layers(StatusType.ReactivePlating)).Append(")\n");
        }

        protected internal override void OnDamageTaken(DamageEvent damage)
        {
            if (damage.Outcome == DamageOutcome.Dealt && !damage.IsDamageOverTime && maxLayers > 0)
            {
                Owner.Statuses.TryApply(new StatusApplication(
                    StatusType.ReactivePlating, Owner, resetSeconds, armorPerLayer, maxLayers));
            }
        }
    }
}
