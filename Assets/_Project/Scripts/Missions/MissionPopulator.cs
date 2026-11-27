using System.Collections.Generic;
using UnityEngine;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.World;

namespace Zenthium.Missions
{
    /// <summary>
    /// Fills an assembled map for a mission (maps.md, Sockets: each mission fills them by its own rules), in an order
    /// that lets later steps build on earlier ones:
    /// <list type="number">
    /// <item>the objective: the Countdown target's socket, and the key building's doors locked (difficulty.md);</item>
    /// <item>Valuables in buildings (world.md, Placement);</item>
    /// <item>Rescue's target citizens in the key building, then citizens in the sockets that roll one;</item>
    /// <item>the starting force, bought from the threat budget and placed by the mission type's profile
    /// (enemies.md, Mission type profiles).</item>
    /// </list>
    /// Heavy Turrets (from rank B) aren't spawned yet: no mission reaches rank B before Milestone 11.
    /// </summary>
    public sealed class MissionPopulator
    {
        private readonly GameDatabase database;
        private readonly MapAssembler assembler;
        private readonly BuildingRegistry registry;
        private readonly CitizenSpawner citizens;
        private readonly ValuableService valuables;
        private readonly MissionEnemySpawner enemies;
        private readonly IRandomSource random;
        private readonly float bodyHeight;
        private readonly float interiorInset;
        private readonly List<StructureMaterial> lockMaterials = new List<StructureMaterial>();
        private readonly List<SpawnSquad> squads = new List<SpawnSquad>();
        private readonly List<Vector3> squadPoints = new List<Vector3>();

        /// <param name="bodyHeight">Meters above the ground for a spawned body's center.</param>
        /// <param name="interiorInset">Meters kept clear of the walls when placing target citizens inside.</param>
        public MissionPopulator(GameDatabase database, MapAssembler assembler, BuildingRegistry registry,
            CitizenSpawner citizens, ValuableService valuables, MissionEnemySpawner enemies, IRandomSource random,
            float bodyHeight, float interiorInset)
        {
            this.database = database;
            this.assembler = assembler;
            this.registry = registry;
            this.citizens = citizens;
            this.valuables = valuables;
            this.enemies = enemies;
            this.random = random;
            this.bodyHeight = bodyHeight;
            this.interiorInset = interiorInset;
        }

        public MissionMap Fill(MissionSetup setup)
        {
            var map = new MissionMap { ObjectivePoint = assembler.KeyPoint };
            if (setup.Type == MissionType.Countdown)
            {
                PlaceCountdownSocket(map);
                LockKeyBuilding(setup, map);
            }

            PlaceValuables(map);
            if (setup.Type == MissionType.Rescue)
            {
                PlaceTargetCitizens(setup, map);
            }

            PlaceScatteredCitizens(map);
            SpawnStartingForce(setup, map);
            return map;
        }

        /// <summary>A free Countdown socket in the key building, or the free one nearest the mission's location.</summary>
        private void PlaceCountdownSocket(MissionMap map)
        {
            MapSocket socket = null;
            if (assembler.KeyBuilding != null)
            {
                foreach (MapSocket candidate in assembler.KeyBuilding.Sockets)
                {
                    if (candidate != null && !assembler.Sockets.IsTaken(candidate)
                        && SocketPicker.WeightFor(candidate.Options, SocketContent.CountdownTarget) > 0f)
                    {
                        socket = candidate;
                        break;
                    }
                }
            }

            if (socket == null)
            {
                socket = SocketSearch.Nearest(assembler.Sockets, SocketContent.CountdownTarget, map.ObjectivePoint,
                    true);
            }

            if (socket == null || !assembler.Sockets.TryTake(socket))
            {
                Debug.LogWarning("Mission: the map has no free Countdown socket; the target goes at the location.");
                return;
            }

            map.CountdownSocket = socket;
            map.ObjectivePoint = Ground(socket.transform.position);
        }

        /// <summary>Every door of the key building, each in a random material the rank allows.</summary>
        private void LockKeyBuilding(MissionSetup setup, MissionMap map)
        {
            database.MissionConstants.LockedDoorMaterials(setup.DifficultyLevel, lockMaterials);
            if (assembler.KeyBuilding == null || lockMaterials.Count == 0)
            {
                return;
            }

            foreach (Door door in assembler.KeyBuilding.GetComponentsInChildren<Door>())
            {
                door.Lock(lockMaterials[Index(lockMaterials.Count)]);
                map.LockedDoors.Add(door);
            }
        }

        private void PlaceValuables(MissionMap map)
        {
            MissionFillConstants constants = database.MissionFillConstants;
            foreach (Building building in registry.Buildings)
            {
                if (building == null)
                {
                    continue;
                }

                int count = MissionFillRules.BuildingValuables(building.Location, constants, random);
                if (count <= 0)
                {
                    continue;
                }

                valuables.DropAround(ValuablesPoint(building) + Vector3.up * bodyHeight, count);
                map.ValuablesPlaced += count;
            }
        }

        /// <summary>The building's free Valuables socket, taken, or the middle of its floor.</summary>
        private Vector3 ValuablesPoint(Building building)
        {
            foreach (MapSocket socket in building.Sockets)
            {
                if (socket != null && SocketPicker.WeightFor(socket.Options, SocketContent.Valuables) > 0f
                    && assembler.Sockets.TryTake(socket))
                {
                    return Ground(socket.transform.position);
                }
            }

            if (building.Zones.Count > 0 && building.Zones[0] != null)
            {
                Vector2 floor = building.Zones[0].Footprint.center;
                return new Vector3(floor.x, 0f, floor.y);
            }

            return Ground(building.transform.position);
        }

        /// <summary>Rescue's targets at random points inside the key building, or round an outdoor location.</summary>
        private void PlaceTargetCitizens(MissionSetup setup, MissionMap map)
        {
            int count = database.MissionConstants.RescueTargets(setup.Size);
            Rect area = TargetArea();
            for (int i = 0; i < count; i++)
            {
                var point = new Vector3(Mathf.Lerp(area.xMin, area.xMax, random.Value01()), 0f,
                    Mathf.Lerp(area.yMin, area.yMax, random.Value01()));
                EntityBehaviour citizen = SpawnCitizen(point, map);
                if (citizen != null)
                {
                    map.TargetCitizens.Add(citizen);
                }
            }
        }

        private Rect TargetArea()
        {
            Building key = assembler.KeyBuilding;
            if (key != null && key.Zones.Count > 0 && key.Zones[0] != null)
            {
                Rect floor = key.Zones[0].Footprint;
                float insetX = Mathf.Min(interiorInset, floor.width * 0.5f);
                float insetY = Mathf.Min(interiorInset, floor.height * 0.5f);
                return Rect.MinMaxRect(floor.xMin + insetX, floor.yMin + insetY, floor.xMax - insetX,
                    floor.yMax - insetY);
            }

            Vector3 center = assembler.KeyPoint;
            float half = database.MapConstants.BlockSize * 0.25f;
            return Rect.MinMaxRect(center.x - half, center.z - half, center.x + half, center.z + half);
        }

        /// <summary>Every free socket that offers a citizen rolls its own content; those that roll one get one.</summary>
        private void PlaceScatteredCitizens(MissionMap map)
        {
            var candidates = new List<MapSocket>();
            foreach (MapSocket socket in assembler.Sockets.All)
            {
                if (socket != null && !assembler.Sockets.IsTaken(socket)
                    && SocketPicker.WeightFor(socket.Options, SocketContent.Citizen) > 0f)
                {
                    candidates.Add(socket);
                }
            }

            foreach (MapSocket socket in candidates)
            {
                if (!assembler.Sockets.TryTakeAndRoll(socket, out SocketContent content))
                {
                    continue;
                }

                if (content == SocketContent.Citizen)
                {
                    SpawnCitizen(Ground(socket.transform.position), map);
                }
                else
                {
                    // Rolled something the mission places by other rules, such as goons; left free for them.
                    assembler.Sockets.Release(socket);
                }
            }
        }

        private EntityBehaviour SpawnCitizen(Vector3 ground, MissionMap map)
        {
            MissionFillConstants constants = database.MissionFillConstants;
            int carried = MissionFillRules.CivilianValuables(constants, random);
            EntityBehaviour citizen = citizens.Spawn(ground + Vector3.up * bodyHeight,
                MissionFillRules.CitizenKindFor(constants, random.Value01()), carried);
            if (citizen != null)
            {
                map.Citizens.Add(citizen);
                map.ValuablesPlaced += carried;
            }

            return citizen;
        }

        private void SpawnStartingForce(MissionSetup setup, MissionMap map)
        {
            SpawnConstants constants = database.SpawnConstants;
            float budget = constants.Budget(setup.Size);
            map.StartingBudget = setup.Type == MissionType.Invasion ? budget * constants.InvasionStartFraction : budget;
            map.WaveBudget = budget - map.StartingBudget;

            var roster = new FactionRoster(setup.Faction, database.Entities.Entries, setup.DifficultyLevel);
            ThreatBudgetSpender.Spend(map.StartingBudget, constants.ChampionShare(setup.DifficultyLevel), roster,
                constants, random, squads);

            float guardCost = 0f;
            float guardBudget = 0f;
            if (setup.Type == MissionType.Countdown)
            {
                foreach (SpawnSquad squad in squads)
                {
                    guardBudget += squad.Cost;
                }

                guardBudget *= database.MissionConstants.CountdownGuardFraction;
            }

            squadPoints.Clear();
            foreach (SpawnSquad squad in squads)
            {
                MapSocket socket;
                if (setup.Type == MissionType.Countdown && guardCost < guardBudget)
                {
                    socket = TakeNear(map.ObjectivePoint);
                    guardCost += squad.Cost;
                }
                else
                {
                    socket = TakeFor(setup.Type, map);
                }

                if (socket == null)
                {
                    Debug.LogWarning("Mission: the map has no Goon socket left; the rest of the force isn't spawned.");
                    return;
                }

                squadPoints.Add(socket.transform.position);
                enemies.SpawnSquad(squad, socket, assembler.Sockets, setup.DifficultyLevel,
                    constants.PopulationCap);
            }
        }

        /// <summary>A squad's socket by the mission type's profile (enemies.md, Mission type profiles).</summary>
        private MapSocket TakeFor(MissionType type, MissionMap map)
        {
            switch (type)
            {
                case MissionType.Rescue when map.Citizens.Count > 0:
                    // Near clusters of citizens: next to one picked at random.
                    return TakeNear(map.Citizens[Index(map.Citizens.Count)].transform.position);
                case MissionType.ArsonRiot when squadPoints.Count > 0:
                    // Spread across the map: as far as it can be from every other squad.
                    MapSocket spread = SocketSearch.FarthestFrom(assembler.Sockets, SocketContent.Goon, squadPoints);
                    return spread != null && assembler.Sockets.TryTake(spread) ? spread : TakeNear(map.ObjectivePoint);
                default:
                    return assembler.Sockets.TryTake(SocketContent.Goon, out MapSocket socket) ? socket
                        : TakeNear(map.ObjectivePoint);
            }
        }

        /// <summary>The free Goon socket nearest a point; once none is free, squads share the nearest one.</summary>
        private MapSocket TakeNear(Vector3 point)
        {
            MapSocket socket = SocketSearch.Nearest(assembler.Sockets, SocketContent.Goon, point, true);
            if (socket != null && assembler.Sockets.TryTake(socket))
            {
                return socket;
            }

            return SocketSearch.Nearest(assembler.Sockets, SocketContent.Goon, point, false);
        }

        private int Index(int count)
        {
            return Mathf.Min((int)(random.Value01() * count), count - 1);
        }

        private static Vector3 Ground(Vector3 position)
        {
            position.y = 0f;
            return position;
        }
    }
}
