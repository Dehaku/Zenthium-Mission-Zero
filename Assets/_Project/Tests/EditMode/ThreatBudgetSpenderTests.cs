using System.Collections.Generic;
using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Missions;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Spending the threat budget (enemies.md, Threat budget and Squads): Champions only up to their share, goons with
    /// the rest, squads of 3-5 goons (the fixture's sizes), and Champions leading a squad or standing alone.
    /// </summary>
    public class ThreatBudgetSpenderTests
    {
        private static SpawnConstants Constants => DesignDataTestCsv.Parse().Data.Spawn;

        private static FactionRoster Roster(int goonCost, params int[] championCosts)
        {
            FactionData faction = TestFactions.Faction("Repo");
            var entities = new List<EntityData>
            {
                TestFactions.Entity("Grunt", faction, RankTier.Goon, budgetCost: goonCost),
            };
            for (int i = 0; i < championCosts.Length; i++)
            {
                entities.Add(TestFactions.Entity($"Champion {i}", faction, RankTier.Champion,
                    budgetCost: championCosts[i]));
            }

            return new FactionRoster(faction, entities, 0);
        }

        private static (int Goons, int Champions) Count(List<SpawnSquad> squads)
        {
            int goons = 0;
            int champions = 0;
            foreach (SpawnSquad squad in squads)
            {
                goons += squad.Goons.Count;
                champions += squad.Champion != null ? 1 : 0;
            }

            return (goons, champions);
        }

        [Test]
        public void Spend_ShareTooSmallForAnyChampion_BuysOnlyGoons()
        {
            // Small map at rank D: 10% of 30 is 3, less than the cheapest Champion (4).
            var squads = new List<SpawnSquad>();

            ThreatBudgetSpender.Spend(30f, 0.1f, Roster(1, 4, 5), Constants, new FixedRandom(0.5f), squads);

            Assert.That(Count(squads), Is.EqualTo((30, 0)));
        }

        [Test]
        public void Spend_ChampionsStayWithinTheirShare_GoonsTakeTheRest()
        {
            // 30% of 60 is 18: three 5-point Champions (15), then 45 one-point goons.
            var squads = new List<SpawnSquad>();

            ThreatBudgetSpender.Spend(60f, 0.3f, Roster(1, 5), Constants, new FixedRandom(0.5f), squads);

            Assert.That(Count(squads), Is.EqualTo((45, 3)));
        }

        [Test]
        public void Spend_LeftoverTooSmallForAGoon_IsLeftUnspent()
        {
            var squads = new List<SpawnSquad>();

            ThreatBudgetSpender.Spend(5f, 0f, Roster(2), Constants, new FixedRandom(0.5f), squads);

            Assert.That(Count(squads), Is.EqualTo((2, 0)));
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(0.99f)]
        public void Spend_EverySquadHasThreeToFiveGoons(float roll)
        {
            var squads = new List<SpawnSquad>();

            ThreatBudgetSpender.Spend(30f, 0f, Roster(1), Constants, new FixedRandom(roll), squads);

            foreach (SpawnSquad squad in squads)
            {
                Assert.That(squad.Goons.Count, Is.InRange(3, 5));
            }
        }

        [Test]
        public void Spend_SevenGoonsRollingFive_SplitSoNoneIsUnderThree()
        {
            var squads = new List<SpawnSquad>();

            ThreatBudgetSpender.Spend(7f, 0f, Roster(1), Constants, new FixedRandom(0.99f), squads);

            Assert.That(squads, Has.Count.EqualTo(2));
            Assert.That(squads[0].Goons.Count, Is.EqualTo(4));
            Assert.That(squads[1].Goons.Count, Is.EqualTo(3));
        }

        [Test]
        public void Spend_TooFewGoonsForAFullSquad_MakesOneSmallSquad()
        {
            var squads = new List<SpawnSquad>();

            ThreatBudgetSpender.Spend(2f, 0f, Roster(1), Constants, new FixedRandom(0.5f), squads);

            Assert.That(squads, Has.Count.EqualTo(1));
            Assert.That(squads[0].Goons.Count, Is.EqualTo(2));
        }

        [TestCase(0.2f, true)]
        [TestCase(0.5f, false)]
        public void Spend_ChampionRollUnderSoloChance_StandsAlone(float roll, bool expectedSolo)
        {
            // Fixture solo chance: 50%. 10% of 60 buys one 5-point Champion.
            var squads = new List<SpawnSquad>();

            ThreatBudgetSpender.Spend(60f, 0.1f, Roster(1, 5), Constants, new FixedRandom(roll), squads);

            SpawnSquad led = squads.Find(squad => squad.Champion != null);
            Assert.That(led, Is.Not.Null);
            Assert.That(led.Goons.Count == 0, Is.EqualTo(expectedSolo));
        }

        [Test]
        public void SquadCost_AddsItsMembers()
        {
            var squads = new List<SpawnSquad>();

            ThreatBudgetSpender.Spend(9f, 0.6f, Roster(1, 5), Constants, new FixedRandom(0.5f), squads);

            // One 5-point Champion leading the four goons the other 4 points buy.
            Assert.That(squads, Has.Count.EqualTo(1));
            Assert.That(squads[0].Cost, Is.EqualTo(9));
        }
    }
}
