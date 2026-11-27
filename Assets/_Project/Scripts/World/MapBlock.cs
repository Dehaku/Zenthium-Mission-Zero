using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;

namespace Zenthium.World
{
    /// <summary>
    /// The root of a handmade city block prefab (maps.md, Generation; <c>Docs/HowTo_AddBlock.md</c>): a square of the
    /// block size from constants.csv, centered on this object, with its buildings, props and sockets as children.
    /// The map builder lays blocks on the street grid and turns them in steps of 90°.
    /// </summary>
    public sealed class MapBlock : MonoBehaviour
    {
        [SerializeField] private District district = District.Downtown;

        [Tooltip("Locations on the block that have no Building, such as a Park. Each Building's location counts already.")]
        [SerializeField] private List<LocationType> extraLocations = new List<LocationType>();

        public District District => district;

        /// <summary>Its buildings' locations and its extra ones, each once. Works on the prefab asset too.</summary>
        public void CollectLocations(List<LocationType> results)
        {
            results.Clear();
            foreach (Building building in GetComponentsInChildren<Building>(true))
            {
                AddOnce(results, building.Location);
            }

            foreach (LocationType location in extraLocations)
            {
                AddOnce(results, location);
            }
        }

        public MapBlockOption ToOption()
        {
            var locations = new List<LocationType>();
            CollectLocations(locations);
            return new MapBlockOption(district, locations);
        }

        private static void AddOnce(List<LocationType> results, LocationType location)
        {
            if (!results.Contains(location))
            {
                results.Add(location);
            }
        }
    }
}
