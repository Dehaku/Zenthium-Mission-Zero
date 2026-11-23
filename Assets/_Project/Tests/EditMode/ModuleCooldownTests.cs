using NUnit.Framework;
using Zenthium.Modules;
using Zenthium.Timing;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// time.md and CLAUDE.md (Architecture: Time): cooldowns run in real seconds, ignore the game timescale and stop
    /// while paused.
    /// </summary>
    public class ModuleCooldownTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Start_IsNotReadyUntilTheSecondsPass()
        {
            var cooldown = new ModuleCooldown(3f);
            cooldown.Start();

            cooldown.Tick(2.9f);
            Assert.That(cooldown.IsReady, Is.False);

            cooldown.Tick(0.1f);
            Assert.That(cooldown.IsReady, Is.True);
        }

        [Test]
        public void New_IsReady()
        {
            Assert.That(new ModuleCooldown(3f).IsReady, Is.True);
        }

        [Test]
        public void Tick_WithCooldownDelta_IgnoresTimeSlowAndTheGlobalScale()
        {
            var clock = new GameClock();
            clock.SetGlobalScale(0.5f);
            clock.SetTimePowerScale(0f);
            var cooldown = new ModuleCooldown(3f);
            cooldown.Start();

            cooldown.Tick(clock.CooldownDelta(1f));

            Assert.That(cooldown.Remaining, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void Tick_WithCooldownDelta_StopsWhilePaused()
        {
            var clock = new GameClock();
            clock.SetPaused(true);
            var cooldown = new ModuleCooldown(3f);
            cooldown.Start();

            cooldown.Tick(clock.CooldownDelta(1f));

            Assert.That(cooldown.Remaining, Is.EqualTo(3f).Within(Tolerance));
        }
    }
}
