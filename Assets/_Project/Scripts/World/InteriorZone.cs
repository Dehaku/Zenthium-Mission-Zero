using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Entities;

namespace Zenthium.World
{
    /// <summary>
    /// A designer-drawn box that is "inside" its building (world.md, Buildings). It keeps a live list of the
    /// entities and props inside, updated as they enter and leave, so fire and loot rules never work it out from
    /// geometry. An L-shaped building uses two.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class InteriorZone : MonoBehaviour
    {
        private static readonly Predicate<EntityBehaviour> IsEntityGone =
            entity => entity == null || !entity.isActiveAndEnabled;
        private static readonly Predicate<PropBody> IsPropGone = prop => prop == null || !prop.isActiveAndEnabled;

        private readonly HashSet<EntityBehaviour> entities = new HashSet<EntityBehaviour>();
        private readonly HashSet<PropBody> props = new HashSet<PropBody>();
        private BoxCollider box;

        /// <summary>Its floor area on the XZ plane: x is world X, y is world Z.</summary>
        public Rect Footprint
        {
            get
            {
                Bounds bounds = box.bounds;
                return Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            }
        }

        /// <summary>Replaces the list's contents with the entities inside now.</summary>
        public void CollectEntities(List<EntityBehaviour> results)
        {
            entities.RemoveWhere(IsEntityGone);
            results.Clear();
            results.AddRange(entities);
        }

        /// <summary>Replaces the list's contents with the props inside now.</summary>
        public void CollectProps(List<PropBody> results)
        {
            props.RemoveWhere(IsPropGone);
            results.Clear();
            results.AddRange(props);
        }

        private void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null)
            {
                return;
            }

            if (body.TryGetComponent(out EntityBehaviour entity))
            {
                entities.Add(entity);
            }
            else if (body.TryGetComponent(out PropBody prop))
            {
                props.Add(prop);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null)
            {
                return;
            }

            if (body.TryGetComponent(out EntityBehaviour entity))
            {
                entities.Remove(entity);
            }
            else if (body.TryGetComponent(out PropBody prop))
            {
                props.Remove(prop);
            }
        }
    }
}
