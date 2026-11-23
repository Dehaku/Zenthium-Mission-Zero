using NUnit.Framework;
using Zenthium.Timing;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// time.md and CLAUDE.md (Architecture: Time): time powers exempt the player, cooldowns run in real seconds,
    /// and pausing stops everything.
    /// </summary>
    public class GameClockTests
    {
        private const float Tolerance = 0.0001f;

        [TestCase(false, 0.25f, TestName = "GameDelta_TimeSlow_SlowsEveryoneElse")]
        [TestCase(true, 1f, TestName = "GameDelta_TimeSlow_PlayerUnaffected")]
        public void GameDelta_TimeSlow(bool exempt, float expected)
        {
            var clock = new GameClock();
            clock.SetTimePowerScale(0.25f);

            Assert.That(clock.GameDelta(1f, exempt), Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(false, 0f, TestName = "GameDelta_TimeStop_FreezesEveryoneElse")]
        [TestCase(true, 1f, TestName = "GameDelta_TimeStop_PlayerUnaffected")]
        public void GameDelta_TimeStop(bool exempt, float expected)
        {
            var clock = new GameClock();
            clock.SetTimePowerScale(0f);

            Assert.That(clock.GameDelta(1f, exempt), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void GameDelta_GlobalScale_SlowsThePlayerToo()
        {
            var clock = new GameClock();
            clock.SetGlobalScale(0.5f);

            Assert.That(clock.GameDelta(1f, exemptFromTimePowers: true), Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void CooldownDelta_IgnoresBothScales()
        {
            var clock = new GameClock();
            clock.SetGlobalScale(0.5f);
            clock.SetTimePowerScale(0f);

            Assert.That(clock.CooldownDelta(1f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Paused_StopsGameTimeAndCooldowns()
        {
            var clock = new GameClock();
            clock.SetPaused(true);

            Assert.That(clock.GameDelta(1f, exemptFromTimePowers: true), Is.EqualTo(0f));
            Assert.That(clock.CooldownDelta(1f), Is.EqualTo(0f));
            Assert.That(clock.UnityTimeScale, Is.EqualTo(0f));
        }

        [Test]
        public void UnityTimeScale_FollowsTheGlobalScaleButNotTimePowers()
        {
            var clock = new GameClock();
            clock.SetGlobalScale(0.5f);
            clock.SetTimePowerScale(0f);

            Assert.That(clock.UnityTimeScale, Is.EqualTo(0.5f).Within(Tolerance));
        }
    }
}
