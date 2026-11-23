using NUnit.Framework;
using UnityEngine;
using Zenthium.Modules;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Kick reaches targets in an arc around the aim direction, on the XZ plane only.
    /// </summary>
    public class MeleeArcTests
    {
        private const float HalfAngle = 60f;

        [TestCase(0f, 0f, 1f, true, TestName = "Contains_StraightAhead_IsInside")]
        [TestCase(1f, 0f, 1f, true, TestName = "Contains_45DegreesOff_IsInside")]
        [TestCase(1f, 0f, 0f, false, TestName = "Contains_90DegreesOff_IsOutside")]
        [TestCase(0f, 0f, -1f, false, TestName = "Contains_Behind_IsOutside")]
        [TestCase(0f, 5f, 1f, true, TestName = "Contains_HeightIgnored")]
        public void Contains(float x, float y, float z, bool expected)
        {
            bool inside = MeleeArc.Contains(Vector3.zero, Vector3.forward, new Vector3(x, y, z), HalfAngle);

            Assert.That(inside, Is.EqualTo(expected));
        }

        [Test]
        public void Contains_TargetAtTheAttackersPosition_IsInside()
        {
            Assert.That(MeleeArc.Contains(Vector3.one, Vector3.forward, Vector3.one, HalfAngle), Is.True);
        }
    }
}
