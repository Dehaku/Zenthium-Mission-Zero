using System;

namespace Zenthium.World
{
    /// <summary>
    /// A pickup by anyone but the player (world.md, Picking up): it takes a while, and any damage taken starts it
    /// over. Milestone 6's AI drives it; the player picks up at once instead.
    /// </summary>
    public sealed class ValuablePickupTimer
    {
        private readonly float seconds;
        private float elapsed;

        /// <param name="seconds">Game seconds a pickup takes.</param>
        public ValuablePickupTimer(float seconds)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A pickup can't take negative time.");
            }

            this.seconds = seconds;
        }

        public bool IsRunning { get; private set; }

        /// <summary>From 0 to 1.</summary>
        public float Progress => seconds > 0f ? Math.Min(1f, elapsed / seconds) : 1f;

        public void Start()
        {
            IsRunning = true;
            elapsed = 0f;
        }

        public void Cancel()
        {
            IsRunning = false;
            elapsed = 0f;
        }

        /// <summary>Damage taken while picking up: the pickup starts over.</summary>
        public void OnDamaged()
        {
            if (IsRunning)
            {
                elapsed = 0f;
            }
        }

        /// <returns>True once, when the pickup completes.</returns>
        public bool Advance(float gameDelta)
        {
            if (!IsRunning)
            {
                return false;
            }

            elapsed += Math.Max(0f, gameDelta);
            if (elapsed < seconds)
            {
                return false;
            }

            Cancel();
            return true;
        }
    }
}
