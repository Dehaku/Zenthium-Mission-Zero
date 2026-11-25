using System;
using Zenthium.Data;

namespace Zenthium.World
{
    public enum BuildingFireState
    {
        Unburned,
        Burning,
        BurnedOut,
    }

    /// <summary>What fell due in one <see cref="BuildingFire.Advance"/>.</summary>
    public readonly struct FireStep
    {
        public FireStep(int burnTicks, int partBreaks, int spreadRolls, bool burnedOut)
        {
            BurnTicks = burnTicks;
            PartBreaks = partBreaks;
            SpreadRolls = spreadRolls;
            BurnedOut = burnedOut;
        }

        /// <summary>Burn ticks: each gives everything inside a Burn.</summary>
        public int BurnTicks { get; }

        /// <summary>Each destroys one standing wall, window or door.</summary>
        public int PartBreaks { get; }

        /// <summary>Spread rolls against each unburned neighbor (if spreading is on).</summary>
        public int SpreadRolls { get; }

        /// <summary>It burned out during this step.</summary>
        public bool BurnedOut { get; }
    }

    /// <summary>
    /// A building's fire (world.md, Fire): unburned, burning or burned out. While burning, a Burn tick falls due every
    /// tick interval, a part break every break interval (a local design decision: walls broke too fast at the tick
    /// interval) and a spread roll every spread interval, all counted from when it caught fire; at the burn-out time
    /// it's burned out for good, with nothing at that moment. Putting it out makes it unburned again, and a
    /// later fire starts its clock over. Time comes in as game seconds, so Time Slow and Time Stop affect it.
    /// </summary>
    public sealed class BuildingFire
    {
        private readonly FireConstants constants;
        private int ticksDone;
        private int breaksDone;
        private int rollsDone;

        public BuildingFire(FireConstants constants)
        {
            this.constants = constants ?? throw new ArgumentNullException(nameof(constants));
        }

        public BuildingFireState State { get; private set; } = BuildingFireState.Unburned;

        /// <summary>Game seconds since it caught fire; 0 unless burning.</summary>
        public float SecondsBurning { get; private set; }

        /// <returns>True if it caught fire: only an unburned building can.</returns>
        public bool Ignite()
        {
            if (State != BuildingFireState.Unburned)
            {
                return false;
            }

            State = BuildingFireState.Burning;
            SecondsBurning = 0f;
            ticksDone = 0;
            breaksDone = 0;
            rollsDone = 0;
            return true;
        }

        /// <returns>True if it was burning and is now out.</returns>
        public bool Extinguish()
        {
            if (State != BuildingFireState.Burning)
            {
                return false;
            }

            State = BuildingFireState.Unburned;
            SecondsBurning = 0f;
            return true;
        }

        public FireStep Advance(float gameDelta)
        {
            if (State != BuildingFireState.Burning || gameDelta <= 0f)
            {
                return default;
            }

            SecondsBurning += gameDelta;
            bool burnedOut = SecondsBurning >= constants.BurnOutSeconds;

            // Events at the burn-out moment itself don't happen: it's no longer burning.
            float counted = burnedOut ? constants.BurnOutSeconds : SecondsBurning;
            int ticks = Due(counted, constants.TickSeconds, burnedOut) - ticksDone;
            int breaks = Due(counted, constants.BreakIntervalSeconds, burnedOut) - breaksDone;
            int rolls = Due(counted, constants.SpreadIntervalSeconds, burnedOut) - rollsDone;
            ticksDone += ticks;
            breaksDone += breaks;
            rollsDone += rolls;
            if (burnedOut)
            {
                State = BuildingFireState.BurnedOut;
                SecondsBurning = 0f;
            }

            return new FireStep(ticks, breaks, rolls, burnedOut);
        }

        /// <summary>Whole intervals in the time, leaving out one ending exactly at it if asked.</summary>
        private static int Due(float seconds, float interval, bool excludeEnd)
        {
            if (!(interval > 0f))
            {
                return 0;
            }

            int count = (int)Math.Floor(seconds / interval);
            if (excludeEnd && count > 0 && count * interval >= seconds)
            {
                count--;
            }

            return count;
        }
    }
}
