using NUnit.Framework;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Statuses;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// The Captured state (Stasis Cuffs, the user's design decision): only an Incapacitated entity can be captured,
    /// and after that nothing affects it: no damage, knockback, statuses or revive.
    /// </summary>
    public class CaptureTests
    {
        private static readonly TierValue NormalKnockback = new TierValue("Normal", 4f);

        private static Entity CreateCapturedGoon()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(1000f), goon);
            goon.Capture();
            return goon;
        }

        [Test]
        public void Capture_StandingEntity_IsRefused()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);

            Assert.That(goon.Capture(), Is.False);
            Assert.That(goon.IsCaptured, Is.False);
        }

        [Test]
        public void Capture_EndsItsStatuses()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(1000f), goon);
            goon.Statuses.TryApply(new StatusApplication(StatusType.Marked, null, 10f));

            bool captured = goon.Capture();

            Assert.That(captured, Is.True);
            Assert.That(goon.Statuses.Has(StatusType.Marked), Is.False);
        }

        [Test]
        public void Captured_TakesNoNewStatuses()
        {
            Entity goon = CreateCapturedGoon();

            Assert.That(goon.Statuses.TryApply(new StatusApplication(StatusType.Root, null, 3f)), Is.False);
            Assert.That(goon.Statuses.SetInsideArea(new StatusApplication(StatusType.Stun, null,
                StatusApplication.UntilRemoved)), Is.False);
        }

        [Test]
        public void Captured_CantBeRevived()
        {
            Entity goon = CreateCapturedGoon();

            Assert.That(goon.Revive(50f), Is.False);
            Assert.That(goon.IsIncapacitated, Is.True);
        }

        [Test]
        public void Captured_IsNotKnockedBack_EvenWhenFriendlyFireAllows()
        {
            Entity goon = CreateCapturedGoon();
            Entity hero = CombatTestSetup.CreateEntity(Team.Player);
            var friendlyFire = new FriendlyFireRules();
            DamageEvent kick = CombatTestSetup.CreatePipeline(friendlyFire: friendlyFire)
                .Apply(new Hit(hero, 50f, DamageType.Physical), goon);

            bool knocked = new KnockbackResolver(friendlyFire).TryGetDistance(NormalKnockback, kick, out _);

            Assert.That(kick.Outcome, Is.EqualTo(DamageOutcome.NotAllowed), "No damage");
            Assert.That(knocked, Is.False);
        }
    }
}
