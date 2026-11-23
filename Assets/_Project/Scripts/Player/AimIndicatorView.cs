using UnityEngine;

namespace Zenthium.Player
{
    /// <summary>
    /// Turns a marker to show where the player aims. Presentation only.
    /// </summary>
    public sealed class AimIndicatorView : MonoBehaviour
    {
        [SerializeField] private PlayerAim aim;

        [Tooltip("Turned to face the aim. Its forward (+Z) is the pointing end.")]
        [SerializeField] private Transform pivot;

        private void OnEnable()
        {
            aim.OnAimChanged += Face;
            Face(aim.Direction);
        }

        private void OnDisable()
        {
            if (aim != null)
            {
                aim.OnAimChanged -= Face;
            }
        }

        private void Face(Vector3 direction)
        {
            pivot.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
