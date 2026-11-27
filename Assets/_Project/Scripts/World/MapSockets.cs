using System.Collections.Generic;
using Zenthium.Core;

namespace Zenthium.World
{
    /// <summary>
    /// Every socket on an assembled map, and which are taken (maps.md, Sockets). Missions fill sockets by their own
    /// rules through it (Milestone 9): ask for a socket that offers some content, or let a socket roll its own.
    /// </summary>
    public sealed class MapSockets
    {
        private readonly IRandomSource random;
        private readonly List<MapSocket> sockets = new List<MapSocket>();
        private readonly HashSet<MapSocket> taken = new HashSet<MapSocket>();
        private readonly List<float> weights = new List<float>();

        public MapSockets(IRandomSource random)
        {
            this.random = random;
        }

        public IReadOnlyList<MapSocket> All => sockets;

        public void Add(MapSocket socket)
        {
            if (socket != null && !sockets.Contains(socket))
            {
                sockets.Add(socket);
            }
        }

        public void Clear()
        {
            sockets.Clear();
            taken.Clear();
        }

        public bool IsTaken(MapSocket socket)
        {
            return taken.Contains(socket);
        }

        /// <summary>
        /// Takes a free socket that offers the content, picked at random: the more weight a socket gives the content,
        /// the likelier it is.
        /// </summary>
        /// <returns>False if no free socket offers it.</returns>
        public bool TryTake(SocketContent content, out MapSocket socket)
        {
            weights.Clear();
            foreach (MapSocket candidate in sockets)
            {
                weights.Add(candidate == null || taken.Contains(candidate)
                    ? 0f
                    : SocketPicker.WeightFor(candidate.Options, content));
            }

            int index = SocketPicker.Pick(weights, random.Value01());
            socket = index >= 0 ? sockets[index] : null;
            if (socket != null)
            {
                taken.Add(socket);
            }

            return socket != null;
        }

        /// <summary>Takes a socket the caller chose, such as the one nearest a point.</summary>
        /// <returns>False if it's taken or not on this map.</returns>
        public bool TryTake(MapSocket socket)
        {
            return socket != null && sockets.Contains(socket) && taken.Add(socket);
        }

        /// <summary>Takes the socket, if free, and rolls what it spawns by its own weights.</summary>
        /// <returns>False if it's taken or offers nothing.</returns>
        public bool TryTakeAndRoll(MapSocket socket, out SocketContent content)
        {
            content = default;
            if (socket == null || taken.Contains(socket)
                || !SocketPicker.TryRollContent(socket.Options, random.Value01(), out content))
            {
                return false;
            }

            taken.Add(socket);
            return true;
        }

        /// <summary>Frees a socket again, such as when what spawned there is gone for good.</summary>
        public void Release(MapSocket socket)
        {
            taken.Remove(socket);
        }
    }
}
