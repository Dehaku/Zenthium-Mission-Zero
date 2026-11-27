using UnityEngine;
using Zenthium.Core.Controls;

namespace Zenthium.Player
{
    /// <summary>
    /// Alt + mouse wheel zooms the overhead camera: one step per frame the wheel turns, so it works the same
    /// whatever size the platform reports for a wheel notch.
    /// </summary>
    [RequireComponent(typeof(OverheadCameraRig))]
    public sealed class CameraZoomInput : MonoBehaviour
    {
        private OverheadCameraRig rig;
        private InputSystem_Actions actions;

        private void Awake()
        {
            rig = GetComponent<OverheadCameraRig>();
            actions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            actions.View.Enable();
        }

        private void OnDisable()
        {
            actions.View.Disable();
        }

        private void OnDestroy()
        {
            actions.Dispose();
        }

        private void Update()
        {
            float wheel = actions.View.Zoom.ReadValue<float>();
            if (wheel != 0f)
            {
                // Wheel up (away from you) zooms in.
                rig.Zoom(wheel > 0f ? 1 : -1);
            }
        }
    }
}
