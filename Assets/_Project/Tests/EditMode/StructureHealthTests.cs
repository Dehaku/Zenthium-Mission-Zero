using NUnit.Framework;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>world.md: walls, doors and props have HP, and break once at 0.</summary>
    public class StructureHealthTests
    {
        [Test]
        public void TakeDamage_LowersHpWithoutBreaking()
        {
            var health = new StructureHealth(100f);

            float lost = health.TakeDamage(30f);

            Assert.That(lost, Is.EqualTo(30f));
            Assert.That(health.Current, Is.EqualTo(70f));
            Assert.That(health.IsBroken, Is.False);
        }

        [Test]
        public void TakeDamage_MoreThanLeft_BreaksItOnce()
        {
            var health = new StructureHealth(100f);
            int broken = 0;
            health.OnBroken += () => broken++;

            float lost = health.TakeDamage(250f);
            health.TakeDamage(10f);

            Assert.That(lost, Is.EqualTo(100f));
            Assert.That(health.IsBroken, Is.True);
            Assert.That(broken, Is.EqualTo(1));
        }

        [Test]
        public void TakeDamage_OnceBroken_LosesNothing()
        {
            var health = new StructureHealth(10f);
            health.TakeDamage(10f);

            float lost = health.TakeDamage(5f);

            Assert.That(lost, Is.EqualTo(0f));
            Assert.That(health.Current, Is.EqualTo(0f));
        }

        [Test]
        public void Break_AtFullHp_BreaksIt()
        {
            var health = new StructureHealth(100f);

            health.Break();

            Assert.That(health.IsBroken, Is.True);
        }
    }
}
