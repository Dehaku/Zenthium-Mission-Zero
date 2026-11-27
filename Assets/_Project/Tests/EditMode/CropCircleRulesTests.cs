using NUnit.Framework;
using Zenthium.Data;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Factions/GalacticRepoAgency.md: 1 crop circle at rank D, plus 1 per rank, up to 5 at S; no more after that.
    /// </summary>
    public class CropCircleRulesTests
    {
        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(3, 4)]
        [TestCase(4, 5)]
        [TestCase(5, 5)]
        [TestCase(9, 5)]
        public void Count_GrowsWithRankUpToTheMax(int difficultyLevel, int expected)
        {
            var rules = new CropCircleRules(1f, 1f, 5f, 3f);

            Assert.That(rules.Count(difficultyLevel), Is.EqualTo(expected));
        }

        [Test]
        public void TryCreate_ReadsTheFactionsValues()
        {
            FactionData repo = TestFactions.FactionWithValues("Repo", ("crop_circles_base", 1f),
                ("crop_circles_per_rank", 1f), ("crop_circles_max", 5f), ("crop_circle_radius_m", 3f));

            bool created = CropCircleRules.TryCreate(repo, out CropCircleRules rules, out _);

            Assert.That(created, Is.True);
            Assert.That(rules.Radius, Is.EqualTo(3f));
            Assert.That(rules.Count(2), Is.EqualTo(3));
        }

        [Test]
        public void TryCreate_MissingRadius_Fails()
        {
            FactionData repo = TestFactions.FactionWithValues("Repo", ("crop_circles_base", 1f),
                ("crop_circles_per_rank", 1f), ("crop_circles_max", 5f));

            Assert.That(CropCircleRules.TryCreate(repo, out _, out string problem), Is.False);
            Assert.That(problem, Does.Contain("crop_circle_radius_m"));
        }
    }
}
