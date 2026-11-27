using System.Collections.Generic;
using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Missions;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// What a faction can field at a rank (factions.md, Minimum ranks; enemies.md, Threat budget): its own entities
    /// that spawn at the rank, the budget buying goons and Champions, and the Leader outside it.
    /// </summary>
    public class FactionRosterTests
    {
        [Test]
        public void Roster_KeepsOnlyEntitiesThatSpawnAtTheRank()
        {
            FactionData repo = TestFactions.Faction("Repo");
            var entities = new List<EntityData>
            {
                TestFactions.Entity("Shield Bearer", repo, RankTier.Champion, Rank.D, budgetCost: 5),
                TestFactions.Entity("Auditor", repo, RankTier.Champion, Rank.C, budgetCost: 5),
            };

            var atD = new FactionRoster(repo, entities, 0);
            var atC = new FactionRoster(repo, entities, 1);

            Assert.That(atD.Champions, Has.Count.EqualTo(1));
            Assert.That(atC.Champions, Has.Count.EqualTo(2));
        }

        [Test]
        public void Roster_LeavesOutOtherFactionsAndEntitiesWithoutACost()
        {
            FactionData repo = TestFactions.Faction("Repo");
            FactionData circus = TestFactions.Faction("Circus");
            var entities = new List<EntityData>
            {
                TestFactions.Entity("Grey", repo, RankTier.Goon, budgetCost: 1),
                TestFactions.Entity("Clown", circus, RankTier.Goon, budgetCost: 1),
                TestFactions.Entity("Summon", repo, RankTier.Goon, null),
            };

            var roster = new FactionRoster(repo, entities, 0);

            Assert.That(roster.Goons, Has.Count.EqualTo(1));
            Assert.That(roster.Goons[0].Id, Is.EqualTo("Grey"));
        }

        [Test]
        public void Roster_FindsTheLeaderOutsideTheBudget()
        {
            FactionData repo = TestFactions.Faction("Repo");
            var entities = new List<EntityData>
            {
                TestFactions.Entity("GleebPeep", repo, RankTier.Leader),
                TestFactions.Entity("Grey", repo, RankTier.Goon, budgetCost: 1),
            };

            var roster = new FactionRoster(repo, entities, 0);

            Assert.That(roster.Leader.Id, Is.EqualTo("GleebPeep"));
            Assert.That(roster.Goons, Has.Count.EqualTo(1));
            Assert.That(roster.Champions, Is.Empty);
        }
    }
}
