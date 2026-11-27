using NUnit.Framework;
using Zenthium.AI;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// missions.md, Citizens: a Scared citizen flees from an enemy within its 20 m fear range, and drops its
    /// Valuables once, when it first starts to flee (the 6b decision). Other kinds never flee.
    /// </summary>
    public class CitizenFearTests
    {
        [TestCase(CitizenKind.Scared, 5f, true)]
        [TestCase(CitizenKind.Scared, 20f, true)]
        [TestCase(CitizenKind.Scared, 20.5f, false)]
        [TestCase(CitizenKind.Scared, float.PositiveInfinity, false)]
        [TestCase(CitizenKind.Stubborn, 5f, false)]
        [TestCase(CitizenKind.Incapacitated, 5f, false)]
        public void Update_FleesOnlyWhenScaredAndAnEnemyIsInRange(CitizenKind kind, float enemyDistance, bool flees)
        {
            var fear = new CitizenFear(20f);

            bool result = fear.Update(kind, enemyDistance, out _);

            Assert.That(result, Is.EqualTo(flees));
            Assert.That(fear.IsFleeing, Is.EqualTo(flees));
        }

        [Test]
        public void Update_StartingToFlee_DropsValuables()
        {
            var fear = new CitizenFear(20f);

            fear.Update(CitizenKind.Scared, 10f, out bool drops);

            Assert.That(drops, Is.True);
        }

        [Test]
        public void Update_KeepsFleeing_DoesNotDropAgain()
        {
            var fear = new CitizenFear(20f);
            fear.Update(CitizenKind.Scared, 10f, out _);

            fear.Update(CitizenKind.Scared, 8f, out bool drops);

            Assert.That(drops, Is.False);
        }

        [Test]
        public void Update_FleesAgainLater_DoesNotDropAgain()
        {
            var fear = new CitizenFear(20f);
            fear.Update(CitizenKind.Scared, 10f, out _);
            fear.Update(CitizenKind.Scared, float.PositiveInfinity, out _);

            fear.Update(CitizenKind.Scared, 10f, out bool drops);

            Assert.That(drops, Is.False);
        }

        [Test]
        public void Update_NotFleeing_DoesNotDrop()
        {
            var fear = new CitizenFear(20f);

            fear.Update(CitizenKind.Stubborn, 10f, out bool drops);

            Assert.That(drops, Is.False);
        }
    }
}
