using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Entities;

namespace Zenthium.World
{
    /// <summary>
    /// A door (world.md, Doors). It sits on a trigger box around the doorway, with its panel (a
    /// <see cref="BuildingPart"/>) as a child. Unlocked, the panel opens while any entity stands in the box and closes
    /// when the box is empty. Locked, the panel stays solid until it's broken, by modules or knockback.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Door : MonoBehaviour
    {
        private static readonly Predicate<Rigidbody> IsGone = body => body == null || !body.gameObject.activeInHierarchy;

        [SerializeField] private BuildingPart panel;
        [SerializeField] private bool isLocked;

        // Bodies, not colliders, so an entity counts once however many colliders it has.
        private readonly HashSet<Rigidbody> visitors = new HashSet<Rigidbody>();
        private bool isOpen;

        public bool IsLocked => isLocked;
        public bool IsOpen => isOpen;
        public BuildingPart Panel => panel;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            if (panel == null)
            {
                Debug.LogError($"{name}: {nameof(Door)} needs its panel.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // Entities that are destroyed or switched off inside the box never send an exit.
            if (visitors.Count > 0 && visitors.RemoveWhere(IsGone) > 0)
            {
                Refresh();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body != null && body.TryGetComponent(out EntityBehaviour _) && visitors.Add(body))
            {
                Refresh();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body != null && visitors.Remove(body))
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            bool open = !isLocked && visitors.Count > 0;
            if (open == isOpen)
            {
                return;
            }

            isOpen = open;
            panel.SetSolid(!open);
        }
    }
}
