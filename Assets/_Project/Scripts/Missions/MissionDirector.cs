using System;
using UnityEngine;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.World;

namespace Zenthium.Missions
{
    /// <summary>
    /// Runs a mission in the Mission scene: takes the pending mission when the scene loads (CLAUDE.md, Scenes and
    /// flow), gives the player its hero, builds the map for its rank, fills it (<see cref="MissionPopulator"/>), and
    /// starts the player in the exit zone farthest from the goal. The Leader enters after its delay. With no mission
    /// pending it waits, so a launcher can start one.
    /// </summary>
    public sealed class MissionDirector : MonoBehaviour
    {
        // Added to the mission's seed for the fill's rolls, so they don't repeat the map's.
        private const int FillSeedOffset = 7919;

        [SerializeField] private ModuleWorld world;
        [SerializeField] private MapAssembler assembler;
        [SerializeField] private BuildingRegistry registry;
        [SerializeField] private EntityBehaviour player;
        [SerializeField] private MissionEnemySpawner enemies;
        [SerializeField] private CitizenSpawner citizens;
        [SerializeField] private ValuableService valuables;

        [Tooltip("Meters above the ground for a body's center.")]
        [SerializeField, Min(0f)] private float bodyHeight = 1f;

        [Tooltip("Meters kept clear of the walls when placing Rescue's target citizens inside the key building.")]
        [SerializeField, Min(0f)] private float interiorInset = 1.5f;

        private IRandomSource random;
        private bool leaderEntered;

        /// <summary>Raised once a mission is built and filled.</summary>
        public event Action OnMissionStarted;

        /// <summary>Raised when the Leader makes its entrance.</summary>
        public event Action<EntityBehaviour> OnLeaderEntered;

        /// <summary>Null until a mission starts.</summary>
        public MissionSetup Setup { get; private set; }

        /// <summary>What the mission put on the map; null until a mission starts.</summary>
        public MissionMap Map { get; private set; }

        public bool IsRunning => Setup != null;

        /// <summary>Game seconds since the mission started; it stops while paused and slows with game time.</summary>
        public float ElapsedSeconds { get; private set; }

        public MapAssembler Assembler => assembler;
        public MissionEnemySpawner Enemies => enemies;
        public EntityBehaviour Player => player;

        private void Start()
        {
            if (PendingMission.TryTake(out MissionLaunch launch))
            {
                Begin(launch);
            }
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            ElapsedSeconds += world.Clock.Clock.GameDelta(realDelta, false);
            if (Setup.HasLeader && !leaderEntered
                && ElapsedSeconds >= world.Database.SpawnConstants.LeaderEntranceSeconds)
            {
                EnterLeader();
            }
        }

        private void Begin(MissionLaunch launch)
        {
            if (world == null || assembler == null || registry == null || player == null || enemies == null
                || citizens == null || valuables == null)
            {
                Debug.LogError($"{name}: {nameof(MissionDirector)} is missing references.", this);
                return;
            }

            MissionSetup setup = launch.Setup;
            if (launch.Hero != null)
            {
                player.SetHero(launch.Hero, launch.UnlockedSlots, null);
            }

            GameDatabase database = world.Database;
            var request = new MapRequest(setup.Location, setup.Size, setup.Seed,
                database.MissionConstants.ExitZones(setup.DifficultyLevel), setup.Faction, setup.DifficultyLevel);
            if (!assembler.TryAssemble(request, out string problem))
            {
                Debug.LogError($"{name}: couldn't build the map for {setup}: {problem}", this);
                return;
            }

            random = new SeededRandomSource(setup.Seed + FillSeedOffset);
            var populator = new MissionPopulator(database, assembler, registry, citizens, valuables, enemies, random,
                bodyHeight, interiorInset);
            Setup = setup;
            Map = populator.Fill(setup);
            MovePlayer(StartPoint());
            Debug.Log($"Mission: {setup}. {enemies.Enemies.Count} enemies (held back by the cap: "
                + $"{enemies.Held.Count}), {Map.Citizens.Count} citizens ({Map.TargetCitizens.Count} targets), "
                + $"{Map.ValuablesPlaced} Valuables, {Map.LockedDoors.Count} locked doors, "
                + $"{assembler.ExitZones.Count} exit zones.", this);
            OnMissionStarted?.Invoke();
        }

        /// <summary>The exit zone farthest from the goal, or the crossing nearest the center if there's none.</summary>
        private Vector3 StartPoint()
        {
            Vector3 start = assembler.CenterCrossing;
            float farthest = -1f;
            foreach (ExitZone zone in assembler.ExitZones)
            {
                float distance = (zone.transform.position - Map.ObjectivePoint).sqrMagnitude;
                if (distance > farthest)
                {
                    farthest = distance;
                    start = zone.transform.position;
                }
            }

            start.y = 0f;
            return start;
        }

        private void MovePlayer(Vector3 ground)
        {
            Vector3 position = ground + Vector3.up * bodyHeight;
            if (player.TryGetComponent(out Rigidbody body))
            {
                body.position = position;
                body.linearVelocity = Vector3.zero;
            }

            player.transform.position = position;
        }

        /// <summary>
        /// The Leader drops in at a socket away from the player (enemies.md, Threat budget: its entrance uses the
        /// faction's arrival tag, which Milestone 9b shows).
        /// </summary>
        private void EnterLeader()
        {
            leaderEntered = true;
            var roster = new FactionRoster(Setup.Faction, world.Database.Entities.Entries, Setup.DifficultyLevel);
            if (roster.Leader == null)
            {
                return;
            }

            MapSocket socket = SocketSearch.RandomAwayFrom(assembler.Sockets, SocketContent.Goon,
                player.transform.position, world.Database.SpawnConstants.WaveDropMinDistance, random);
            Vector3 ground = socket != null ? socket.transform.position : Map.ObjectivePoint;
            ground.y = 0f;
            EntityBehaviour leader = enemies.SpawnLeader(roster.Leader, ground, Setup.DifficultyLevel);
            if (leader != null)
            {
                Debug.Log($"Mission: {roster.Leader.Id} makes an entrance.", leader);
                OnLeaderEntered?.Invoke(leader);
            }
        }
    }
}
