using NUnit.Framework;
using Zenthium.Data;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>A mission's location decides its district (maps.md, Locations).</summary>
    public class LocationDistrictsTests
    {
        [TestCase(LocationType.Bank, District.Downtown)]
        [TestCase(LocationType.RecordsOffice, District.Downtown)]
        [TestCase(LocationType.SwiftTower, District.Downtown)]
        [TestCase(LocationType.Lab, District.Industrial)]
        [TestCase(LocationType.Warehouse, District.Industrial)]
        [TestCase(LocationType.House, District.Residential)]
        [TestCase(LocationType.Park, District.Residential)]
        public void Of_GivesTheLocationsDistrict(LocationType location, District expected)
        {
            Assert.That(LocationDistricts.Of(location), Is.EqualTo(expected));
        }

        [TestCase(LocationType.SwiftTower, true)]
        [TestCase(LocationType.Bank, false)]
        [TestCase(LocationType.Mansion, false)]
        public void IsUnique_OnlySwiftTower(LocationType location, bool expected)
        {
            Assert.That(LocationDistricts.IsUnique(location), Is.EqualTo(expected));
        }
    }
}
