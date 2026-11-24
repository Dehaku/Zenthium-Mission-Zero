using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;

namespace Zenthium.World
{
    /// <summary>
    /// The root of a handmade building, which owns everything in it (world.md, Buildings): its interior zones,
    /// walls, windows and doors (each belongs to exactly one building; neighbors never share walls) and its
    /// sockets. It registers with the scene's <see cref="BuildingRegistry"/>, which gives it an ID and its neighbors.
    /// Its fire state comes with fire (Milestone 5b).
    /// </summary>
    public sealed class Building : MonoBehaviour
    {
        [SerializeField] private LocationType location = LocationType.House;
        [SerializeField] private BuildingRegistry registry;

        private readonly List<Building> neighbors = new List<Building>();
        private BuildingPart[] parts = new BuildingPart[0];
        private InteriorZone[] zones = new InteriorZone[0];
        private MapSocket[] sockets = new MapSocket[0];

        /// <summary>-1 until registered.</summary>
        public int Id { get; private set; } = -1;

        public LocationType Location => location;

        /// <summary>Its walls, windows and door panels.</summary>
        public IReadOnlyList<BuildingPart> Parts => parts;

        public IReadOnlyList<InteriorZone> Zones => zones;
        public IReadOnlyList<MapSocket> Sockets => sockets;

        /// <summary>Buildings within the neighbor radius, set when the registry works them out.</summary>
        public IReadOnlyList<Building> Neighbors => neighbors;

        /// <summary>For buildings placed in code, such as a map assembled at runtime: set before it's enabled.</summary>
        public void SetRegistry(BuildingRegistry newRegistry)
        {
            registry = newRegistry;
        }

        /// <summary>Adds its interior zones' floor areas to the list.</summary>
        public void AddFootprint(List<Rect> results)
        {
            foreach (InteriorZone zone in zones)
            {
                results.Add(zone.Footprint);
            }
        }

        internal void SetId(int id)
        {
            Id = id;
        }

        internal void SetNeighbors(List<Building> newNeighbors)
        {
            neighbors.Clear();
            neighbors.AddRange(newNeighbors);
        }

        private void Awake()
        {
            parts = GetComponentsInChildren<BuildingPart>(true);
            zones = GetComponentsInChildren<InteriorZone>(true);
            sockets = GetComponentsInChildren<MapSocket>(true);
            foreach (BuildingPart part in parts)
            {
                part.Building = this;
            }

            if (zones.Length == 0)
            {
                Debug.LogError($"{name}: a {nameof(Building)} needs at least one {nameof(InteriorZone)}.", this);
            }
        }

        private void OnEnable()
        {
            if (registry != null)
            {
                registry.Register(this);
            }
            else
            {
                Debug.LogError($"{name}: {nameof(Building)} needs the scene's {nameof(BuildingRegistry)}.", this);
            }
        }

        private void OnDisable()
        {
            if (registry != null)
            {
                registry.Unregister(this);
            }
        }
    }
}
