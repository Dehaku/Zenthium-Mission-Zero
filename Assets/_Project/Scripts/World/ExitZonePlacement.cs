using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.World
{
    /// <summary>
    /// Where exit zones go (missions.md, Exits: marked zones on the map's edges): centered in the street that runs
    /// round the map's edge, spread evenly round it from a random start. Positions are on the XZ plane relative to
    /// the map's center: x is X, y is Z.
    /// </summary>
    public static class ExitZonePlacement
    {
        private const int SquareSides = 4;

        /// <param name="side">Meters across the map, its outer streets included.</param>
        /// <param name="streetWidth">Meters across the outer street.</param>
        /// <param name="startRoll">A number from 0 to 1: where round the map the first zone goes.</param>
        /// <param name="results">Replaced with one center per zone.</param>
        public static void Place(float side, float streetWidth, int count, float startRoll, List<Vector2> results)
        {
            results.Clear();
            if (count <= 0)
            {
                return;
            }

            // The outer street's middle line, as a square round the map's center.
            float half = Mathf.Max(0f, side - streetWidth) * 0.5f;
            float perimeter = SquareSides * 2f * half;
            for (int i = 0; i < count; i++)
            {
                float fraction = Mathf.Repeat(startRoll + (float)i / count, 1f);
                results.Add(PointOnSquare(half, fraction * perimeter));
            }
        }

        /// <summary>A point a distance round the square, counterclockwise seen from above, from its -X, -Z corner.</summary>
        private static Vector2 PointOnSquare(float half, float distance)
        {
            float edge = 2f * half;
            if (edge <= 0f)
            {
                return Vector2.zero;
            }

            int side = Mathf.Min((int)(distance / edge), SquareSides - 1);
            float along = distance - side * edge;
            switch (side)
            {
                case 0:
                    return new Vector2(-half + along, -half);
                case 1:
                    return new Vector2(half, -half + along);
                case 2:
                    return new Vector2(half - along, half);
                default:
                    return new Vector2(-half, half - along);
            }
        }
    }
}
