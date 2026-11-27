using Zenthium.Data;

namespace Zenthium.World
{
    /// <summary>Which district each location is in (maps.md, Locations). A mission's location decides its district.</summary>
    public static class LocationDistricts
    {
        public static District Of(LocationType location)
        {
            switch (location)
            {
                case LocationType.Lab:
                case LocationType.Armory:
                case LocationType.Warehouse:
                    return District.Industrial;
                case LocationType.House:
                case LocationType.ApartmentBuilding:
                case LocationType.SmallShop:
                case LocationType.AntiqueShop:
                case LocationType.Mansion:
                case LocationType.Park:
                    return District.Residential;
                default:
                    return District.Downtown;
            }
        }

        /// <summary>Only Story Arcs use it, as their key location; it never fills a map at random (maps.md).</summary>
        public static bool IsUnique(LocationType location)
        {
            return location == LocationType.SwiftTower;
        }
    }
}
