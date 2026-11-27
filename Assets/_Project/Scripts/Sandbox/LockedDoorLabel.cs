using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zenthium.Entities;
using Zenthium.World;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Shows "LOCKED" over a locked door while the player's side is near it, until the door breaks. The text lies
    /// flat facing the overhead camera, whichever way the block is turned. Presentation only.
    /// </summary>
    public sealed class LockedDoorLabel : MonoBehaviour
    {
        // The overhead camera looks straight down, so flat text faces it lying on its back.
        private static readonly Quaternion FacingCamera = Quaternion.Euler(90f, 0f, 0f);

        [SerializeField] private Door door;
        [SerializeField] private TMP_Text label;

        [Tooltip("Meters from the door within which the player sees the label.")]
        [SerializeField, Min(0f)] private float showDistance = 10f;

        [Tooltip("Real seconds between checks for the player nearby.")]
        [SerializeField, Min(0.02f)] private float checkSeconds = 0.2f;

        private readonly List<EntityBehaviour> nearby = new List<EntityBehaviour>();
        private float untilCheck;

        private void Awake()
        {
            if (door == null || label == null)
            {
                Debug.LogError($"{name}: {nameof(LockedDoorLabel)} needs its door and label.", this);
                enabled = false;
                return;
            }

            label.enabled = false;
        }

        private void Update()
        {
            untilCheck -= Time.unscaledDeltaTime;
            if (untilCheck > 0f)
            {
                return;
            }

            untilCheck = checkSeconds;
            bool show = door.IsLocked && door.Panel != null && !door.Panel.IsBroken && IsPlayerNear();
            label.enabled = show;
            if (show)
            {
                label.transform.rotation = FacingCamera;
            }
        }

        private bool IsPlayerNear()
        {
            Structure structure = door.Panel.Structure;
            if (structure == null || structure.World == null)
            {
                return false;
            }

            structure.World.CollectEntities(transform.position, showDistance, nearby);
            foreach (EntityBehaviour entity in nearby)
            {
                if (entity != null && entity.Entity != null && entity.Entity.Team == Team.Player)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
