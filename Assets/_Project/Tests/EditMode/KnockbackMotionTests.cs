using NUnit.Framework;
using Zenthium.Movement;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// combat.md (Knockback): a fixed distance per tier, and it can't pass through what's in the way.
    /// </summary>
    public class KnockbackMotionTests
    {
        private const float Tolerance = 0.0001f;
        private const float NothingInTheWay = float.PositiveInfinity;

        [Test]
        public void Step_UnevenSteps_TravelExactlyTheDistance()
        {
            var motion = new KnockbackMotion();
            motion.Start(4f);

            float travelled = 0f;
            foreach (float travel in new[] { 0.3f, 1.7f, 0.5f, 1.1f, 2f, 2f })
            {
                travelled += motion.Step(travel, NothingInTheWay).Distance;
            }

            Assert.That(travelled, Is.EqualTo(4f).Within(Tolerance));
            Assert.That(motion.IsActive, Is.False);
        }

        [Test]
        public void Step_ObstacleCloserThanTheStep_StopsAtTheObstacle()
        {
            var motion = new KnockbackMotion();
            motion.Start(4f);
            motion.Step(1f, NothingInTheWay);

            KnockbackStep step = motion.Step(1f, 0.4f);

            Assert.That(step.Distance, Is.EqualTo(0.4f).Within(Tolerance));
            Assert.That(step.HitObstacle, Is.True);
            Assert.That(step.UnusedDistance, Is.EqualTo(2.6f).Within(Tolerance));
            Assert.That(motion.IsActive, Is.False);
        }

        [Test]
        public void Step_ObstacleBeyondTheStep_DoesNotStop()
        {
            var motion = new KnockbackMotion();
            motion.Start(4f);

            KnockbackStep step = motion.Step(1f, 1.5f);

            Assert.That(step.Distance, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(step.HitObstacle, Is.False);
            Assert.That(motion.Remaining, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void Step_OneHugeStep_NeverOvershoots()
        {
            var motion = new KnockbackMotion();
            motion.Start(4f);

            KnockbackStep step = motion.Step(100f, NothingInTheWay);

            Assert.That(step.Distance, Is.EqualTo(4f).Within(Tolerance));
            Assert.That(step.HitObstacle, Is.False);
        }

        [Test]
        public void Step_Inactive_DoesNotMove()
        {
            var motion = new KnockbackMotion();

            KnockbackStep step = motion.Step(1f, NothingInTheWay);

            Assert.That(step.Distance, Is.EqualTo(0f));
            Assert.That(step.HitObstacle, Is.False);
        }

        [Test]
        public void Start_DuringAKnockback_ReplacesIt()
        {
            var motion = new KnockbackMotion();
            motion.Start(4f);
            motion.Step(3f, NothingInTheWay);

            motion.Start(8f);

            Assert.That(motion.Remaining, Is.EqualTo(8f).Within(Tolerance));
        }
    }
}
