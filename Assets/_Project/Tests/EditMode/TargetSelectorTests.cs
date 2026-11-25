using NUnit.Framework;
using Zenthium.AI;
using Zenthium.Entities;
using Zenthium.Statuses;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// enemies.md, Targets: the nearest valid target; Taunt overrides it; it turns on whoever hit it unless that was
    /// an ally; enemies prefer the player and allies, then civilians, then neutrals; Heavy Turrets never target
    /// civilians. statuses.md: Hypnotized fights on the hypnotizer's side.
    /// </summary>
    public class TargetSelectorTests
    {
        [Test]
        public void Choose_SeveralPlayerSide_PicksTheNearest()
        {
            Entity player = CombatTestSetup.CreateEntity(Team.Player);
            Entity ally = CombatTestSetup.CreateEntity(Team.Ally);

            Entity chosen = TargetSelector.Choose(Team.Enemy, false, null, null,
                new[] { new TargetCandidate(player, 12f), new TargetCandidate(ally, 7f) });

            Assert.That(chosen, Is.SameAs(ally));
        }

        [Test]
        public void Choose_Taunted_PicksTheTaunterOverANearerTarget()
        {
            Entity taunter = CombatTestSetup.CreateEntity(Team.Player);
            Entity ally = CombatTestSetup.CreateEntity(Team.Ally);

            Entity chosen = TargetSelector.Choose(Team.Enemy, false, taunter, null,
                new[] { new TargetCandidate(ally, 2f) });

            Assert.That(chosen, Is.SameAs(taunter));
        }

        [Test]
        public void Choose_HitByAFartherTarget_TurnsOnTheAttacker()
        {
            Entity attacker = CombatTestSetup.CreateEntity(Team.Ally);
            Entity player = CombatTestSetup.CreateEntity(Team.Player);

            Entity chosen = TargetSelector.Choose(Team.Enemy, false, null, attacker,
                new[] { new TargetCandidate(player, 3f), new TargetCandidate(attacker, 20f) });

            Assert.That(chosen, Is.SameAs(attacker));
        }

        [Test]
        public void Choose_HitByAnotherEnemy_DoesntTurnOnIt()
        {
            Entity otherEnemy = CombatTestSetup.CreateEntity(Team.Enemy);
            Entity player = CombatTestSetup.CreateEntity(Team.Player);

            Entity chosen = TargetSelector.Choose(Team.Enemy, false, null, otherEnemy,
                new[] { new TargetCandidate(otherEnemy, 1f), new TargetCandidate(player, 20f) });

            Assert.That(chosen, Is.SameAs(player));
        }

        [Test]
        public void Choose_Enemy_PrefersThePlayerOverANearerCivilianAndCivilianOverNeutral()
        {
            Entity player = CombatTestSetup.CreateEntity(Team.Player);
            Entity civilian = CombatTestSetup.CreateEntity(Team.Civilian);
            Entity neutral = CombatTestSetup.CreateEntity(Team.Neutral);

            Entity withPlayer = TargetSelector.Choose(Team.Enemy, false, null, null, new[]
            {
                new TargetCandidate(neutral, 1f), new TargetCandidate(civilian, 2f), new TargetCandidate(player, 30f),
            });
            Entity withoutPlayer = TargetSelector.Choose(Team.Enemy, false, null, null, new[]
            {
                new TargetCandidate(neutral, 1f), new TargetCandidate(civilian, 30f),
            });
            Entity onlyNeutral = TargetSelector.Choose(Team.Enemy, false, null, null,
                new[] { new TargetCandidate(neutral, 40f) });

            Assert.That(withPlayer, Is.SameAs(player));
            Assert.That(withoutPlayer, Is.SameAs(civilian));
            Assert.That(onlyNeutral, Is.SameAs(neutral));
        }

        [Test]
        public void Choose_TurretSparingCivilians_NeverPicksACivilian()
        {
            Entity civilian = CombatTestSetup.CreateEntity(Team.Civilian);
            Entity enemy = CombatTestSetup.CreateEntity(Team.Enemy);

            Entity onlyCivilian = TargetSelector.Choose(Team.Neutral, true, null, null,
                new[] { new TargetCandidate(civilian, 1f) });
            Entity withEnemy = TargetSelector.Choose(Team.Neutral, true, null, null,
                new[] { new TargetCandidate(civilian, 1f), new TargetCandidate(enemy, 20f) });

            Assert.That(onlyCivilian, Is.Null);
            Assert.That(withEnemy, Is.SameAs(enemy));
        }

        [Test]
        public void Choose_Ally_NeverPicksCiviliansOrThePlayer()
        {
            Entity civilian = CombatTestSetup.CreateEntity(Team.Civilian);
            Entity player = CombatTestSetup.CreateEntity(Team.Player);

            Entity chosen = TargetSelector.Choose(Team.Ally, false, null, null,
                new[] { new TargetCandidate(civilian, 1f), new TargetCandidate(player, 2f) });

            Assert.That(chosen, Is.Null);
        }

        [Test]
        public void Notices_Enemy_OnlyThePlayerAndAllies()
        {
            Assert.That(AiSides.Notices(Team.Enemy, Team.Player, false), Is.True);
            Assert.That(AiSides.Notices(Team.Enemy, Team.Ally, false), Is.True);
            Assert.That(AiSides.Notices(Team.Enemy, Team.Civilian, false), Is.False);
            Assert.That(AiSides.Notices(Team.Enemy, Team.Neutral, false), Is.False);
        }

        [Test]
        public void EffectiveTeam_HypnotizedByThePlayer_FightsForThePlayersSide()
        {
            Entity player = CombatTestSetup.CreateEntity(Team.Player);
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy);

            goon.Statuses.TryApply(new StatusApplication(StatusType.Hypnotized, player, 5f));

            Assert.That(AiSides.EffectiveTeam(goon), Is.EqualTo(Team.Ally));
        }
    }
}
