using UnityEngine;

namespace Zenthium.Player
{
    /// <summary>
    /// Keeps an orthographic camera looking straight down at the XZ gameplay plane, following a target, and zooms
    /// between a closest and a farthest view (<see cref="Zoom"/>).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OverheadCameraRig : MonoBehaviour
    {
        // Straight down the Y axis with screen-up along +Z, so a 2D input (x, y) maps directly to world (x, z).
        private static readonly Quaternion LookStraightDown = Quaternion.Euler(90f, 0f, 0f);

        [SerializeField] private Transform target;
        [SerializeField, Min(1f)] private float height = 30f;
        [SerializeField, Min(0.1f)] private float orthographicSize = 12f;
        [SerializeField, Min(0f)] private float followSmoothTime = 0.1f;

        [Tooltip("The closest zoom: half the view's height, in meters.")]
        [SerializeField, Min(0.1f)] private float minOrthographicSize = 6f;

        [Tooltip("The farthest zoom: half the view's height, in meters.")]
        [SerializeField, Min(0.1f)] private float maxOrthographicSize = 32f;

        [Tooltip("The share of the view each zoom step adds or takes away.")]
        [SerializeField, Range(0.01f, 0.5f)] private float zoomStep = 0.15f;

        [Tooltip("Real seconds the view takes to settle on a new zoom.")]
        [SerializeField, Min(0f)] private float zoomSmoothTime = 0.08f;

        private Camera cam;
        private Vector3 followVelocity;
        private float targetSize;
        private float zoomVelocity;

        /// <summary>Zooms by whole steps: positive zooms in, negative zooms out.</summary>
        public void Zoom(int steps)
        {
            float factor = Mathf.Pow(1f - zoomStep, steps);
            targetSize = Mathf.Clamp(targetSize * factor, minOrthographicSize, maxOrthographicSize);
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            targetSize = orthographicSize;
            ApplyLens(cam, orthographicSize);
            transform.rotation = LookStraightDown;
            if (target != null)
            {
                transform.position = PositionAbove(target.position);
            }
        }

        private void OnValidate()
        {
            if (TryGetComponent(out Camera lens))
            {
                ApplyLens(lens, orthographicSize);
            }
        }

        private void LateUpdate()
        {
            // Real time, so zooming still works while game time is slowed, stopped or paused.
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity,
                zoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            if (target == null)
            {
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position, PositionAbove(target.position), ref followVelocity, followSmoothTime);
        }

        private static void ApplyLens(Camera lens, float size)
        {
            lens.orthographic = true;
            lens.orthographicSize = size;
        }

        private Vector3 PositionAbove(Vector3 point)
        {
            return new Vector3(point.x, point.y + height, point.z);
        }
    }
}
