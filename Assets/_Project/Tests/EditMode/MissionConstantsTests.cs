using System.Collections.Generic;
using NUnit.Framework;
using Zenthium.Data;
using Zenthium.Editor.DataImport;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// The rules each rank sets (difficulty.md; missions.md, Types), read from the fixture's mission rows: D is 0, C 1,
    /// B 2, A 3, S 4 and S2 5.
    /// </summary>
    public class MissionConstantsTests
    {
        private static ParsedDesignData Data => DesignDataTestCsv.Parse().Data;

        [TestCase(0, MapSize.Small)]
        [TestCase(1, MapSize.Medium)]
        [TestCase(2, MapSize.Medium)]
        [TestCase(3, MapSize.Large)]
        [TestCase(5, MapSize.Large)]
        public void MapSizeFor_GrowsWithRank(int difficultyLevel, MapSize expected)
        {
            Assert.That(Data.Mission.MapSizeFor(difficultyLevel), Is.EqualTo(expected));
        }

        [TestCase(0, 4)]
        [TestCase(1, 3)]
        [TestCase(3, 2)]
        [TestCase(4, 1)]
        [TestCase(7, 1)]
        public void ExitZones_FewerEachRank(int difficultyLevel, int expected)
        {
            Assert.That(Data.Mission.ExitZones(difficultyLevel), Is.EqualTo(expected));
        }

        [Test]
        public void LockedDoorMaterials_EachRankKeepsTheOnesBelow()
        {
            var atD = new List<StructureMaterial>();
            var atC = new List<StructureMaterial>();

            Data.Mission.LockedDoorMaterials(0, atD);
            Data.Mission.LockedDoorMaterials(1, atC);

            Assert.That(atD, Is.EquivalentTo(new[] { StructureMaterial.Glass, StructureMaterial.Wood }));
            Assert.That(atC,
                Is.EquivalentTo(new[] { StructureMaterial.Glass, StructureMaterial.Wood, StructureMaterial.Stone }));
        }

        [TestCase(0, 180f)]
        [TestCase(5, 130f)]
        [TestCase(10, 90f)]
        public void CountdownSeconds_ShorterEachRank_NeverUnderTheMinimum(int difficultyLevel, float expected)
        {
            Assert.That(Data.Mission.CountdownSeconds(difficultyLevel), Is.EqualTo(expected).Within(0.001f));
        }

        [TestCase(0, 0.25f)]
        [TestCase(2, 0.35f)]
        public void RescueFailFraction_RisesEachRank(int difficultyLevel, float expected)
        {
            Assert.That(Data.Mission.RescueFailFraction(difficultyLevel), Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(0, 0.5f)]
        [TestCase(2, 0.4f)]
        [TestCase(8, 0.2f)]
        public void ArsonFailFraction_LowerEachRank_NeverUnderTheFloor(int difficultyLevel, float expected)
        {
            Assert.That(Data.Mission.ArsonFailFraction(difficultyLevel), Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(MapSize.Small, 6)]
        [TestCase(MapSize.Large, 15)]
        public void RescueTargets_BySize(MapSize size, int expected)
        {
            Assert.That(Data.Mission.RescueTargets(size), Is.EqualTo(expected));
        }

        [TestCase(0, 0f)]
        [TestCase(1, 0.2f)]
        [TestCase(6, 1f)]
        public void LeaderChance_ByRank_SAndUpUseS(int difficultyLevel, float expected)
        {
            Assert.That(Data.Mission.LeaderChance(difficultyLevel), Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(1, 0)]
        [TestCase(2, 3)]
        [TestCase(4, 5)]
        public void ReinforcementWaves_FromRankB_OneMoreEachRank(int difficultyLevel, int expected)
        {
            Assert.That(Data.Spawn.ReinforcementWaves(difficultyLevel), Is.EqualTo(expected));
        }

        [TestCase(MapSize.Small, 30f)]
        [TestCase(MapSize.Medium, 60f)]
        [TestCase(MapSize.Large, 100f)]
        public void Budget_BySize(MapSize size, float expected)
        {
            Assert.That(Data.Spawn.Budget(size), Is.EqualTo(expected));
        }

        [TestCase(0, 0.1f)]
        [TestCase(3, 0.5f)]
        [TestCase(5, 0.5f)]
        public void ChampionShare_ByRank(int difficultyLevel, float expected)
        {
            Assert.That(Data.Spawn.ChampionShare(difficultyLevel), Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
