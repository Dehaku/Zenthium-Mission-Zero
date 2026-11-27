using System.Collections.Generic;
using UnityEngine;
using Zenthium.Core;
using Zenthium.World;

namespace Zenthium.Missions
{
    /// <summary>Finds map sockets that offer some content by where they are, for placing a mission's spawns.</summary>
    public static class SocketSearch
    {
        /// <summary>The socket nearest a point that offers the content.</summary>
        /// <param name="freeOnly">Skip sockets already taken.</param>
        /// <returns>Null if none.</returns>
        public static MapSocket Nearest(MapSockets sockets, SocketContent content, Vector3 point, bool freeOnly,
            MapSocket except = null)
        {
            MapSocket nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (MapSocket socket in sockets.All)
            {
                if (!Offers(sockets, socket, content, freeOnly) || socket == except)
                {
                    continue;
                }

                float distance = Flat(socket.transform.position - point).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = socket;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /// <summary>The free socket offering the content whose nearest point in the list is farthest away.</summary>
        /// <returns>Null if none.</returns>
        public static MapSocket FarthestFrom(MapSockets sockets, SocketContent content, IReadOnlyList<Vector3> points)
        {
            MapSocket best = null;
            float bestDistance = -1f;
            foreach (MapSocket socket in sockets.All)
            {
                if (!Offers(sockets, socket, content, true))
                {
                    continue;
                }

                float nearest = float.MaxValue;
                foreach (Vector3 point in points)
                {
                    nearest = Mathf.Min(nearest, Flat(socket.transform.position - point).sqrMagnitude);
                }

                if (nearest > bestDistance)
                {
                    best = socket;
                    bestDistance = nearest;
                }
            }

            return best;
        }

        /// <summary>A random socket offering the content at least a distance from a point, free or not.</summary>
        /// <returns>Null if none.</returns>
        public static MapSocket RandomAwayFrom(MapSockets sockets, SocketContent content, Vector3 point,
            float minDistance, IRandomSource random)
        {
            var candidates = new List<MapSocket>();
            foreach (MapSocket socket in sockets.All)
            {
                if (Offers(sockets, socket, content, false)
                    && Flat(socket.transform.position - point).sqrMagnitude >= minDistance * minDistance)
                {
                    candidates.Add(socket);
                }
            }

            return candidates.Count == 0 ? null
                : candidates[Mathf.Min((int)(random.Value01() * candidates.Count), candidates.Count - 1)];
        }

        /// <summary>The sockets offering the content nearest to one, not counting it, nearest first.</summary>
        public static void Neighbors(MapSockets sockets, MapSocket from, SocketContent content, int count,
            List<MapSocket> results)
        {
            results.Clear();
            Vector3 origin = from.transform.position;
            foreach (MapSocket socket in sockets.All)
            {
                if (socket != from && Offers(sockets, socket, content, false))
                {
                    results.Add(socket);
                }
            }

            results.Sort((a, b) => Flat(a.transform.position - origin).sqrMagnitude
                .CompareTo(Flat(b.transform.position - origin).sqrMagnitude));
            if (results.Count > count)
            {
                results.RemoveRange(count, results.Count - count);
            }
        }

        private static bool Offers(MapSockets sockets, MapSocket socket, SocketContent content, bool freeOnly)
        {
            return socket != null && (!freeOnly || !sockets.IsTaken(socket))
                && SocketPicker.WeightFor(socket.Options, content) > 0f;
        }

        private static Vector3 Flat(Vector3 offset)
        {
            offset.y = 0f;
            return offset;
        }
    }
}
