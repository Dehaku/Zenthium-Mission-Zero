using System;
using Zenthium.Data;

namespace Zenthium.AI
{
    /// <summary>
    /// One entity's awareness (stats.md, Detection; enemies.md, Becoming aware and Losing the target), on game time.
    /// Unaware, its bar fills while it sees someone it notices, faster up close and for higher Detectability, and
    /// drains while it sees no one. Full, hit or alerted, it notices: it waits, then its alert goes out and it's
    /// aware. Aware, it can start forgetting: the bar drains, then it's unaware again.
    /// </summary>
    public sealed class Awareness
    {
        // Float sums of small fill steps can land a hair under 1.
        private const float FullBar = 0.9999f;

        private readonly BalanceFormulas formulas;
        private readonly AiConstants constants;

        public Awareness(BalanceFormulas formulas, AiConstants constants)
        {
            this.formulas = formulas ?? throw new ArgumentNullException(nameof(formulas));
            this.constants = constants ?? throw new ArgumentNullException(nameof(constants));
        }

        public AwarenessLevel Level { get; private set; }

        /// <summary>From 0 to 1. Full while noticing, and while aware until it starts forgetting.</summary>
        public float Bar { get; private set; }

        /// <summary>Aware, with the bar draining toward unaware.</summary>
        public bool IsForgetting { get; private set; }

        /// <summary>Game seconds until the alert goes out; 0 unless noticing.</summary>
        public float AlertSecondsLeft { get; private set; }

        /// <summary>How fast the bar fills, per game second, for someone seen at this distance.</summary>
        /// <param name="detectability">Their Detectability multiplier: 1 is the default (stats.md).</param>
        /// <returns>0 beyond vision range or with no Detectability; infinity within the instant range.</returns>
        public float FillRate(float distance, float detectability)
        {
            if (detectability <= 0f)
            {
                return 0f;
            }

            float seconds = formulas.AwarenessFillSeconds(distance);
            if (float.IsPositiveInfinity(seconds))
            {
                return 0f;
            }

            return seconds <= 0f ? float.PositiveInfinity : detectability / seconds;
        }

        /// <param name="gameDelta">Game seconds since the last tick.</param>
        /// <param name="fillRate">The fastest <see cref="FillRate"/> among those it sees now; 0 if none.</param>
        public AwarenessChange Tick(float gameDelta, float fillRate)
        {
            switch (Level)
            {
                case AwarenessLevel.Unaware:
                    if (fillRate <= 0f)
                    {
                        Drain(gameDelta);
                        return AwarenessChange.None;
                    }

                    Bar = float.IsPositiveInfinity(fillRate) ? 1f : Math.Min(1f, Bar + fillRate * gameDelta);
                    return Bar >= FullBar && Notice() ? AwarenessChange.Noticed : AwarenessChange.None;

                case AwarenessLevel.Noticing:
                    AlertSecondsLeft -= gameDelta;
                    if (AlertSecondsLeft > 0f)
                    {
                        return AwarenessChange.None;
                    }

                    AlertSecondsLeft = 0f;
                    Level = AwarenessLevel.Aware;
                    return AwarenessChange.AlertSent;

                default:
                    if (!IsForgetting)
                    {
                        return AwarenessChange.None;
                    }

                    Drain(gameDelta);
                    if (Bar > 0f)
                    {
                        return AwarenessChange.None;
                    }

                    Level = AwarenessLevel.Unaware;
                    IsForgetting = false;
                    return AwarenessChange.Forgotten;
            }
        }

        /// <summary>Hit while unaware, alerted, or the bar filled: it notices at once. Aware, it stops forgetting.</summary>
        /// <returns>True if it was unaware and now notices.</returns>
        public bool Notice()
        {
            if (Level != AwarenessLevel.Unaware)
            {
                KeepAware();
                return false;
            }

            Level = AwarenessLevel.Noticing;
            Bar = 1f;
            AlertSecondsLeft = constants.AlertDelaySeconds;
            return true;
        }

        /// <summary>It sees its target again: an aware entity that was forgetting stays aware.</summary>
        public void KeepAware()
        {
            if (Level == AwarenessLevel.Aware)
            {
                IsForgetting = false;
                Bar = 1f;
            }
        }

        /// <summary>Searching is over: the bar starts draining (enemies.md, Losing the target).</summary>
        public void BeginForgetting()
        {
            if (Level == AwarenessLevel.Aware)
            {
                IsForgetting = true;
            }
        }

        /// <summary>Smoke Bomb: an unaware entity's bar empties. One noticing or aware is unaffected.</summary>
        public void ResetBar()
        {
            if (Level == AwarenessLevel.Unaware)
            {
                Bar = 0f;
            }
        }

        /// <summary>Defeated: back to unaware with an empty bar, so an alert still waiting never goes out.</summary>
        public void Reset()
        {
            Level = AwarenessLevel.Unaware;
            Bar = 0f;
            IsForgetting = false;
            AlertSecondsLeft = 0f;
        }

        private void Drain(float gameDelta)
        {
            float drainSeconds = constants.AwarenessDrainSeconds;
            Bar = drainSeconds > 0f ? Math.Max(0f, Bar - gameDelta / drainSeconds) : 0f;
        }
    }
}
