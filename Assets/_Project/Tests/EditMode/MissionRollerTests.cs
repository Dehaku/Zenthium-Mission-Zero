using System.Collections.Generic;
using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Missions;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Picking a mission (factions.md, Weights): the type at random, then a faction by Mission Weight, then a location
    /// by Location Weight, with unlisted weights at 1; and the Leader roll by rank (difficulty.md).
    /// </summary>
    public class MissionRollerTests
    {
        private static MissionConstants Constants => DesignDataTestCsv.Parse().Data.Mission;

        [TestCase(0f, MissionType.Rescue)]
        [TestCase(0.3f, MissionType.Countdown)]
        [TestCase(0.6f, MissionType.Invasion)]
        [TestCase(0.99f, MissionType.ArsonRiot)]
        [TestCase(1f, MissionType.ArsonRiot)]
        public void RollType_EachTypeHasAnEqualShare(float roll, MissionType expected)
        {
            Assert.That(MissionRoller.RollType(new FixedRandom(roll)), Is.EqualTo(expected));
        }

        [TestCase(0.7f, "Repo")]
        [TestCase(0.8f, "Other")]
        public void PickFaction_WeighsByMissionWeight(float roll, string expected)
        {
            // Rescue: Repo 3, Other unlisted (1), so Repo takes the first three quarters of the roll.
            var factions = new List<FactionData>
            {
                TestFactions.FactionWithWeights("Repo", new[] { ("rescue", 3f) }, new (string, float)[0]),
                TestFactions.FactionWithWeights("Other", new (string, float)[0], new (string, float)[0]),
            };

            FactionData picked = MissionRoller.PickFaction(new FixedRandom(roll), factions, MissionType.Rescue);

            Assert.That(picked.Id, Is.EqualTo(expected));
        }

        [Test]
        public void PickFaction_UsesTheTypesOwnWeight()
        {
            // Repo's Rescue weight doesn't count for Arson Riot, where both are unlisted: half each.
            var factions = new List<FactionData>
            {
                TestFactions.FactionWithWeights("Repo", new[] { ("rescue", 3f) }, new (string, float)[0]),
                TestFactions.FactionWithWeights("Other", new (string, float)[0], new (string, float)[0]),
            };

            FactionData picked = MissionRoller.PickFaction(new FixedRandom(0.6f), factions, MissionType.ArsonRiot);

            Assert.That(picked.Id, Is.EqualTo("Other"));
        }

        [Test]
        public void PickFaction_ArsonRiotWeight_IsReadFromItsDataKey()
        {
            var factions = new List<FactionData>
            {
                TestFactions.FactionWithWeights("Repo", new[] { ("arson_riot", 0f) }, new (string, float)[0]),
                TestFactions.FactionWithWeights("Other", new (string, float)[0], new (string, float)[0]),
            };

            FactionData picked = MissionRoller.PickFaction(new FixedRandom(0f), factions, MissionType.ArsonRiot);

            Assert.That(picked.Id, Is.EqualTo("Other"));
        }

        [TestCase(0.6f, LocationType.Bank)]
        [TestCase(0.7f, LocationType.Museum)]
        public void TryPickLocation_WeighsByLocationWeight(float roll, LocationType expected)
        {
            // Bank 2, Museum unlisted (1): the bank takes the first two thirds of the roll.
            FactionData faction = TestFactions.FactionWithWeights("Repo", new (string, float)[0],
                new[] { ("bank", 2f) });
            var locations = new List<LocationType> { LocationType.Bank, LocationType.Museum };

            bool picked = MissionRoller.TryPickLocation(new FixedRandom(roll), faction, locations,
                out LocationType location);

            Assert.That(picked, Is.True);
            Assert.That(location, Is.EqualTo(expected));
        }

        [TestCase(0, 0f, false)]
        [TestCase(1, 0.1f, true)]
        [TestCase(1, 0.3f, false)]
        [TestCase(4, 1f, true)]
        public void RollsLeader_ChanceRisesWithRank(int difficultyLevel, float roll, bool expected)
        {
            // Fixture: D 0%, C 20%, and S guaranteed.
            FactionData faction = TestFactions.Faction("Repo");
            var entities = new List<EntityData> { TestFactions.Entity("Boss", faction, RankTier.Leader) };

            bool rolled = MissionRoller.RollsLeader(new FixedRandom(roll), faction, entities, difficultyLevel,
                Constants);

            Assert.That(rolled, Is.EqualTo(expected));
        }

        [Test]
        public void RollsLeader_FactionWithoutALeader_NeverHasOne()
        {
            FactionData faction = TestFactions.Faction("Repo");
            var entities = new List<EntityData> { TestFactions.Entity("Grunt", faction, RankTier.Goon) };

            Assert.That(MissionRoller.RollsLeader(new FixedRandom(0f), faction, entities, 4, Constants), Is.False);
        }

        [TestCase(0, MapSize.Small)]
        [TestCase(2, MapSize.Medium)]
        [TestCase(5, MapSize.Large)]
        public void TryRoll_MapSizeFollowsRank(int difficultyLevel, MapSize expected)
        {
            var factions = new List<FactionData> { TestFactions.Faction("Repo") };
            var locations = new List<LocationType> { LocationType.Bank };

            bool rolled = MissionRoller.TryRoll(new FixedRandom(0.5f), factions, locations, new List<EntityData>(),
                difficultyLevel, Constants, 42, out MissionSetup setup);

            Assert.That(rolled, Is.True);
            Assert.That(setup.Size, Is.EqualTo(expected));
            Assert.That(setup.Seed, Is.EqualTo(42));
        }

        [Test]
        public void TryRoll_NoLocations_Fails()
        {
            var factions = new List<FactionData> { TestFactions.Faction("Repo") };

            bool rolled = MissionRoller.TryRoll(new FixedRandom(0.5f), factions, new List<LocationType>(),
                new List<EntityData>(), 0, Constants, 1, out MissionSetup setup);

            Assert.That(rolled, Is.False);
            Assert.That(setup, Is.Null);
        }

        [TestCase(MissionType.ArsonRiot, "arson_riot")]
        [TestCase(MissionType.Rescue, "rescue")]
        public void Key_MissionType_MatchesFactionsCsv(MissionType type, string expected)
        {
            Assert.That(MissionWeights.Key(type), Is.EqualTo(expected));
        }

        [TestCase(LocationType.RecordsOffice, "records_office")]
        [TestCase(LocationType.Bank, "bank")]
        public void Key_Location_MatchesFactionsCsv(LocationType location, string expected)
        {
            Assert.That(MissionWeights.Key(location), Is.EqualTo(expected));
        }
    }
}
