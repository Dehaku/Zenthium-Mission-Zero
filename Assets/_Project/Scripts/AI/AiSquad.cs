using System.Collections.Generic;
using UnityEngine;
using Zenthium.Core;
using Zenthium.Timing;
using Zenthium.World;

namespace Zenthium.AI
{
    /// <summary>
    /// A squad's patrol while Idle (enemies.md, Squads): its members gather around one socket, wait, then all head to
    /// another of its sockets, chosen at random. Members in combat or searching don't hold the others up.
    /// </summary>
    public sealed class AiSquad : MonoBehaviour
    {
        [SerializeField] private GameClockDriver clock;

        [Tooltip("The sockets it patrols between. Needs at least two.")]
        [SerializeField] private List<MapSocket> sockets = new List<MapSocket>();

        [Tooltip("Game seconds the squad waits at a socket once everyone on patrol is there.")]
        [SerializeField, Min(0f)] private float pauseSeconds = 3f;

        [Tooltip("Meters from the socket that members stand, spread in a ring.")]
        [SerializeField, Min(0f)] private float spacing = 1.5f;

        [Tooltip("Meters from its spot that count as a member being there.")]
        [SerializeField, Min(0.1f)] private float arriveDistance = 1f;

        private readonly List<AiBrain> members = new List<AiBrain>();
        private readonly IRandomSource random = new UnityRandomSource();
        private int current;
        private float pauseLeft;

        /// <summary>
        /// Sets up a squad made in code, such as a mission's: add it to an inactive object, configure it, then activate
        /// it. The first socket is where it starts.
        /// </summary>
        public void Configure(GameClockDriver newClock, IReadOnlyList<MapSocket> newSockets)
        {
            clock = newClock;
            sockets.Clear();
            sockets.AddRange(newSockets);
            current = 0;
        }

        /// <summary>Where a member stands at the current socket.</summary>
        public Vector3 PatrolPoint(AiBrain member)
        {
            if (sockets.Count == 0 || sockets[current] == null)
            {
                return member.transform.position;
            }

            Vector3 socket = sockets[current].transform.position;
            int index = members.IndexOf(member);
            if (members.Count < 2 || index < 0)
            {
                return socket;
            }

            float angle = index * Mathf.PI * 2f / members.Count;
            return socket + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spacing;
        }

        internal void Join(AiBrain member)
        {
            if (!members.Contains(member))
            {
                members.Add(member);
            }
        }

        internal void Leave(AiBrain member)
        {
            members.Remove(member);
        }

        private void Awake()
        {
            pauseLeft = pauseSeconds;
            if (clock == null)
            {
                Debug.LogError($"{name}: {nameof(AiSquad)} needs the scene's Game Clock Driver.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (sockets.Count < 2 || !AllPatrollingArrived())
            {
                pauseLeft = pauseSeconds;
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            pauseLeft -= clock.Clock.GameDelta(realDelta, false);
            if (pauseLeft <= 0f)
            {
                pauseLeft = pauseSeconds;
                current = NextSocket();
            }
        }

        /// <summary>False if no member is patrolling, so a squad all in combat stays where it was headed.</summary>
        private bool AllPatrollingArrived()
        {
            bool anyPatrolling = false;
            foreach (AiBrain member in members)
            {
                if (!member.IsPatrolling)
                {
                    continue;
                }

                anyPatrolling = true;
                if (!member.IsNear(PatrolPoint(member), arriveDistance))
                {
                    return false;
                }
            }

            return anyPatrolling;
        }

        private int NextSocket()
        {
            int next = Mathf.Min((int)(random.Value01() * (sockets.Count - 1)), sockets.Count - 2);
            return next >= current ? next + 1 : next;
        }
    }
}
