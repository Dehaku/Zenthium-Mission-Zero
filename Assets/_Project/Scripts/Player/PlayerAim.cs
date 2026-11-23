using System;
using UnityEngine;

namespace Zenthium.Player
{
    /// <summary>
    /// The player's aim direction on the XZ plane (controls.md): toward the mouse, found by casting the pointer onto
    /// a flat plane at the player's height, or along the right stick. Whichever was used last wins, and the aim
    /// keeps its direction when the stick is let go.
    /// </summary>
    public sealed class PlayerAim : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;

        private bool isStickAiming;
        private bool hasPointer;
        private Vector2 lastPointer;

        /// <summary>Raised with the new direction when the aim turns.</summary>
        public event Action<Vector3> OnAimChanged;

        /// <summary>A flat unit vector. Starts facing +Z, screen-up.</summary>
        public Vector3 Direction { get; private set; } = Vector3.forward;

        /// <param name="pointerScreenPosition">The mouse position in screen pixels.</param>
        /// <param name="stick">The right stick, after the Input System's default deadzone.</param>
        public void UpdateAim(Vector2 pointerScreenPosition, Vector2 stick)
        {
            bool pointerMoved = hasPointer && pointerScreenPosition != lastPointer;
            lastPointer = pointerScreenPosition;
            hasPointer = true;

            if (stick.sqrMagnitude > 0f)
            {
                isStickAiming = true;
            }
            else if (pointerMoved)
            {
                isStickAiming = false;
            }

            if (isStickAiming)
            {
                Turn(new Vector3(stick.x, 0f, stick.y));
            }
            else if (TryGetPointerDirection(pointerScreenPosition, out Vector3 toPointer))
            {
                // Recomputed every frame, since the player moves under a still mouse.
                Turn(toPointer);
            }
        }

        private bool TryGetPointerDirection(Vector2 pointerScreenPosition, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (aimCamera == null)
            {
                return false;
            }

            // A math plane, not a physics raycast, so props and walls under the cursor don't bend the aim.
            Ray ray = aimCamera.ScreenPointToRay(pointerScreenPosition);
            var ground = new Plane(Vector3.up, transform.position);
            if (!ground.Raycast(ray, out float enter))
            {
                return false;
            }

            direction = ray.GetPoint(enter) - transform.position;
            direction.y = 0f;
            return true;
        }

        private void Turn(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            direction.Normalize();
            if (direction == Direction)
            {
                return;
            }

            Direction = direction;
            OnAimChanged?.Invoke(direction);
        }
    }
}
