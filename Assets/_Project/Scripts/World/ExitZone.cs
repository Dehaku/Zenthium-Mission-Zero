using UnityEngine;

namespace Zenthium.World
{
    /// <summary>
    /// A marked zone on the map's edge (missions.md, Exits). Its box is 1 m across at scale 1, so the map builder
    /// scales it to the exit zone size. Capturing and extraction use it from Milestone 9.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ExitZone : MonoBehaviour
    {
        private BoxCollider box;

        private BoxCollider Box
        {
            get
            {
                if (box == null)
                {
                    box = GetComponent<BoxCollider>();
                }

                return box;
            }
        }

        /// <param name="size">Meters across the square zone.</param>
        public void Configure(float size)
        {
            Vector3 scale = transform.localScale;
            transform.localScale = new Vector3(size, scale.y, size);
        }

        /// <summary>Whether the point is over the zone, at any height.</summary>
        public bool Contains(Vector3 point)
        {
            Bounds bounds = Box.bounds;
            return point.x >= bounds.min.x && point.x <= bounds.max.x && point.z >= bounds.min.z
                && point.z <= bounds.max.z;
        }

        private void Awake()
        {
            Box.isTrigger = true;
        }
    }
}
