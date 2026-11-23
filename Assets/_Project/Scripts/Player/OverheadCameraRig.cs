using UnityEngine;

namespace Zenthium.Player
{
    /// <summary>
    /// Keeps an orthographic camera looking straight down at the XZ gameplay plane, following a target.
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

        private Vector3 followVelocity;

        private void Awake()
        {
            ApplyLens(GetComponent<Camera>());
            transform.rotation = LookStraightDown;
            if (target != null)
            {
                transform.position = PositionAbove(target.position);
            }
        }

        private void OnValidate()
        {
            if (TryGetComponent(out Camera cam))
            {
                ApplyLens(cam);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position, PositionAbove(target.position), ref followVelocity, followSmoothTime);
        }

        private void ApplyLens(Camera cam)
        {
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }

        private Vector3 PositionAbove(Vector3 point)
        {
            return new Vector3(point.x, point.y + height, point.z);
        }
    }
}
