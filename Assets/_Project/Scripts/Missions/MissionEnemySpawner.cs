using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.AI;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.World;

namespace Zenthium.Missions
{
    /// <summary>
    /// Spawns a mission's enemies from the standard spawner (enemies.md): each squad around its socket, patrolling it
    /// and its nearest Goon sockets, and the Leader. It keeps to the population cap: what doesn't fit is held back
    /// for reinforcements to send in once there's room (Milestone 9b).
    /// </summary>
    public sealed class MissionEnemySpawner : MonoBehaviour
    {
        [SerializeField] private FactionEntitySpawner spawner;
        [SerializeField] private ModuleWorld world;

        [Tooltip("Sockets a squad patrols between, its own included.")]
        [SerializeField, Min(2)] private int patrolSockets = 3;

        [Tooltip("Meters from its socket that a squad's members spawn, spread in a ring.")]
        [SerializeField, Min(0f)] private float spawnSpacing = 1.5f;

        [Tooltip("Meters above the ground for a spawned body's center.")]
        [SerializeField, Min(0f)] private float bodyHeight = 1f;

        private readonly List<EntityBehaviour> enemies = new List<EntityBehaviour>();
        private readonly List<EntityData> held = new List<EntityData>();
        private readonly List<MapSocket> route = new List<MapSocket>();
        private readonly List<MapSocket> neighbors = new List<MapSocket>();
        private int squadCount;

        /// <summary>Raised for each enemy it spawns.</summary>
        public event Action<EntityBehaviour> OnSpawned;

        /// <summary>Every enemy it spawned, defeated or not: they count toward the population cap while alive.</summary>
        public IReadOnlyList<EntityBehaviour> Enemies => enemies;

        /// <summary>Bought from the budget but held back by the population cap.</summary>
        public IReadOnlyList<EntityData> Held => held;

        /// <summary>Spawned enemies still in play: not defeated, captured, escaped or abducted.</summary>
        public int AliveCount
        {
            get
            {
                int alive = 0;
                foreach (EntityBehaviour enemy in enemies)
                {
                    if (IsAlive(enemy))
                    {
                        alive++;
                    }
                }

                return alive;
            }
        }

        public static bool IsAlive(EntityBehaviour enemy)
        {
            if (enemy == null || enemy.Entity == null)
            {
                return false;
            }

            Entity entity = enemy.Entity;
            return !entity.IsIncapacitated && !entity.IsCaptured && !entity.IsEscaped && !entity.IsAbducted;
        }

        /// <summary>
        /// Spawns the squad around the socket, its Champion first. Members past the population cap are held back.
        /// </summary>
        public void SpawnSquad(SpawnSquad squad, MapSocket socket, MapSockets sockets, int difficultyLevel,
            int populationCap)
        {
            if (spawner == null || world == null || socket == null)
            {
                Debug.LogError($"{name}: {nameof(MissionEnemySpawner)} needs the spawner, the Module World and a socket.",
                    this);
                return;
            }

            AiSquad patrol = CreatePatrol(socket, sockets);
            var members = new List<EntityData>(squad.Count);
            if (squad.Champion != null)
            {
                members.Add(squad.Champion);
            }

            members.AddRange(squad.Goons);
            for (int i = 0; i < members.Count; i++)
            {
                if (AliveCount >= populationCap)
                {
                    held.Add(members[i]);
                    continue;
                }

                Vector3 position = RingPoint(socket.transform.position, i, members.Count);
                Track(spawner.SpawnInSquad(members[i], Team.Enemy, difficultyLevel, position, patrol));
            }
        }

        /// <summary>The Leader, outside the budget but counted toward the cap once it's in (enemies.md, Limits).</summary>
        public EntityBehaviour SpawnLeader(EntityData leader, Vector3 ground, int difficultyLevel)
        {
            if (spawner == null || leader == null)
            {
                return null;
            }

            EntityBehaviour spawned = spawner.SpawnInSquad(leader, Team.Enemy, difficultyLevel,
                ground + Vector3.up * bodyHeight, null);
            Track(spawned);
            return spawned;
        }

        private void Track(EntityBehaviour spawned)
        {
            if (spawned == null)
            {
                return;
            }

            enemies.Add(spawned);
            OnSpawned?.Invoke(spawned);
        }

        /// <summary>A squad that patrols its socket and the Goon sockets nearest it.</summary>
        private AiSquad CreatePatrol(MapSocket socket, MapSockets sockets)
        {
            SocketSearch.Neighbors(sockets, socket, SocketContent.Goon, patrolSockets - 1, neighbors);
            route.Clear();
            route.Add(socket);
            route.AddRange(neighbors);

            // Configured while inactive, since it needs the clock when it wakes.
            var squadObject = new GameObject($"Squad {++squadCount}");
            squadObject.SetActive(false);
            squadObject.transform.SetParent(transform, false);
            squadObject.transform.position = socket.transform.position;
            var patrol = squadObject.AddComponent<AiSquad>();
            patrol.Configure(world.Clock, route);
            squadObject.SetActive(true);
            return patrol;
        }

        private Vector3 RingPoint(Vector3 center, int index, int count)
        {
            Vector3 point = center + Vector3.up * bodyHeight;
            if (count < 2)
            {
                return point;
            }

            float angle = index * Mathf.PI * 2f / count;
            return point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnSpacing;
        }
    }
}
