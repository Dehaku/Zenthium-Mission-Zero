using System;

namespace Zenthium.Modules
{
    /// <summary>
    /// A module's cooldown (modules.md, time.md). Tick it with <c>GameClock.CooldownDelta</c>: real seconds that ignore
    /// the game timescale and stop while paused.
    /// </summary>
    public sealed class ModuleCooldown
    {
        public ModuleCooldown(float seconds)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A cooldown can't be negative.");
            }

            Seconds = seconds;
        }

        public float Seconds { get; }
        public float Remaining { get; private set; }
        public bool IsReady => Remaining <= 0f;

        public void Start()
        {
            Remaining = Seconds;
        }

        /// <param name="cooldownDelta">Real seconds since the last tick, 0 while paused.</param>
        public void Tick(float cooldownDelta)
        {
            if (Remaining > 0f)
            {
                Remaining = Math.Max(0f, Remaining - cooldownDelta);
            }
        }
    }
}
