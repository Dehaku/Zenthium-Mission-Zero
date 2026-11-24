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
                Collider hit = buffer[i].collider;
                if (!ModuleWorld.TryGetEntity(hit, out _) && !hit.TryGetComponent(out SeeThrough _))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
