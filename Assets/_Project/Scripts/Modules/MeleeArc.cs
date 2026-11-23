using UnityEngine;

namespace Zenthium.Modules
{
    /// <summary>
    /// Whether a point is in front of an attacker on the XZ plane, within an arc around the aim direction.
    /// Range is checked separately, against colliders.
    /// </summary>
    public static class MeleeArc
    {
        /// <param name="halfAngleDegrees">Degrees either side of the aim direction.</param>
        public static bool Contains(Vector3 origin, Vector3 aimDirection, Vector3 point, float halfAngleDegrees)
        {
            Vector3 toPoint = point - origin;
            toPoint.y = 0f;
            aimDirection.y = 0f;

            // Standing inside the attacker counts as in front.
            if (toPoint.sqrMagnitude <= 0f || aimDirection.sqrMagnitude <= 0f)
            {
                return true;
            }

            return Vector3.Angle(aimDirection, toPoint) <= halfAngleDegrees;
        }
    }
}
