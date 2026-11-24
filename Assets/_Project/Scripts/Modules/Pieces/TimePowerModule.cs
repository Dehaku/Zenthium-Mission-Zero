using System.Globalization;
using System.Text;
using UnityEngine;
using Zenthium.Data;

namespace Zenthium.Modules
{
    /// <summary>
    /// Time Slow and Time Stop (time.md): the game runs at the module's <c>timescale</c> for <c>duration_real_s</c>
    /// real seconds, while the player keeps full speed. A module with no timescale stops time.
    /// </summary>
    public sealed class TimePowerModule : ModuleBehaviour
    {
        // Keys of the time powers' values in modules.csv.
        public const string DurationKey = "duration_real_s";
        public const string TimescaleKey = "timescale";

        // time.md: Time Stop freezes the game.
        private const float StoppedScale = 0f;

        private const string NumberFormat = "0.##";

        private readonly float scale;
        private readonly float realSeconds;

        public TimePowerModule(ModuleData data, float realSeconds) : base(data)
        {
            this.realSeconds = realSeconds;
            scale = data.Values.TryGetNumber(TimescaleKey, out float timescale) ? timescale : StoppedScale;
        }

        /// <returns>False if the module has no duration.</returns>
        public static bool TryGetDuration(ModuleData data, out float realSeconds)
        {
            return data.Values.TryGetNumber(DurationKey, out realSeconds) && realSeconds > 0f;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Game speed x").Append(scale.ToString(NumberFormat, CultureInfo.InvariantCulture))
                .Append(" for ").Append(realSeconds.ToString(NumberFormat, CultureInfo.InvariantCulture))
                .Append(" real s; you keep full speed\n");
        }

        protected override void Activate(Vector3 aimDirection)
        {
            Context.World.Clock.TimePowers.Start(scale, realSeconds);
        }
    }
}
