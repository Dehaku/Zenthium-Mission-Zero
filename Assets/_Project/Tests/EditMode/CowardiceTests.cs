using NUnit.Framework;
using Zenthium.AI;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// enemies.md, Cowardice: a Coward rolls once when it drops below 20% HP, with a 25% chance to panic.
    /// </summary>
    public class CowardiceTests
    {
        private static Cowardice CreateCowardice()
        {
            return new Cowardice(0.2f, 0.25f);
        }

        [TestCase(0.1f, true)]
        [TestCase(0.24f, true)]
        [TestCase(0.25f, false)]
        [TestCase(0.9f, false)]
        public void Check_BelowThreshold_PanicsOnlyWhenRollIsUnderChance(float roll, bool panics)
        {
            Cowardice cowardice = CreateCowardice();

            bool result = cowardice.Check(19f, 100f, new FixedRandom(roll));

            Assert.That(result, Is.EqualTo(panics));
            Assert.That(cowardice.IsPanicking, Is.EqualTo(panics));
            Assert.That(cowardice.HasRolled, Is.True);
        }

        [TestCase(20f)]
        [TestCase(60f)]
        [TestCase(100f)]
        public void Check_AtOrAboveThreshold_DoesNotRoll(float hp)
        {
            Cowardice cowardice = CreateCowardice();

            bool result = cowardice.Check(hp, 100f, new FixedRandom(0f));

            Assert.That(result, Is.False);
            Assert.That(cowardice.HasRolled, Is.False);
        }

        [Test]
        public void Check_AfterAFailedRoll_NeverRollsAgain()
        {
            Cowardice cowardice = CreateCowardice();
            cowardice.Check(15f, 100f, new FixedRandom(0.9f));

            bool result = cowardice.Check(5f, 100f, new FixedRandom(0f));

            Assert.That(result, Is.False);
            Assert.That(cowardice.IsPanicking, Is.False);
        }

        [Test]
        public void Check_AfterPanicking_StaysPanicked()
        {
            Cowardice cowardice = CreateCowardice();
            cowardice.Check(15f, 100f, new FixedRandom(0f));

            bool result = cowardice.Check(10f, 100f, new FixedRandom(0.9f));

            Assert.That(result, Is.False);
            Assert.That(cowardice.IsPanicking, Is.True);
        }

        [Test]
        public void Check_Incapacitated_DoesNotRoll()
        {
            Cowardice cowardice = CreateCowardice();

            cowardice.Check(0f, 100f, new FixedRandom(0f));

            Assert.That(cowardice.HasRolled, Is.False);
        }
    }
}
