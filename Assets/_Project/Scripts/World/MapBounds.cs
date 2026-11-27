using UnityEngine;

namespace Zenthium.World
{
    /// <summary>
    /// The map's playable area, a rectangle on the XZ plane centered on this object. Fleeing cowards and goons
    /// carrying Valuables off the map head for its nearest edge and escape once they're within the edge margin
    /// (enemies.md, Limits; missions.md: fleeing enemies can leave from any edge). The map builder sizes it from
    /// Milestone 8.
    /// </summary>
    public sealed class MapBounds : MonoBehaviour
    {
        [Tooltip("Meters across the map along X and Z.")]
        [SerializeField] private Vector2 size = new Vector2(60f, 60f);

        [Tooltip("Meters from the edge that count as leaving the map. Walls along the edge keep entities a little inside it.")]
        [SerializeField, Min(0f)] private float edgeMargin = 2.5f;

        private Rect Area
        {
            get
            {
                Vector3 center = transform.position;
                return new Rect(center.x - size.x * 0.5f, center.z - size.y * 0.5f, size.x, size.y);
            }
        }

        /// <summary>The nearest point on the map's edge, at the position's height.</summary>
        public Vector3 NearestEdgePoint(Vector3 position)
        {
            Vector2 point = MapEdge.NearestPoint(Area, new Vector2(position.x, position.z));
            return new Vector3(point.x, position.y, point.y);
        }

        /// <summary>Close enough to the edge to leave the map.</summary>
        public bool IsAtEdge(Vector3 position)
        {
            return MapEdge.DistanceToEdge(Area, new Vector2(position.x, position.z)) <= edgeMargin;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0f, size.y));
        }
    }
}
