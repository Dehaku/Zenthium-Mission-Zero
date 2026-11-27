using System.Collections.Generic;
using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Modules;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// modules.md, Call for Backup: a few goons and Champions of the summoner's faction. Only entities that spawn at
    /// the mission's rank come (the user's decision, Milestone 7).
    /// </summary>
    public class BackupRosterTests
    {
        [TestCase(0f, 3)]
        [TestCase(0.5f, 4)]
        [TestCase(0.99f, 5)]
        public void RollCount_Range_RollsEachWholeNumberInIt(float roll, int expected)
        {
            DataValue goons = DataValue.FromRange("goons", 3f, 5f);

            Assert.That(BackupRoster.RollCount(goons, new FixedRandom(roll)), Is.EqualTo(expected));
        }

        [Test]
        public void RollCount_Number_IsThatNumber()
        {
            DataValue champions = DataValue.FromNumber("champions", 2f);

            Assert.That(BackupRoster.RollCount(champions, new FixedRandom(0.99f)), Is.EqualTo(2));
        }

        [Test]
        public void Pick_OnlyTheFactionsEntitiesOfTheTierThatSpawnAtTheRank()
        {
            FactionData repo = TestFactions.Faction("Repo");
            FactionData circus = TestFactions.Faction("Circus");
            EntityData grey = TestFactions.Entity("Grey", repo, RankTier.Goon, Rank.D);
            EntityData saucer = TestFactions.Entity("Saucer", repo, RankTier.Goon, Rank.B);
            EntityData drone = TestFactions.Entity("Drone", repo, RankTier.Champion, Rank.D);
            EntityData clown = TestFactions.Entity("Clown", circus, RankTier.Goon, Rank.D);
            var entities = new[] { clown, grey, saucer, drone };
            var results = new List<EntityData>();

            BackupRoster.Pick(entities, repo, RankTier.Goon, 1, 3, new FixedRandom(0.99f), new List<EntityData>(),
                results);

            Assert.That(results, Is.EqualTo(new[] { grey, grey, grey }));
        }

        [TestCase(0f, "Grey")]
        [TestCase(0.99f, "Saucer")]
        public void Pick_AtRankB_CanBringTheSaucer(float roll, string expected)
        {
            FactionData repo = TestFactions.Faction("Repo");
            EntityData grey = TestFactions.Entity("Grey", repo, RankTier.Goon, Rank.D);
            EntityData saucer = TestFactions.Entity("Saucer", repo, RankTier.Goon, Rank.B);
            var results = new List<EntityData>();

            BackupRoster.Pick(new[] { grey, saucer }, repo, RankTier.Goon, 2, 1, new FixedRandom(roll),
                new List<EntityData>(), results);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Id, Is.EqualTo(expected));
        }
    }
}
