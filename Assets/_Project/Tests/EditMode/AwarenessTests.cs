using NUnit.Framework;
using Zenthium.AI;
using Zenthium.Data;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// stats.md, Detection: instant at 10 m, 10 s at 100 m, slower for lower Detectability. enemies.md: the 0.5 s
    /// wait before the alert, no alert if defeated first, and the 3 s drain after searching.
    /// </summary>
    public class AwarenessTests
    {
        internal static AiConstants CreateConstants()
        {
            return new AiConstants(
                alertDelaySeconds: 0.5f,
                alertRadius: 10f,
                searchSeconds: 10f,
                awarenessDrainSeconds: 3f,
                rangedBackoffDistance: 3f,
                allyFollowDistance: 10f,
                telegraphSeconds: 0.5f,
                patrolSpeedFraction: 0.5f);
        }

        internal static Awareness CreateAwareness()
        {
            return new Awareness(CombatTestSetup.CreateRules().Formulas, CreateConstants());
        }

        [Test]
        public void Tick_SeenWithinInstantRange_NoticesAtOnce()
        {
            Awareness awareness = CreateAwareness();

            AwarenessChange change = awareness.Tick(0.01f, awareness.FillRate(10f, 1f));

            Assert.That(change, Is.EqualTo(AwarenessChange.Noticed));
            Assert.That(awareness.Level, Is.EqualTo(AwarenessLevel.Noticing));
        }

        [TestCase(100f, 1f, 9.9f, TestName = "Tick_SeenAt100m_NoticesAfter10Seconds")]
        [TestCase(55f, 1f, 4.9f, TestName = "Tick_SeenAt55m_NoticesAfter5Seconds")]
        [TestCase(100f, 2f, 4.9f, TestName = "Tick_DoubleDetectabilityAt100m_NoticesAfter5Seconds")]
        public void Tick_SeenAtDistance_NoticesWhenTheFillTimeIsUp(float distance, float detectability,
            float justBefore)
        {
            Awareness awareness = CreateAwareness();
            float rate = awareness.FillRate(distance, detectability);

            AwarenessChange before = awareness.Tick(justBefore, rate);
            AwarenessChange after = awareness.Tick(0.2f, rate);

            Assert.That(before, Is.EqualTo(AwarenessChange.None));
            Assert.That(after, Is.EqualTo(AwarenessChange.Noticed));
        }

        [Test]
        public void FillRate_BeyondVisionRange_IsZero()
        {
            Awareness awareness = CreateAwareness();

            Assert.That(awareness.FillRate(101f, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void Tick_NoOneInView_BarDrainsOverThreeSeconds()
        {
            Awareness awareness = CreateAwareness();
            awareness.Tick(6f, awareness.FillRate(100f, 1f));

            awareness.Tick(0.9f, 0f);

            Assert.That(awareness.Bar, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(awareness.Level, Is.EqualTo(AwarenessLevel.Unaware));
        }

        [Test]
        public void Tick_Noticing_AlertGoesOutAfterHalfASecond()
        {
            Awareness awareness = CreateAwareness();
            awareness.Notice();

            AwarenessChange before = awareness.Tick(0.4f, 0f);
            AwarenessChange after = awareness.Tick(0.2f, 0f);

            Assert.That(before, Is.EqualTo(AwarenessChange.None));
            Assert.That(after, Is.EqualTo(AwarenessChange.AlertSent));
            Assert.That(awareness.Level, Is.EqualTo(AwarenessLevel.Aware));
        }

        [Test]
        public void Reset_DefeatedWhileNoticing_NoAlertGoesOut()
        {
            Awareness awareness = CreateAwareness();
            awareness.Notice();

            awareness.Reset();
            AwarenessChange change = awareness.Tick(1f, 0f);

            Assert.That(change, Is.EqualTo(AwarenessChange.None));
            Assert.That(awareness.Level, Is.EqualTo(AwarenessLevel.Unaware));
        }

        [Test]
        public void BeginForgetting_Aware_UnawareAfterThreeSeconds()
        {
            Awareness awareness = CreateAwareness();
            awareness.Notice();
            awareness.Tick(0.5f, 0f);

            awareness.BeginForgetting();
            AwarenessChange before = awareness.Tick(2.9f, 0f);
            AwarenessChange after = awareness.Tick(0.2f, 0f);

            Assert.That(before, Is.EqualTo(AwarenessChange.None));
            Assert.That(after, Is.EqualTo(AwarenessChange.Forgotten));
            Assert.That(awareness.Level, Is.EqualTo(AwarenessLevel.Unaware));
        }

        [Test]
        public void KeepAware_WhileForgetting_StaysAwareWithAFullBar()
        {
            Awareness awareness = CreateAwareness();
            awareness.Notice();
            awareness.Tick(0.5f, 0f);
            awareness.BeginForgetting();
            awareness.Tick(2f, 0f);

            awareness.KeepAware();
            AwarenessChange change = awareness.Tick(5f, 0f);

            Assert.That(change, Is.EqualTo(AwarenessChange.None));
            Assert.That(awareness.Level, Is.EqualTo(AwarenessLevel.Aware));
            Assert.That(awareness.Bar, Is.EqualTo(1f));
        }

        [Test]
        public void Notice_HitWhileUnaware_NoticesAtOnce()
        {
            Awareness awareness = CreateAwareness();

            bool noticed = awareness.Notice();

            Assert.That(noticed, Is.True);
            Assert.That(awareness.Level, Is.EqualTo(AwarenessLevel.Noticing));
        }

        [Test]
        public void ResetBar_Unaware_EmptiesTheBar()
        {
            Awareness awareness = CreateAwareness();
            awareness.Tick(5f, awareness.FillRate(100f, 1f));

            awareness.ResetBar();

            Assert.That(awareness.Bar, Is.EqualTo(0f));
        }
    }
}
