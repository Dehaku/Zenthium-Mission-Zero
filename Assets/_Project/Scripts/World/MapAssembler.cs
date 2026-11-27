using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Modules;

namespace Zenthium.World
{
    /// <summary>What to build: the mission's location, which picks the district and the key block, and its size.</summary>
    public readonly struct MapRequest
    {
        /// <param name="seed">The same seed with the same blocks builds the same map.</param>
        /// <param name="exitZones">How many exit zones; the mission sets it by rank (Milestone 9).</param>
        /// <param name="faction">The mission's faction, for crop circles; null for none.</param>
        /// <param name="difficultyLevel">Mission ranks above D (0 at D), for the crop circle count.</param>
        public MapRequest(LocationType location, MapSize size, int seed, int exitZones, FactionData faction,
            int difficultyLevel)
        {
            Location = location;
            Size = size;
            Seed = seed;
            ExitZones = exitZones;
            Faction = faction;
            DifficultyLevel = difficultyLevel;
        }

        public LocationType Location { get; }
        public MapSize Size { get; }
        public int Seed { get; }
        public int ExitZones { get; }
        public FactionData Faction { get; }
        public int DifficultyLevel { get; }
    }

    /// <summary>
    /// Builds a random Blazithion map at runtime (maps.md, Generation): it lays the district's blocks on a street
    /// grid (<see cref="MapLayout"/>), hands every building and structure the scene's services a prefab can't hold,
    /// registers the buildings and works out their neighbors, sizes <see cref="MapBounds"/>, bakes the NavMesh, and
    /// places exit zones and the faction's crop circles. Each new map replaces the last.
    /// </summary>
    public sealed class MapAssembler : MonoBehaviour
    {
        // NavMeshAgent's default agent type, Humanoid; the AI's agents use it.
        private const int HumanoidAgentTypeId = 0;

        // Meters of headroom above and below the ground for the bake: the map is one ground floor (maps.md).
        private const float BakeHeight = 20f;

        [Tooltip("The scene's module services: the game data, clock and combat rules the map's structures run on.")]
        [SerializeField] private ModuleWorld world;

        [SerializeField] private BuildingRegistry registry;

        [Tooltip("Sized to each new map. Everything that references it, such as the spawner and AI, follows the map.")]
        [SerializeField] private MapBounds mapBounds;

        [SerializeField] private MapBlockLibrary library;

        [Tooltip("The street surface: a box 1 m across, scaled to the map along X and Z. Its top should be at Y 0.")]
        [SerializeField] private Transform groundPrefab;

        [Tooltip("1 m across at scale 1; scaled to the exit zone size from constants.csv.")]
        [SerializeField] private ExitZone exitZonePrefab;

        [Tooltip("1 m across at scale 1; scaled to the faction's crop circle size from factions.csv.")]
        [SerializeField] private CropCircle cropCirclePrefab;

        [Tooltip("How many turns of the exit zones round the map to try, to keep them away from the key building.")]
        [SerializeField, Min(1)] private int exitPlacementSteps = 16;

        private readonly List<ExitZone> exitZones = new List<ExitZone>();
        private readonly List<CropCircle> cropCircles = new List<CropCircle>();
        private readonly List<MapBlockOption> options = new List<MapBlockOption>();
        private readonly List<Vector2> exitPoints = new List<Vector2>();
        private readonly List<NavMeshBuildSource> navSources = new List<NavMeshBuildSource>();
        private readonly List<NavMeshBuildMarkup> navMarkups = new List<NavMeshBuildMarkup>();
        private NavMeshDataInstance navMesh;
        private GameObject mapRoot;

        /// <summary>Raised when a new map is built and baked.</summary>
        public event Action OnAssembled;

        /// <summary>Null until a map is built.</summary>
        public MapLayout Layout { get; private set; }

        /// <summary>The map's sockets, for missions to fill (Milestone 9). Null until a map is built.</summary>
        public MapSockets Sockets { get; private set; }

        /// <summary>The building of the mission's location, in the key block; null if it has none, such as a Park.</summary>
        public Building KeyBuilding { get; private set; }

        public IReadOnlyList<ExitZone> ExitZones => exitZones;
        public IReadOnlyList<CropCircle> CropCircles => cropCircles;

        /// <summary>Real milliseconds the last NavMesh bake took, to check it isn't too slow on WebGL.</summary>
        public float LastBakeMilliseconds { get; private set; }

        /// <summary>
        /// The middle of the mission's location, on the ground: its key building's floor, or the key block for an
        /// outdoor location such as a Park.
        /// </summary>
        public Vector3 KeyPoint
        {
            get
            {
                if (Layout == null)
                {
                    return transform.position;
                }

                if (KeyBuilding != null && KeyBuilding.Zones.Count > 0 && KeyBuilding.Zones[0] != null)
                {
                    Vector2 floor = KeyBuilding.Zones[0].Footprint.center;
                    return new Vector3(floor.x, transform.position.y, floor.y);
                }

                Vector2 block = Layout.Blocks[Layout.KeyBlock].Area.center;
                return transform.position + new Vector3(block.x, 0f, block.y);
            }
        }

        /// <summary>The street crossing nearest the map's center, on the ground.</summary>
        public Vector3 CenterCrossing
        {
            get
            {
                if (Layout == null)
                {
                    return transform.position;
                }

                MapConstants constants = world.Database.MapConstants;
                float cell = constants.BlockSize + constants.StreetWidth;
                float offset = -Layout.Side * 0.5f + Layout.BlocksPerSide / 2 * cell + constants.StreetWidth * 0.5f;
                return transform.position + new Vector3(offset, 0f, offset);
            }
        }

        /// <summary>Replaces the current map with a new one.</summary>
        /// <param name="problem">Why it couldn't, such as no block holding the location. The old map stays then.</param>
        public bool TryAssemble(MapRequest request, out string problem)
        {
            if (world == null || registry == null || mapBounds == null || library == null || groundPrefab == null
                || exitZonePrefab == null || cropCirclePrefab == null)
            {
                problem = $"{nameof(MapAssembler)} needs the Module World, Building Registry, Map Bounds, block "
                    + "library, ground, exit zone and crop circle prefabs.";
                return false;
            }

            options.Clear();
            foreach (MapBlock block in library.Blocks)
            {
                if (block == null)
                {
                    problem = $"{library.name} has a missing block: run Zenthium > Map > Refresh Block Library.";
                    return false;
                }

                options.Add(block.ToOption());
            }

            MapConstants constants = world.Database.MapConstants;
            var random = new SeededRandomSource(request.Seed);
            if (!MapLayout.TryCreate(constants.Size(request.Size), constants.BlockSize, constants.StreetWidth,
                    request.Location, options, random, out MapLayout layout, out problem))
            {
                return false;
            }

            Clear();
            Layout = layout;
            Sockets = new MapSockets(random);

            // Built inactive, so nothing wakes before it has the scene's services.
            mapRoot = new GameObject("Map");
            mapRoot.SetActive(false);
            mapRoot.transform.SetParent(transform, false);
            PlaceGround(layout.Side);
            for (int i = 0; i < layout.Blocks.Count; i++)
            {
                PlaceBlock(layout.Blocks[i], i == layout.KeyBlock, request.Location);
            }

            mapRoot.SetActive(true);
            registry.RebuildNeighbors();
            mapBounds.SetArea(transform.position, layout.Side);
            BakeNavMesh(layout.Side);
            PlaceExitZones(layout.Side, constants, request.ExitZones);
            PlaceCropCircles(request);
            Debug.Log($"Map: {request.Size} {LocationDistricts.Of(request.Location)} ({layout.BlocksPerSide} x "
                + $"{layout.BlocksPerSide} blocks, {layout.Side} m) around a {request.Location}, seed {request.Seed}. "
                + $"NavMesh bake: {LastBakeMilliseconds:F0} ms.", this);
            OnAssembled?.Invoke();
            return true;
        }

        private void OnDestroy()
        {
            RemoveNavMesh();
        }

        private void Clear()
        {
            RemoveNavMesh();
            exitZones.Clear();
            cropCircles.Clear();
            KeyBuilding = null;
            if (mapRoot != null)
            {
                // Switched off first, so its buildings and crop circles unregister now, not at the end of the frame.
                mapRoot.SetActive(false);
                Destroy(mapRoot);
                mapRoot = null;
            }
        }

        private void RemoveNavMesh()
        {
            if (navMesh.valid)
            {
                navMesh.Remove();
            }
        }

        private void PlaceGround(float side)
        {
            Transform ground = Instantiate(groundPrefab, mapRoot.transform, false);
            ground.name = "Ground";
            Vector3 scale = ground.localScale;
            ground.localScale = new Vector3(side, scale.y, side);
        }

        private void PlaceBlock(BlockPlacement placement, bool isKey, LocationType keyLocation)
        {
            MapBlock prefab = library.Blocks[placement.Option];
            var position = new Vector3(placement.Area.center.x, 0f, placement.Area.center.y);
            MapBlock block = Instantiate(prefab, mapRoot.transform, false);
            block.transform.SetLocalPositionAndRotation(position,
                Quaternion.Euler(0f, placement.QuarterTurns * 90f, 0f));

            foreach (Structure structure in block.GetComponentsInChildren<Structure>(true))
            {
                structure.SetWorld(world);
            }

            foreach (Building building in block.GetComponentsInChildren<Building>(true))
            {
                building.SetRegistry(registry);
                if (isKey && KeyBuilding == null && building.Location == keyLocation)
                {
                    KeyBuilding = building;
                }
            }

            foreach (MapSocket socket in block.GetComponentsInChildren<MapSocket>(true))
            {
                Sockets.Add(socket);
            }
        }

        /// <summary>
        /// Bakes the ground and the blocks' fixed obstacles. Walls, windows and props are left out, since they carve
        /// their own holes and open the NavMesh when broken; doors stay walkable, as enemies plan paths as if doors
        /// weren't there (world.md, Doors); interior zones only carve while their building burns.
        /// </summary>
        private void BakeNavMesh(float side)
        {
            double start = Time.realtimeSinceStartupAsDouble;
            navMarkups.Clear();
            AddIgnored<Structure>();
            AddIgnored<Door>();
            AddIgnored<InteriorZone>();
            NavMeshBuilder.CollectSources(mapRoot.transform, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, false,
                navMarkups, false, navSources);

            var bounds = new Bounds(Vector3.zero, new Vector3(side, BakeHeight, side));
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(HumanoidAgentTypeId),
                navSources, bounds, mapRoot.transform.position, Quaternion.identity);
            navMesh = NavMesh.AddNavMeshData(data, mapRoot.transform.position, Quaternion.identity);
            LastBakeMilliseconds = (float)((Time.realtimeSinceStartupAsDouble - start) * 1000.0);
        }

        private void AddIgnored<T>() where T : Component
        {
            foreach (T component in mapRoot.GetComponentsInChildren<T>(true))
            {
                navMarkups.Add(new NavMeshBuildMarkup
                {
                    root = component.transform,
                    ignoreFromBuild = true,
                    applyToChildren = true,
                });
            }
        }

        /// <summary>Spread evenly round the edge, turned to keep them away from the mission's location.</summary>
        private void PlaceExitZones(float side, MapConstants constants, int count)
        {
            Vector3 key = KeyPoint - transform.position;
            ExitZonePlacement.PlaceAwayFrom(side, constants.StreetWidth, count, new Vector2(key.x, key.z),
                exitPlacementSteps, exitPoints);

            foreach (Vector2 point in exitPoints)
            {
                ExitZone zone = Instantiate(exitZonePrefab, mapRoot.transform, false);
                zone.transform.localPosition = new Vector3(point.x, zone.transform.localPosition.y, point.y);
                zone.Configure(constants.ExitZoneSize);
                exitZones.Add(zone);
            }
        }

        /// <summary>
        /// A faction whose loot goes to crop circles gets <see cref="CropCircleRules.Count"/> of them, in random
        /// sockets that offer one. With too few such sockets it gets fewer, with a warning.
        /// </summary>
        private void PlaceCropCircles(MapRequest request)
        {
            FactionData faction = request.Faction;
            if (faction == null || !HasTag(faction, EntityTag.LootDestinationCropCircle))
            {
                return;
            }

            if (!CropCircleRules.TryCreate(faction, out CropCircleRules rules, out string problem))
            {
                Debug.LogError($"{name}: no crop circles for {faction.Id}: {problem}", this);
                return;
            }

            // Staged under an inactive parent, so each wakes only once it has the scene's Module World.
            var staging = new GameObject("Staging");
            staging.SetActive(false);
            staging.transform.SetParent(mapRoot.transform, false);
            int wanted = rules.Count(request.DifficultyLevel);
            for (int i = 0; i < wanted && Sockets.TryTake(SocketContent.CropCircle, out MapSocket socket); i++)
            {
                CropCircle circle = Instantiate(cropCirclePrefab, staging.transform, false);
                circle.Configure(world, faction);
                Vector3 scale = circle.transform.localScale;
                circle.transform.localScale = new Vector3(rules.Radius * 2f, scale.y, rules.Radius * 2f);
                circle.transform.SetParent(mapRoot.transform, true);
                circle.transform.position = new Vector3(socket.transform.position.x, circle.transform.position.y,
                    socket.transform.position.z);
                cropCircles.Add(circle);
            }

            Destroy(staging);
            if (cropCircles.Count < wanted)
            {
                Debug.LogWarning($"{name}: {faction.Id} needs {wanted} crop circles, but the map has only "
                    + $"{cropCircles.Count} free sockets that offer one.", this);
            }
        }

        private static bool HasTag(FactionData faction, EntityTag tag)
        {
            foreach (EntityTag held in faction.Tags)
            {
                if (held == tag)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
