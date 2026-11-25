using NUnit.Framework;
using Zenthium.Statuses;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// statuses.md: a DoT deals its damage in equal ticks on game time (0.5 s apart, a local design decision), so
    /// Time Slow stretches it and Time Stop freezes it.
    /// </summary>
    public class DamageOverTimeTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void DamagePerTick_SplitsTheTotalEvenly()
        {
            // world.md: Burn 20 over 4 s.
            var burn = new DamageOverTime(20f, 4f, 0.5f);

            Assert.That(burn.DamagePerTick, Is.EqualTo(2.5f).Within(Tolerance));
        }

        [Test]
        public void Advance_InSmallSteps_TicksEightTimesOver4Seconds()
        {
            var burn = new DamageOverTime(20f, 4f, 0.5f);
            int ticks = 0;

            for (int i = 0; i < 40; i++)
            {
                ticks += burn.Advance(0.1f);
            }

            Assert.That(ticks, Is.EqualTo(8));
            Assert.That(burn.IsFinished, Is.True);
        }

        [Test]
        public void Advance_PastTheEnd_StopsAtTheDuration()
        {
            var burn = new DamageOverTime(20f, 4f, 0.5f);

            int ticks = burn.Advance(30f);

            Assert.That(ticks, Is.EqualTo(8));
            Assert.That(burn.Advance(1f), Is.EqualTo(0));
        }

        [Test]
        public void Advance_ZeroTime_NoTicks()
        {
            // Time Stop gives the DoT no game time.
            var burn = new DamageOverTime(20f, 4f, 0.5f);

            Assert.That(burn.Advance(0f), Is.EqualTo(0));
            Assert.That(burn.Remaining, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void Advance_BetweenTicks_WaitsForTheNext()
        {
            var burn = new DamageOverTime(20f, 4f, 0.5f);

            Assert.That(burn.Advance(0.4f), Is.EqualTo(0));
            Assert.That(burn.Advance(0.2f), Is.EqualTo(1));
        }
    }
}
