using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Zenthium.Entities;

namespace Zenthium.World
{
    /// <summary>
    /// A designer-drawn box that is "inside" its building (world.md, Buildings). It keeps a live list of the
    /// entities, props and loose Valuables inside, updated as they enter and leave, so fire and loot rules never work
    /// it out from geometry. An L-shaped building uses two. While its building burns, its NavMesh obstacle (if it has
    /// one) carves its floor out, so enemies never path through a burning building.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class InteriorZone : MonoBehaviour
    {
        private static readonly Predicate<EntityBehaviour> IsEntityGone =
            entity => entity == null || !entity.isActiveAndEnabled;
        private static readonly Predicate<PropBody> IsPropGone = prop => prop == null || !prop.isActiveAndEnabled;
        private static readonly Predicate<ValuableBag> IsBagGone = bag => bag == null || !bag.isActiveAndEnabled;

        private readonly HashSet<EntityBehaviour> entities = new HashSet<EntityBehaviour>();
        private readonly HashSet<PropBody> props = new HashSet<PropBody>();
        private readonly HashSet<ValuableBag> bags = new HashSet<ValuableBag>();
        private BoxCollider box;
        private NavMeshObstacle obstacle;

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

        /// <summary>Replaces the list's contents with the loose Valuables inside now.</summary>
        public void CollectBags(List<ValuableBag> results)
        {
            bags.RemoveWhere(IsBagGone);
            results.Clear();
            results.AddRange(bags);
        }

        /// <summary>Carves its floor out of the NavMesh, or puts it back.</summary>
        public void SetBlocksPaths(bool blocks)
        {
            if (obstacle != null)
            {
                obstacle.enabled = blocks;
            }
        }

        private void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            if (TryGetComponent(out obstacle))
            {
                obstacle.enabled = false;
            }
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
            else if (body.TryGetComponent(out ValuableBag bag))
            {
                bags.Add(bag);
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
            else if (body.TryGetComponent(out ValuableBag bag))
            {
                bags.Remove(bag);
            }
        }
    }
}
