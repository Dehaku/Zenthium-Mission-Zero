using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.World
{
    /// <summary>
    /// Which buildings are neighbors (world.md, Buildings): those whose floor areas come within a radius of each other,
    /// measured edge to edge between their interior zones, so a long building's far end still counts. Worked out
    /// once when the map is assembled; fire spreading reads the lists, never searching at runtime.
    /// </summary>
    public static class BuildingNeighbors
    {
        /// <summary>The gap between two rectangles on the ground; 0 if they touch or overlap.</summary>
        public static float Distance(Rect a, Rect b)
        {
            float dx = Math.Max(0f, Math.Max(a.xMin - b.xMax, b.xMin - a.xMax));
            float dy = Math.Max(0f, Math.Max(a.yMin - b.yMax, b.yMin - a.yMax));
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <param name="footprints">Each building's interior zone rectangles, by building index.</param>
        /// <param name="results">Filled with one list per building: the indices of its neighbors, in index order.</param>
        public static void Compute(IReadOnlyList<IReadOnlyList<Rect>> footprints, float radius, List<List<int>> results)
        {
            results.Clear();
            for (int i = 0; i < footprints.Count; i++)
            {
                results.Add(new List<int>());
            }

            for (int i = 0; i < footprints.Count; i++)
            {
                for (int j = i + 1; j < footprints.Count; j++)
                {
                    if (AreWithin(footprints[i], footprints[j], radius))
                    {
                        results[i].Add(j);
                        results[j].Add(i);
                    }
                }
            }
        }

        private static bool AreWithin(IReadOnlyList<Rect> a, IReadOnlyList<Rect> b, float radius)
        {
            foreach (Rect first in a)
            {
                foreach (Rect second in b)
                {
                    if (Distance(first, second) <= radius)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
