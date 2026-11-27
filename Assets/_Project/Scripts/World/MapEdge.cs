using UnityEngine;

namespace Zenthium.World
{
    /// <summary>The map's edge as plain geometry on the XZ plane, with X and Y of the vectors standing for X and Z.</summary>
    public static class MapEdge
    {
        /// <summary>The point on the area's edge nearest a position; for a position outside it, the nearest point.</summary>
        public static Vector2 NearestPoint(Rect area, Vector2 position)
        {
            if (!area.Contains(position))
            {
                return new Vector2(Mathf.Clamp(position.x, area.xMin, area.xMax),
                    Mathf.Clamp(position.y, area.yMin, area.yMax));
            }

            float toLeft = position.x - area.xMin;
            float toRight = area.xMax - position.x;
            float toBottom = position.y - area.yMin;
            float toTop = area.yMax - position.y;
            float nearest = Mathf.Min(Mathf.Min(toLeft, toRight), Mathf.Min(toBottom, toTop));
            if (nearest == toLeft)
            {
                return new Vector2(area.xMin, position.y);
            }

            if (nearest == toRight)
            {
                return new Vector2(area.xMax, position.y);
            }

            return nearest == toBottom ? new Vector2(position.x, area.yMin) : new Vector2(position.x, area.yMax);
        }

        /// <summary>Meters from a position inside the area to its nearest edge; 0 on or outside the edge.</summary>
        public static float DistanceToEdge(Rect area, Vector2 position)
        {
            if (!area.Contains(position))
            {
                return 0f;
            }

            return Mathf.Min(Mathf.Min(position.x - area.xMin, area.xMax - position.x),
                Mathf.Min(position.y - area.yMin, area.yMax - position.y));
        }
    }
}
