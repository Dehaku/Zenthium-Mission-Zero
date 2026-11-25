using NUnit.Framework;
using Zenthium.Data;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// world.md (Fire): a burning building gives Burn every 5 s, breaks a wall, window or door every 10 s (a local
    /// design decision), rolls to spread every 15 s, and burns out after 60 s.
    /// Putting it out makes it unburned again; a burned-out building can't burn again.
    /// </summary>
    public class BuildingFireTests
    {
        private static BuildingFire CreateFire()
        {
            return new BuildingFire(new FireConstants(
                tickSeconds: 5f,
                breakIntervalSeconds: 10f,
                burnDamage: 20f,
                burnSeconds: 4f,
                burnOutSeconds: 60f,
                spreadIntervalSeconds: 15f,
                spreadChance: 0.25f,
                propBurnDurationMultiplier: 10f));
        }

        [Test]
        public void Advance_Unburned_NothingHappens()
        {
            BuildingFire fire = CreateFire();

            FireStep step = fire.Advance(30f);

            Assert.That(step.BurnTicks, Is.EqualTo(0));
            Assert.That(fire.State, Is.EqualTo(BuildingFireState.Unburned));
        }

        [TestCase(4.9f, 0)]
        [TestCase(5f, 1)]
        [TestCase(12f, 2)]
        public void Advance_Burning_TicksEvery5Seconds(float seconds, int expectedTicks)
        {
            BuildingFire fire = CreateFire();
            fire.Ignite();

            Assert.That(fire.Advance(seconds).BurnTicks, Is.EqualTo(expectedTicks));
        }

        [Test]
        public void Advance_InSteps_CountsEachTickOnce()
        {
            BuildingFire fire = CreateFire();
            fire.Ignite();

            int ticks = fire.Advance(3f).BurnTicks + fire.Advance(3f).BurnTicks + fire.Advance(3f).BurnTicks;

            Assert.That(ticks, Is.EqualTo(1));
        }

        [TestCase(9.9f, 0)]
        [TestCase(10f, 1)]
        [TestCase(25f, 2)]
        public void Advance_Burning_BreaksAPartEvery10Seconds(float seconds, int expectedBreaks)
        {
            // A local design decision: walls broke too fast at the 5 s tick.
            BuildingFire fire = CreateFire();
            fire.Ignite();

            FireStep step = fire.Advance(seconds);

            Assert.That(step.PartBreaks, Is.EqualTo(expectedBreaks));
        }

        [Test]
        public void Advance_To60Seconds_BurnsOutAfter11Ticks5BreaksAnd3Rolls()
        {
            // Nothing happens at the burn-out moment itself.
            BuildingFire fire = CreateFire();
            fire.Ignite();

            FireStep step = fire.Advance(75f);

            Assert.That(step.BurnedOut, Is.True);
            Assert.That(step.BurnTicks, Is.EqualTo(11));
            Assert.That(step.PartBreaks, Is.EqualTo(5));
            Assert.That(step.SpreadRolls, Is.EqualTo(3));
            Assert.That(fire.State, Is.EqualTo(BuildingFireState.BurnedOut));
        }

        [Test]
        public void Ignite_BurnedOut_CantBurnAgain()
        {
            BuildingFire fire = CreateFire();
            fire.Ignite();
            fire.Advance(60f);

            Assert.That(fire.Ignite(), Is.False);
            Assert.That(fire.State, Is.EqualTo(BuildingFireState.BurnedOut));
        }

        [Test]
        public void Extinguish_Burning_IsUnburnedAndCanBurnAgainFromTheStart()
        {
            BuildingFire fire = CreateFire();
            fire.Ignite();
            fire.Advance(50f);

            bool putOut = fire.Extinguish();
            fire.Ignite();
            FireStep step = fire.Advance(20f);

            Assert.That(putOut, Is.True);
            Assert.That(step.BurnedOut, Is.False, "Its burn-out clock started over");
            Assert.That(step.BurnTicks, Is.EqualTo(4));
        }

        [Test]
        public void Extinguish_Unburned_DoesNothing()
        {
            Assert.That(CreateFire().Extinguish(), Is.False);
        }
    }
}
