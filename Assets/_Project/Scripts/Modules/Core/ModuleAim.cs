using UnityEngine;

namespace Zenthium.Modules
{
    /// <summary>
    /// Where a module is aimed: a direction on the XZ plane, and how far away the aimed point is, for modules
    /// placed where you point (Glyph Trap, Smoke Bomb). Stick aiming has no point, so its distance is infinite and
    /// placed modules go to their full range.
    /// </summary>
    public readonly struct ModuleAim
    {
        public ModuleAim(Vector3 direction, float pointDistance = float.PositiveInfinity)
        {
            direction.y = 0f;
            Direction = direction;
            PointDistance = pointDistance;
        }

        public Vector3 Direction { get; }
        public float PointDistance { get; }

        /// <summary>The point along the aim, at the aimed point but no further than the range.</summary>
        public Vector3 PlacementPoint(Vector3 origin, float range)
        {
            Vector3 direction = Direction.sqrMagnitude > 0f ? Direction.normalized : Vector3.zero;
            return origin + direction * Mathf.Clamp(PointDistance, 0f, range);
        }
    }
}
