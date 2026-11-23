using System;

// The namespace isn't Zenthium.Time: that would hide UnityEngine.Time in every Zenthium.* namespace.
namespace Zenthium.Timing
{
    /// <summary>
    /// The game's timescale (time.md; CLAUDE.md, Architecture: Time). Pure logic; <see cref="GameClockDriver"/>
    /// applies it to Unity.
    /// <list type="bullet">
    /// <item>Pause stops everything, cooldowns included.</item>
    /// <item>The global scale slows everyone, the player too, such as the player's Defiance slowdown.</item>
    /// <item>The time-power scale is Time Slow and Time Stop. Entities exempt from time powers (the player) ignore it.</item>
    /// <item>Cooldowns and resource regen run in real seconds, ignoring both scales.</item>
    /// </list>
    /// </summary>
    public sealed class GameClock
    {
        public bool IsPaused { get; private set; }

        /// <summary>Slows everyone. 1 is normal speed.</summary>
        public float GlobalScale { get; private set; } = 1f;

        /// <summary>Time Slow and Time Stop: below 1 is slower, 0 is stopped. Exempt entities ignore it.</summary>
        public float TimePowerScale { get; private set; } = 1f;

        /// <summary>The value for Unity's <c>Time.timeScale</c>, which drives physics and animation.</summary>
        public float UnityTimeScale => IsPaused ? 0f : GlobalScale;

        /// <summary>Raised when <see cref="UnityTimeScale"/> may have changed.</summary>
        public event Action OnUnityTimeScaleChanged;

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            OnUnityTimeScaleChanged?.Invoke();
        }

        public void SetGlobalScale(float scale)
        {
            GlobalScale = Math.Max(0f, scale);
            OnUnityTimeScaleChanged?.Invoke();
        }

        public void SetTimePowerScale(float scale)
        {
            TimePowerScale = Math.Max(0f, scale);
        }

        /// <summary>The timescale an entity runs at.</summary>
        public float Scale(bool exemptFromTimePowers)
        {
            if (IsPaused)
            {
                return 0f;
            }

            return exemptFromTimePowers ? GlobalScale : GlobalScale * TimePowerScale;
        }

        /// <summary>Game seconds that pass for an entity: for movement, status durations, DoT ticks and timers.</summary>
        public float GameDelta(float realDelta, bool exemptFromTimePowers)
        {
            return realDelta * Scale(exemptFromTimePowers);
        }

        /// <summary>Seconds that pass for cooldowns and resource regen: real time, stopped only while paused.</summary>
        public float CooldownDelta(float realDelta)
        {
            return IsPaused ? 0f : realDelta;
        }
    }
}
