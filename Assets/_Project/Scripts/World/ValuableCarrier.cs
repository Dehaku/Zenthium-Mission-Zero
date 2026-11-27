using System;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Statuses;

namespace Zenthium.World
{
    /// <summary>
    /// The Valuables an entity holds (world.md, Carrying), the player included:
    /// <list type="bullet">
    /// <item>Carrying Treasure: one layer per Valuable, each a share of slow, up to the cap.</item>
    /// <item>Defeated, it drops them all around itself (presentation shows a "KA-CHING!" from
    /// <see cref="OnDroppedAll"/>).</item>
    /// <item>The player's Kick launches them all when no prop is dragged (<see cref="ICarriedLoot"/>).</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour))]
    public sealed class ValuableCarrier : MonoBehaviour, ICarriedLoot
    {
        [SerializeField] private ValuableService service;

        [Tooltip("Testing: Valuables it holds from the start, such as a sandbox dummy's.")]
        [SerializeField, Min(0)] private int startingValuables;

        private EntityBehaviour entityBehaviour;
        private Entity entity;
        private StatusModifier[] slowPerLayer;
        private bool hasStartingValuables;

        /// <summary>Raised when the number held changes.</summary>
        public event Action OnCountChanged;

        /// <summary>Raised when it drops what it held: defeated, or a citizen starting to flee.</summary>
        public event Action OnDroppedAll;

        public int Count { get; private set; }

        /// <summary>Null if the scene has no Valuable service; then it can still pick up but never drops.</summary>
        public ValuableService Service => service;

        /// <summary>For entities placed in code: set before it's used.</summary>
        public void SetService(ValuableService newService)
        {
            service = newService;
        }

        public void Add(int amount)
        {
            if (amount > 0)
            {
                SetCount(Count + amount);
            }
        }

        /// <summary>Hands everything it holds to another carrier, such as a goon to its Leader or its charmer.</summary>
        public void GiveAllTo(ValuableCarrier receiver)
        {
            if (receiver == null || receiver == this || Count == 0)
            {
                return;
            }

            receiver.Add(Count);
            SetCount(0);
        }

        /// <summary>Drops everything it holds around itself, such as a Scared citizen starting to flee.</summary>
        public void DropAll()
        {
            if (Count == 0 || service == null)
            {
                return;
            }

            service.DropAround(transform.position, Count);
            SetCount(0);
            OnDroppedAll?.Invoke();
        }

        public bool TryLaunchAll(in ThrowInfo info)
        {
            if (entity == null || entity.Team != Team.Player || Count == 0 || service == null)
            {
                return false;
            }

            service.Launch(info, Count);
            SetCount(0);
            return true;
        }

        private void Awake()
        {
            entityBehaviour = GetComponent<EntityBehaviour>();
        }

        private void OnEnable()
        {
            // The entity may run Awake before or after this, so bind now and on every (re)creation.
            entityBehaviour.OnEntityCreated += Bind;
            if (entityBehaviour.Entity != null)
            {
                Bind(entityBehaviour.Entity);
            }
        }

        private void OnDisable()
        {
            entityBehaviour.OnEntityCreated -= Bind;
            Unbind();
        }

        private void Bind(Entity newEntity)
        {
            Unbind();
            entity = newEntity;
            entity.OnIncapacitated += DropAll;
            entity.OnEscaped += LoseAll;
            if (!hasStartingValuables)
            {
                hasStartingValuables = true;
                Count = startingValuables;
            }

            // A fresh entity has no statuses yet, so it needs the slow for what's already held.
            ApplyCarryingTreasure();
            OnCountChanged?.Invoke();
        }

        private void Unbind()
        {
            if (entity != null)
            {
                entity.OnIncapacitated -= DropAll;
                entity.OnEscaped -= LoseAll;
                entity = null;
            }
        }

        /// <summary>Carried off the map: they count as lost (world.md, Valuables secondary goal).</summary>
        private void LoseAll()
        {
            if (Count == 0)
            {
                return;
            }

            if (service != null)
            {
                service.CountLost(Count);
            }

            SetCount(0);
        }

        private void SetCount(int newCount)
        {
            Count = newCount;
            ApplyCarryingTreasure();
            OnCountChanged?.Invoke();
        }

        private void ApplyCarryingTreasure()
        {
            if (entity == null || service == null || service.World == null)
            {
                return;
            }

            ValuableConstants constants = service.World.Database.ValuableConstants;
            if (slowPerLayer == null)
            {
                slowPerLayer = new[] { new StatusModifier(StatusModifierTarget.Slow, constants.SlowPerValuable) };
            }

            var application = new StatusApplication(StatusType.CarryingTreasure, null, StatusApplication.UntilRemoved,
                slowPerLayer, ValuableRules.MaxCarryingTreasureLayers(constants));
            entity.Statuses.SetLayers(application, ValuableRules.CarryingTreasureLayers(Count, constants));
        }
    }
}
