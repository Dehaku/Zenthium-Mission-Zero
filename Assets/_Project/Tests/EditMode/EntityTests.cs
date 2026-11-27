using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Escaping off the map counts as defeated (enemies.md, Limits); citizens can start the mission Incapacitated
    /// (missions.md); Charming switches a goon's team (modules.md).
    /// </summary>
    public class EntityTests
    {
        [Test]
        public void Escape_StandingEntity_EscapesAndRaisesEvent()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            int raised = 0;
            goon.OnEscaped += () => raised++;

            bool result = goon.Escape();

            Assert.That(result, Is.True);
            Assert.That(goon.IsEscaped, Is.True);
            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void Escape_Twice_IsRefusedTheSecondTime()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            goon.Escape();

            Assert.That(goon.Escape(), Is.False);
        }

        [Test]
        public void Escape_Incapacitated_IsRefused()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            goon.Incapacitate();

            Assert.That(goon.Escape(), Is.False);
            Assert.That(goon.IsEscaped, Is.False);
        }

        [Test]
        public void Incapacitate_StandingEntity_IsDownAndRaisesEvent()
        {
            Entity citizen = CombatTestSetup.CreateEntity(Team.Civilian);
            int raised = 0;
            citizen.OnIncapacitated += () => raised++;

            bool result = citizen.Incapacitate();

            Assert.That(result, Is.True);
            Assert.That(citizen.IsIncapacitated, Is.True);
            Assert.That(citizen.Health.Current, Is.EqualTo(0f));
            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void Incapacitate_AlreadyDown_IsRefused()
        {
            Entity citizen = CombatTestSetup.CreateEntity(Team.Civilian);
            citizen.Incapacitate();

            Assert.That(citizen.Incapacitate(), Is.False);
        }

        [Test]
        public void ChangeTeam_ToAnotherTeam_SwitchesAndRaisesEventOnce()
        {
            Entity goon = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            int raised = 0;
            goon.OnTeamChanged += () => raised++;

            goon.ChangeTeam(Team.Ally);
            goon.ChangeTeam(Team.Ally);

            Assert.That(goon.Team, Is.EqualTo(Team.Ally));
            Assert.That(raised, Is.EqualTo(1));
        }
    }
}
