using NUnit.Framework;
using Zenthium.Data;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// world.md (Carrying): Carrying Treasure is -5% move speed per Valuable, capped at -50%; the icon grows 5% per
    /// Valuable, capped at +50%. Other entities' pickups take 5 s and damage starts them over.
    /// </summary>
    public class ValuableRulesTests
    {
        private const float Tolerance = 0.0001f;

        private static ValuableConstants CreateConstants()
        {
            return new ValuableConstants(
                slowPerValuable: 0.05f,
                slowCap: 0.5f,
                iconGrowthPerValuable: 0.05f,
                iconGrowthCap: 0.5f,
                pickupSeconds: 5f);
        }

        [TestCase(0, 0)]
        [TestCase(4, 4)]
        [TestCase(10, 10)]
        [TestCase(25, 10)]
        public void CarryingTreasureLayers_OnePerValuable_UpToTheCap(int count, int expected)
        {
            Assert.That(ValuableRules.CarryingTreasureLayers(count, CreateConstants()), Is.EqualTo(expected));
        }

        [TestCase(1, 1.05f)]
        [TestCase(4, 1.2f)]
        [TestCase(30, 1.5f)]
        public void IconScale_GrowsPerValuable_UpToTheCap(int count, float expected)
        {
            Assert.That(ValuableRules.IconScale(count, CreateConstants()), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void PickupTimer_CompletesAfterItsTime()
        {
            var timer = new ValuablePickupTimer(5f);
            timer.Start();

            Assert.That(timer.Advance(4f), Is.False);
            Assert.That(timer.Advance(1f), Is.True);
            Assert.That(timer.IsRunning, Is.False);
        }

        [Test]
        public void PickupTimer_DamageStartsItOver()
        {
            var timer = new ValuablePickupTimer(5f);
            timer.Start();
            timer.Advance(4f);

            timer.OnDamaged();

            Assert.That(timer.Advance(4f), Is.False);
            Assert.That(timer.Advance(1f), Is.True);
        }
    }
}
