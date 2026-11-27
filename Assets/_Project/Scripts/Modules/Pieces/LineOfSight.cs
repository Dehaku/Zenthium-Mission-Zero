using UnityEngine;

namespace Zenthium.Modules
{
    /// <summary>
    /// Whether one point can see another (stats.md, world.md): walls, doors and props block sight; entities,
    /// triggers and see-through structures such as windows don't. Broken walls and open doors have their colliders
    /// off, so they never block.
    /// </summary>
    public static class LineOfSight
    {
        /// <param name="buffer">Reused for the raycast, so a check allocates nothing.</param>
        public static bool IsClear(Vector3 from, Vector3 to, RaycastHit[] buffer)
        {
            Vector3 toTarget = to - from;
            float distance = toTarget.magnitude;
            if (distance <= 0f)
            {
                return true;
            }

            int count = Physics.RaycastNonAlloc(from, toTarget / distance, buffer, distance, Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (BlocksSight(buffer[i].collider))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>How far sight reaches from a point in a direction, by the same rule as <see cref="IsClear"/>.</summary>
        /// <param name="direction">Normalized.</param>
        /// <param name="buffer">Reused for the raycast, so a check allocates nothing.</param>
        /// <returns>The distance to the first thing that blocks sight, or the maximum if nothing does.</returns>
        public static float ClearDistance(Vector3 from, Vector3 direction, float maxDistance, RaycastHit[] buffer)
        {
            int count = Physics.RaycastNonAlloc(from, direction, buffer, maxDistance, Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            // The hits come unsorted.
            float nearest = maxDistance;
            for (int i = 0; i < count; i++)
            {
                if (buffer[i].distance < nearest && BlocksSight(buffer[i].collider))
                {
                    nearest = buffer[i].distance;
                }
            }

            return nearest;
        }

        private static bool BlocksSight(Collider hit)
        {
            return !ModuleWorld.TryGetEntity(hit, out _) && !hit.TryGetComponent(out SeeThrough _);
        }
    }
}
