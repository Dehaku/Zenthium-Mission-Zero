using NUnit.Framework;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>world.md (Fire): each fire tick destroys a random wall, window or door that's still standing.</summary>
    public class FireTargetsTests
    {
        [TestCase(0f, 1, TestName = "PickStanding_LowRoll_FirstStanding")]
        [TestCase(0.99f, 3, TestName = "PickStanding_HighRoll_LastStanding")]
        public void PickStanding_SkipsBrokenParts(float roll, int expected)
        {
            bool[] standing = { false, true, false, true };

            Assert.That(FireTargets.PickStanding(standing, roll), Is.EqualTo(expected));
        }

        [Test]
        public void PickStanding_NoneStanding_PicksNothing()
        {
            Assert.That(FireTargets.PickStanding(new[] { false, false }, 0.5f), Is.EqualTo(-1));
        }
    }
}
