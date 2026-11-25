using System;

namespace Zenthium.Statuses
{
    /// <summary>
    /// The ticks of one damage-over-time effect, such as a Burn: its total damage in equal ticks, one every tick
    /// interval of game time, until its time runs out (statuses.md). Time Slow stretches it and Time Stop freezes it,
    /// since it only advances by the game seconds it's given.
    /// </summary>
    public sealed class DamageOverTime
    {
        // Absorbs float error, so a run of steps that adds up to a whole tick isn't a hair short of it.
        private const float Tolerance = 0.0001f;

        private readonly float tickSeconds;
        private float sinceTick;

        /// <param name="totalDamage">Dealt over the whole duration.</param>
        /// <param name="seconds">How long it lasts, in game seconds.</param>
        /// <param name="tickSeconds">Game seconds between ticks.</param>
        public DamageOverTime(float totalDamage, float seconds, float tickSeconds)
        {
            if (!(seconds > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A DoT needs a duration.");
            }

            if (!(tickSeconds > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(tickSeconds), tickSeconds, "A DoT needs a tick interval.");
            }

            this.tickSeconds = tickSeconds;
            Remaining = seconds;
            DamagePerTick = totalDamage * tickSeconds / seconds;
        }

        public float DamagePerTick { get; }

        /// <summary>Game seconds left.</summary>
        public float Remaining { get; private set; }

        public bool IsFinished => Remaining <= Tolerance;

        /// <summary>Moves it on by game seconds, never past its end.</summary>
        /// <returns>How many ticks fell due in that time.</returns>
        public int Advance(float gameDelta)
        {
            if (gameDelta <= 0f || IsFinished)
            {
                return 0;
            }

            float step = Math.Min(gameDelta, Remaining);
            Remaining -= step;
            sinceTick += step;
            int ticks = 0;
            while (sinceTick + Tolerance >= tickSeconds)
            {
                sinceTick -= tickSeconds;
                ticks++;
            }

            return ticks;
        }
    }
}
