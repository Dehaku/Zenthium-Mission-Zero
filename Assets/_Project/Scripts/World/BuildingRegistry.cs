using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Modules;

namespace Zenthium.World
{
    /// <summary>
    /// Every building on the map (world.md, Buildings): each registers with an ID, its location type and its sockets,
    /// and gets its neighbors within the neighbor radius. Mission rules count from here, such as Arson Riot's share
    /// of burning buildings. One per scene.
    /// </summary>
    public sealed class BuildingRegistry : MonoBehaviour
    {
        [SerializeField] private GameDatabase database;

        [Tooltip("The scene's module services: the clock, combat rules and randomness that fire runs on.")]
        [SerializeField] private ModuleWorld world;

        [Tooltip("Fire spreads between neighbors (world.md, Fire): from rank A. Milestone 11 sets it from the rank.")]
        [SerializeField] private bool isSpreadOn;

        private readonly List<Building> buildings = new List<Building>();
        private int nextId;

        public IReadOnlyList<Building> Buildings => buildings;

        /// <summary>Null if the scene has none; buildings can't burn then.</summary>
        public ModuleWorld World => world;

        public bool IsSpreadOn
        {
            get => isSpreadOn;
            set => isSpreadOn = value;
        }

        /// <summary>
        /// Works out every building's neighbors. It runs once on Start, after the scene's buildings have registered;
        /// a map assembled at runtime calls it again once its blocks are placed.
        /// </summary>
        public void RebuildNeighbors()
        {
            if (database == null)
            {
                return;
            }

            var footprints = new List<IReadOnlyList<Rect>>(buildings.Count);
            foreach (Building building in buildings)
            {
                var footprint = new List<Rect>();
                building.AddFootprint(footprint);
                footprints.Add(footprint);
            }

            var neighborIndices = new List<List<int>>();
            BuildingNeighbors.Compute(footprints, database.WorldConstants.BuildingNeighborRadius, neighborIndices);
            var neighbors = new List<Building>();
            for (int i = 0; i < buildings.Count; i++)
            {
                neighbors.Clear();
                foreach (int index in neighborIndices[i])
                {
                    neighbors.Add(buildings[index]);
                }

                buildings[i].SetNeighbors(neighbors);
            }
        }

        internal void Register(Building building)
        {
            if (buildings.Contains(building))
            {
                return;
            }

            if (building.Id < 0)
            {
                building.SetId(nextId++);
            }

            buildings.Add(building);
        }

        internal void Unregister(Building building)
        {
            buildings.Remove(building);
        }

        private void Awake()
        {
            if (database == null || world == null)
            {
                Debug.LogError($"{name}: {nameof(BuildingRegistry)} needs a Game Database and the Module World.", this);
            }
        }

        private void Start()
        {
            RebuildNeighbors();
        }
    }
}
