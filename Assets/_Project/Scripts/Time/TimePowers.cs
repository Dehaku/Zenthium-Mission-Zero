using System;
using System.Collections.Generic;

namespace Zenthium.Timing
{
    /// <summary>
    /// Time Slow and Time Stop (time.md): each sets the clock's time-power scale for a number of real seconds.
    /// While several run, the slowest wins, so a Time Stop inside a Time Slow freezes the game, and the slow resumes
    /// if it outlasts the stop. Durations are real seconds that stop while paused, like cooldowns.
    /// </summary>
    public sealed class TimePowers
    {
        private const float NormalScale = 1f;

        private readonly GameClock clock;
        private readonly List<Effect> effects = new List<Effect>();

        public TimePowers(GameClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Whether any time power is running.</summary>
        public bool IsActive => effects.Count > 0;

        /// <param name="scale">0 stops time; 0.5 runs it at half speed.</param>
        /// <param name="realSeconds">How long it lasts, in real seconds.</param>
        public void Start(float scale, float realSeconds)
        {
            if (realSeconds <= 0f)
            {
                return;
            }

            effects.Add(new Effect { Scale = Math.Max(0f, scale), Remaining = realSeconds });
            Apply();
        }

        /// <param name="cooldownDelta">Real seconds since the last tick, 0 while paused (<c>GameClock.CooldownDelta</c>).</param>
        public void Tick(float cooldownDelta)
        {
            if (effects.Count == 0)
            {
                return;
            }

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                Effect effect = effects[i];
                effect.Remaining -= cooldownDelta;
                if (effect.Remaining <= 0f)
                {
                    effects.RemoveAt(i);
                }
                else
                {
                    effects[i] = effect;
                }
            }

            Apply();
        }

        private void Apply()
        {
            float scale = NormalScale;
            foreach (Effect effect in effects)
            {
                scale = Math.Min(scale, effect.Scale);
            }

            clock.SetTimePowerScale(scale);
        }

        private struct Effect
        {
            public float Scale;
            public float Remaining;
        }
    }
}
