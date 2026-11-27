using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Editor.DataImport;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// entities.csv's modules, locked_until and tags columns, and factions.csv (factions.md: locked modules, entity
    /// and faction tags, arrival tags, Mission and Location Weights).
    /// </summary>
    public class FactionDataParserTests
    {
        private const string BossRow =
            "Galactic Repo Agency,Boss,Leader,D,,,Kick; Ink Lash,Ink Lash:C,Coward; LootDestination:CropCircle,\n";

        private const string RepoFactionRow =
            "Galactic Repo Agency,LootDestination:CropCircle,Beam,rescue=3; arson_riot=1,bank=2,crop_circles_max=5,\n";

        private static void AssertFailsWithOneErrorMentioning(DesignDataParseResult result, string text)
        {
            Assert.That(result.Data, Is.Null, "A failed parse must return no data, so nothing partial is written.");
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain(text));
        }

        [Test]
        public void Parse_EntityColumns_ReadModulesLocksAndTags()
        {
            DesignDataParseResult result = DesignDataTestCsv.Parse(extraEntityRows: BossRow);

            Assert.That(result.Errors, Is.Empty);
            EntityRecord boss = result.Data.Entities.Find(e => e.Id == "Boss");
            Assert.That(boss.ModuleIds, Is.EqualTo(new[] { "Kick", "Ink Lash" }));
            Assert.That(boss.Locks, Has.Count.EqualTo(1));
            Assert.That(boss.Locks[0].ModuleId, Is.EqualTo("Ink Lash"));
            Assert.That(boss.Locks[0].Rank, Is.EqualTo(Rank.C));
            Assert.That(boss.Tags, Is.EqualTo(new[] { EntityTag.Coward, EntityTag.LootDestinationCropCircle }));
        }

        [Test]
        public void Parse_LockOnAModuleNotInTheList_Fails()
        {
            string row = "Galactic Repo Agency,Boss,Leader,D,,,Kick,Ink Lash:C,,\n";

            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(extraEntityRows: row), "Ink Lash");
        }

        [Test]
        public void Parse_UnknownModuleInTheList_Fails()
        {
            string row = "Galactic Repo Agency,Boss,Leader,D,,,Kick; Moon Punch,,,\n";

            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(extraEntityRows: row), "Moon Punch");
        }

        [Test]
        public void Parse_UnknownEntityTag_Fails()
        {
            string row = "Galactic Repo Agency,Boss,Leader,D,,,,,Sparkly,\n";

            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(extraEntityRows: row), "Sparkly");
        }

        [Test]
        public void Parse_FactionRow_ReadsTagsArrivalWeightsAndValues()
        {
            DesignDataParseResult result = DesignDataTestCsv.Parse(factionRows: RepoFactionRow);

            Assert.That(result.Errors, Is.Empty);
            FactionRecord repo = result.Data.Factions.Find(f => f.Id == "Galactic Repo Agency");
            Assert.That(repo.Tags, Is.EqualTo(new[] { EntityTag.LootDestinationCropCircle }));
            Assert.That(repo.Arrival, Is.EqualTo(ArrivalTag.Beam));
            Assert.That(repo.MissionWeights.Find(w => w.Key == "rescue").Number, Is.EqualTo(3f));
            Assert.That(repo.MissionWeights.Find(w => w.Key == "arson_riot").Number, Is.EqualTo(1f));
            Assert.That(repo.LocationWeights.Find(w => w.Key == "bank").Number, Is.EqualTo(2f));
            Assert.That(repo.Values.Find(v => v.Key == "crop_circles_max").Number, Is.EqualTo(5f));
        }

        [Test]
        public void Parse_FactionWithNoEntities_Fails()
        {
            string row = "Space Pirates,,Beam,,,,\n";

            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(factionRows: row), "Space Pirates");
        }

        [Test]
        public void Parse_NegativeWeight_Fails()
        {
            string row = "Galactic Repo Agency,,Beam,rescue=-1,,,\n";

            AssertFailsWithOneErrorMentioning(DesignDataTestCsv.Parse(factionRows: row), "rescue");
        }
    }
}
