using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Notarized and Audit reach entities on the user's side, not the user itself (the user's decisions,
    /// Milestone 7b), and only ones still standing on the map.
    /// </summary>
    public class SideRulesTests
    {
        [Test]
        public void IsOnUsersSide_SameTeam_IsReached()
        {
            Entity notary = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Champion);
            Entity grey = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);

            Assert.That(SideRules.IsOnUsersSide(notary, grey), Is.True);
        }

        [Test]
        public void IsOnUsersSide_TheUserItself_IsNot()
        {
            Entity notary = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Champion);

            Assert.That(SideRules.IsOnUsersSide(notary, notary), Is.False);
        }

        [TestCase(Team.Player)]
        [TestCase(Team.Ally)]
        [TestCase(Team.Neutral)]
        [TestCase(Team.Civilian)]
        public void IsOnUsersSide_OtherTeams_AreNot(Team team)
        {
            Entity notary = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Champion);
            Entity other = CombatTestSetup.CreateEntity(team);

            Assert.That(SideRules.IsOnUsersSide(notary, other), Is.False);
        }

        [Test]
        public void IsOnUsersSide_Incapacitated_IsNot()
        {
            Entity notary = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Champion);
            Entity grey = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            grey.Incapacitate();

            Assert.That(SideRules.IsOnUsersSide(notary, grey), Is.False);
        }

        [Test]
        public void IsOnUsersSide_Escaped_IsNot()
        {
            Entity notary = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Champion);
            Entity grey = CombatTestSetup.CreateEntity(Team.Enemy, RankTier.Goon);
            grey.Escape();

            Assert.That(SideRules.IsOnUsersSide(notary, grey), Is.False);
        }
    }
}
