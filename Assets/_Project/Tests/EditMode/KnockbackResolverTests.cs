using NUnit.Framework;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// combat.md (Knockback): the tier's distance, never scaled by stats; <c>KnockbackResistant</c> blocks it; and
    /// dragged targets can't be moved (modules.md, Drag).
    /// </summary>
    public class KnockbackResolverTests
    {
        private const float Tolerance = 0.0001f;
        private static readonly TierValue NormalKnockback = new TierValue("Normal", 4f);

        private static bool TryKnockback(Entity source, Entity target, out float distance,
            FriendlyFireRules friendlyFire = null, float roll = CombatTestSetup.RollThatMisses)
        {
            friendlyFire = friendlyFire ?? new FriendlyFireRules();
            DamageEvent hit = CombatTestSetup.CreatePipeline(roll, friendlyFire)
                .Apply(new Hit(source, 10f, DamageType.Physical), target);
            return new KnockbackResolver(friendlyFire).TryGetDistance(NormalKnockback, hit, out distance);
        }

        [Test]
        public void TryGetDistance_HitLands_UsesTheTierDistance()
        {
            Entity source = CombatTestSetup.CreateEntity(Team.Player, StatBlock.Uniform(90f));

            bool knocked = TryKnockback(source, CombatTestSetup.CreateEntity(Team.Enemy), out float distance);

            Assert.That(knocked, Is.True);
            Assert.That(distance, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void TryGetDistance_KnockbackResistant_DoesNotMove()
        {
            var setup = new EntitySetup(Team.Enemy, StatBlock.Uniform(10f));
            setup.ModuleTags.Add(ModuleTag.KnockbackResistant);
            var target = new Entity(setup, CombatTestSetup.CreateRules(), new FixedRandom(CombatTestSetup.RollThatMisses));

            bool knocked = TryKnockback(CombatTestSetup.CreateEntity(Team.Player), target, out float distance);

            Assert.That(knocked, Is.False);
            Assert.That(distance, Is.EqualTo(0f));
        }

        [Test]
        public void TryGetDistance_DraggedTarget_DoesNotMove()
        {
            Entity source = CombatTestSetup.CreateEntity(Team.Player);
            Entity target = CombatTestSetup.CreateEntity(Team.Enemy);
            target.SetDraggedBy(CombatTestSetup.CreateEntity(Team.Ally));

            Assert.That(TryKnockback(source, target, out _), Is.False);
        }

        [Test]
        public void TryGetDistance_Dodged_DoesNotMove()
        {
            Entity source = CombatTestSetup.CreateEntity(Team.Player);

            // Roll 0 always dodges the target's 5% dodge chance.
            Assert.That(TryKnockback(source, CombatTestSetup.CreateEntity(Team.Enemy), out _, roll: 0f), Is.False);
        }

        [Test]
        public void TryGetDistance_IncapacitatedEnemy_StillMoves()
        {
            Entity target = CombatTestSetup.CreateEntity(Team.Enemy);
            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(1000f), target);

            bool knocked = TryKnockback(CombatTestSetup.CreateEntity(Team.Player), target, out float distance);

            Assert.That(knocked, Is.True);
            Assert.That(distance, Is.EqualTo(4f).Within(Tolerance));
        }

        [TestCase(false, false, TestName = "TryGetDistance_IncapacitatedAlly_FriendlyFireOff_DoesNotMove")]
        [TestCase(true, true, TestName = "TryGetDistance_IncapacitatedAlly_FriendlyFireHeld_Moves")]
        public void TryGetDistance_IncapacitatedAlly_FollowsFriendlyFire(bool friendlyFireHeld, bool expected)
        {
            Entity target = CombatTestSetup.CreateEntity(Team.Ally);
            CombatTestSetup.CreatePipeline().Apply(CombatTestSetup.Environmental(1000f), target);
            var friendlyFire = new FriendlyFireRules { IsPlayerFriendlyFireHeld = friendlyFireHeld };

            bool knocked = TryKnockback(CombatTestSetup.CreateEntity(Team.Player), target, out _, friendlyFire);

            Assert.That(knocked, Is.EqualTo(expected));
        }

        [Test]
        public void TryGetDistance_FriendlyFireForbidsTheHit_DoesNotMove()
        {
            Entity source = CombatTestSetup.CreateEntity(Team.Player);

            Assert.That(TryKnockback(source, CombatTestSetup.CreateEntity(Team.Civilian), out _), Is.False);
        }

        [Test]
        public void TryGetDistance_NoKnockbackTier_DoesNotMove()
        {
            Entity source = CombatTestSetup.CreateEntity(Team.Player);
            Entity target = CombatTestSetup.CreateEntity(Team.Enemy);
            DamageEvent hit = CombatTestSetup.CreatePipeline().Apply(new Hit(source, 10f, DamageType.Physical), target);

            bool knocked = new KnockbackResolver(new FriendlyFireRules()).TryGetDistance(default, hit, out _);

            Assert.That(knocked, Is.False);
        }
    }
}
