using System;
using System.Collections.Generic;

namespace Zenthium.World
{
    /// <summary>
    /// Which wall, window or door a fire tick destroys (world.md, Fire): one picked at random from those still
    /// standing, from the building's own list.
    /// </summary>
    public static class FireTargets
    {
        /// <param name="standing">One entry per part of the building: true if it's still standing.</param>
        /// <param name="roll">A random value from 0 (inclusive) to 1.</param>
        /// <returns>The index of the part to destroy, or -1 if none is standing.</returns>
        public static int PickStanding(IReadOnlyList<bool> standing, float roll)
        {
            int count = 0;
            foreach (bool isStanding in standing)
            {
                if (isStanding)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return -1;
            }

            int pick = Math.Min(count - 1, Math.Max(0, (int)(roll * count)));
            for (int i = 0; i < standing.Count; i++)
            {
                if (standing[i] && pick-- == 0)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
