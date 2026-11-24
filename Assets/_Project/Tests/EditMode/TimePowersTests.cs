using NUnit.Framework;
using Zenthium.Timing;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// time.md: Time Slow and Time Stop set the time-power scale for real seconds; the slowest running one wins.
    /// </summary>
    public class TimePowersTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Slow_SetsTheScale_UntilItsRealSecondsRunOut()
        {
            var clock = new GameClock();
            var powers = new TimePowers(clock);

            powers.Start(0.5f, 10f);
            Assert.That(clock.TimePowerScale, Is.EqualTo(0.5f).Within(Tolerance));
            powers.Tick(clock.CooldownDelta(10f));

            Assert.That(clock.TimePowerScale, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(powers.IsActive, Is.False);
        }

        [Test]
        public void StopDuringSlow_Freezes_ThenTheSlowResumes()
        {
            var clock = new GameClock();
            var powers = new TimePowers(clock);
            powers.Start(0.5f, 10f);

            powers.Start(0f, 5f);
            Assert.That(clock.TimePowerScale, Is.EqualTo(0f), "Stopped");
            powers.Tick(clock.CooldownDelta(5f));

            Assert.That(clock.TimePowerScale, Is.EqualTo(0.5f).Within(Tolerance), "Slow again");
        }

        [Test]
        public void WhilePaused_TimePowersDontRunOut()
        {
            var clock = new GameClock();
            var powers = new TimePowers(clock);
            powers.Start(0f, 5f);
            clock.SetPaused(true);

            powers.Tick(clock.CooldownDelta(10f));

            Assert.That(clock.TimePowerScale, Is.EqualTo(0f));
        }
    }
}
