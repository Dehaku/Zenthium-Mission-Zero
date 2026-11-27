using Zenthium.Data;

namespace Zenthium.World
{
    /// <summary>
    /// The Repo Agency's Mothership Strike (Factions/GalacticRepoAgency.md), from the faction's factions.csv values:
    /// how often it fires, the countdown before it lands, its damage and its radius.
    /// </summary>
    public sealed class MothershipRules
    {
        public const string IntervalKey = "mothership_interval_s";
        public const string CountdownKey = "mothership_countdown_s";
        public const string DamageKey = "mothership_damage";
        public const string RadiusKey = "mothership_radius_m";

        public MothershipRules(float intervalSeconds, float countdownSeconds, float damage, float radius)
        {
            IntervalSeconds = intervalSeconds;
            CountdownSeconds = countdownSeconds;
            Damage = damage;
            Radius = radius;
        }

        /// <summary>Game seconds between strikes.</summary>
        public float IntervalSeconds { get; }

        /// <summary>Game seconds the red ring counts down before the beam lands.</summary>
        public float CountdownSeconds { get; }

        public float Damage { get; }

        /// <summary>Meters.</summary>
        public float Radius { get; }

        /// <returns>False if the faction lacks one of the values, named in <paramref name="problem"/>.</returns>
        public static bool TryCreate(FactionData faction, out MothershipRules rules, out string problem)
        {
            rules = null;
            problem = null;
            if (faction == null)
            {
                problem = "no faction";
                return false;
            }

            DataValueList values = faction.Values;
            if (!values.TryGetNumber(IntervalKey, out float interval) || !values.TryGetNumber(CountdownKey, out float countdown)
                || !values.TryGetNumber(DamageKey, out float damage) || !values.TryGetNumber(RadiusKey, out float radius)
                || interval <= 0f)
            {
                problem = $"{faction.Id} needs {IntervalKey} (above 0), {CountdownKey}, {DamageKey} and {RadiusKey} "
                    + "in factions.csv";
                return false;
            }

            rules = new MothershipRules(interval, countdown, damage, radius);
            return true;
        }
    }

    /// <summary>When the next Mothership Strike starts: every interval of game time, the first one interval in.</summary>
    public sealed class MothershipTimer
    {
        private readonly float interval;

        public MothershipTimer(float intervalSeconds)
        {
            interval = intervalSeconds;
            SecondsLeft = intervalSeconds;
        }

        /// <summary>Game seconds until the next strike starts.</summary>
        public float SecondsLeft { get; private set; }

        /// <returns>True if a strike starts this step. Time left over counts toward the next one.</returns>
        public bool Tick(float gameDelta)
        {
            SecondsLeft -= gameDelta;
            if (SecondsLeft > 0f)
            {
                return false;
            }

            SecondsLeft += interval;
            return true;
        }

        /// <summary>Switched off and on again: the next strike is a full interval away.</summary>
        public void Restart()
        {
            SecondsLeft = interval;
        }
    }
}
