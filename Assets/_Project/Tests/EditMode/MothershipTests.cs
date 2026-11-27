using NUnit.Framework;
using Zenthium.Data;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Factions/GalacticRepoAgency.md, Mothership Strike: every 30 s an orbital beam targets the player; a countdown
    /// from 5; 300 damage in a 5 m radius.
    /// </summary>
    public class MothershipTests
    {
        [Test]
        public void TryCreate_ReadsTheFactionsValues()
        {
            FactionData repo = TestFactions.FactionWithValues("Repo", ("mothership_interval_s", 30f),
                ("mothership_countdown_s", 5f), ("mothership_damage", 300f), ("mothership_radius_m", 5f));

            bool created = MothershipRules.TryCreate(repo, out MothershipRules rules, out _);

            Assert.That(created, Is.True);
            Assert.That(rules.IntervalSeconds, Is.EqualTo(30f));
            Assert.That(rules.CountdownSeconds, Is.EqualTo(5f));
            Assert.That(rules.Damage, Is.EqualTo(300f));
            Assert.That(rules.Radius, Is.EqualTo(5f));
        }

        [Test]
        public void TryCreate_MissingDamage_Fails()
        {
            FactionData repo = TestFactions.FactionWithValues("Repo", ("mothership_interval_s", 30f),
                ("mothership_countdown_s", 5f), ("mothership_radius_m", 5f));

            Assert.That(MothershipRules.TryCreate(repo, out _, out string problem), Is.False);
            Assert.That(problem, Does.Contain("mothership_damage"));
        }

        [Test]
        public void Timer_FirstStrikeComesOneIntervalIn()
        {
            var timer = new MothershipTimer(30f);

            Assert.That(timer.Tick(29.9f), Is.False);
            Assert.That(timer.Tick(0.2f), Is.True);
        }

        [Test]
        public void Timer_LeftoverTimeCountsTowardTheNextStrike()
        {
            var timer = new MothershipTimer(30f);
            timer.Tick(31f);

            Assert.That(timer.SecondsLeft, Is.EqualTo(29f).Within(0.001f));
            Assert.That(timer.Tick(28.9f), Is.False);
            Assert.That(timer.Tick(0.2f), Is.True);
        }

        [Test]
        public void Timer_Restart_WaitsAFullInterval()
        {
            var timer = new MothershipTimer(30f);
            timer.Tick(25f);

            timer.Restart();

            Assert.That(timer.SecondsLeft, Is.EqualTo(30f));
        }
    }
}
