using System.Collections.Generic;
using UnityEngine;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.World;

namespace Zenthium.AI
{
    /// <summary>
    /// An AI entity's looting (world.md, Picking up): finds the nearest loose Valuable it can see, and picks one up
    /// over the pickup time, which any damage taken starts over. <see cref="AiBrain"/> decides when to loot and walks
    /// it there.
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour), typeof(EntityModules), typeof(ValuableCarrier))]
    public sealed class AiLooter : MonoBehaviour
    {
        private EntityBehaviour self;
        private EntityModules modules;
        private Entity entity;
        private ValuablePickupTimer timer;
        private ValuableBag bag;

        public ValuableCarrier Carrier { get; private set; }

        /// <summary>From 0 to 1; 0 when it isn't picking anything up.</summary>
        public float PickupProgress => timer != null && timer.IsRunning ? timer.Progress : 0f;

        /// <summary>The nearest loose, landed Valuable it can see within range; null if none.</summary>
        /// <param name="sightHits">Reused for the sight raycasts.</param>
        internal ValuableBag FindVisibleBag(Vector3 eye, float range, RaycastHit[] sightHits)
        {
            ValuableService service = Carrier.Service;
            if (service == null || modules.World == null)
            {
                return null;
            }

            ValuableBag nearest = null;
            float nearestDistance = range;
            IReadOnlyList<ValuableBag> bags = service.Bags;
            for (int i = 0; i < bags.Count; i++)
            {
                ValuableBag candidate = bags[i];
                if (candidate == null || candidate.IsFlying)
                {
                    continue;
                }

                Vector3 position = candidate.transform.position;
                float x = position.x - eye.x;
                float z = position.z - eye.z;
                float distance = Mathf.Sqrt(x * x + z * z);

                // Nearest first, so a sight raycast is only spent on a bag that would be the new nearest.
                if (distance > nearestDistance || !modules.World.CanSee(eye, position, sightHits))
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            return nearest;
        }

        /// <summary>Starts picking up the bag, or keeps going if it already is.</summary>
        internal void PickUp(ValuableBag target)
        {
            if (timer == null || (target == bag && timer.IsRunning))
            {
                return;
            }

            bag = target;
            timer.Start();
        }

        internal void Cancel()
        {
            bag = null;
            if (timer != null)
            {
                timer.Cancel();
            }
        }

        /// <summary>Runs the pickup on game time; the bag is taken when it completes.</summary>
        internal void Advance(float gameDelta)
        {
            if (timer == null || !timer.IsRunning)
            {
                return;
            }

            // Someone else picked it up first.
            if (bag == null)
            {
                Cancel();
                return;
            }

            if (timer.Advance(gameDelta))
            {
                ValuableBag taken = bag;
                bag = null;
                taken.PickUp(self);
            }
        }

        private void Awake()
        {
            self = GetComponent<EntityBehaviour>();
            modules = GetComponent<EntityModules>();
            Carrier = GetComponent<ValuableCarrier>();
        }

        private void OnEnable()
        {
            // The entity may be created before or after this, and again on reset, as in AiBrain.
            self.OnEntityCreated += Bind;
            if (self.Entity != null)
            {
                Bind(self.Entity);
            }
        }

        private void OnDisable()
        {
            self.OnEntityCreated -= Bind;
            Unbind();
        }

        private void Bind(Entity newEntity)
        {
            Unbind();
            if (modules.World == null)
            {
                return;
            }

            entity = newEntity;
            entity.OnDamaged += HandleDamaged;
            timer = new ValuablePickupTimer(modules.World.Database.ValuableConstants.PickupSeconds);
        }

        private void Unbind()
        {
            Cancel();
            if (entity != null)
            {
                entity.OnDamaged -= HandleDamaged;
                entity = null;
            }
        }

        private void HandleDamaged(DamageEvent damage)
        {
            if (damage.Outcome == DamageOutcome.Dealt)
            {
                timer.OnDamaged();
            }
        }
    }
}
