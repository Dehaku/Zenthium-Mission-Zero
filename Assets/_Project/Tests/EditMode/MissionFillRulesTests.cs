using NUnit.Framework;
using Zenthium.AI;
using Zenthium.Data;
using Zenthium.Missions;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// What a mission puts on the map (world.md, Placement; missions.md, Citizens), with the fixture's numbers: a bank
    /// 20-40 Valuables, a store 5-10, any other building a 75% chance of 1, a civilian 10%, and citizens 60% Scared,
    /// 20% Incapacitated and 20% Stubborn.
    /// </summary>
    public class MissionFillRulesTests
    {
        private static MissionFillConstants Constants => DesignDataTestCsv.Parse().Data.MissionFill;

        [TestCase(LocationType.Bank, 0f, 20)]
        [TestCase(LocationType.Bank, 0.999f, 40)]
        [TestCase(LocationType.Store, 0f, 5)]
        [TestCase(LocationType.Store, 1f, 10)]
        [TestCase(LocationType.House, 0.5f, 1)]
        [TestCase(LocationType.House, 0.8f, 0)]
        public void BuildingValuables_ByLocation(LocationType location, float roll, int expected)
        {
            Assert.That(MissionFillRules.BuildingValuables(location, Constants, new FixedRandom(roll)),
                Is.EqualTo(expected));
        }

        [TestCase(0.05f, 1)]
        [TestCase(0.2f, 0)]
        public void CivilianValuables_TenPercentChance(float roll, int expected)
        {
            Assert.That(MissionFillRules.CivilianValuables(Constants, new FixedRandom(roll)), Is.EqualTo(expected));
        }

        [TestCase(0f, CitizenKind.Scared)]
        [TestCase(0.59f, CitizenKind.Scared)]
        [TestCase(0.7f, CitizenKind.Incapacitated)]
        [TestCase(0.9f, CitizenKind.Stubborn)]
        [TestCase(1f, CitizenKind.Stubborn)]
        public void CitizenKindFor_ByShare(float roll, CitizenKind expected)
        {
            Assert.That(MissionFillRules.CitizenKindFor(Constants, roll), Is.EqualTo(expected));
        }
    }
}
