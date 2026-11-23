using UnityEngine;

namespace Zenthium.Timing
{
    /// <summary>
    /// Owns the scene's <see cref="GameClock"/> and is the only thing that writes Unity's <c>Time.timeScale</c>.
    /// Time powers aren't applied to <c>Time.timeScale</c>, since the player must keep moving at full speed.
    /// </summary>
    public sealed class GameClockDriver : MonoBehaviour
    {
        private const float NormalTimeScale = 1f;

        private readonly GameClock clock = new GameClock();

        public GameClock Clock => clock;

        private void OnEnable()
        {
            clock.OnUnityTimeScaleChanged += ApplyTimeScale;
            ApplyTimeScale();
        }

        private void OnDisable()
        {
            clock.OnUnityTimeScaleChanged -= ApplyTimeScale;

            // Don't leave the Editor or the next scene paused or slowed.
            Time.timeScale = NormalTimeScale;
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = clock.UnityTimeScale;
        }
    }
}
