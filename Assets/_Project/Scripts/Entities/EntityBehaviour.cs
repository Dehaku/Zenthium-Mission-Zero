using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Timing;

namespace Zenthium.Entities
{
    /// <summary>
    /// Puts an <see cref="Entities.Entity"/> in a scene and ticks it. Set either a hero or a faction entity.
    /// </summary>
    public sealed class EntityBehaviour : MonoBehaviour
    {
        private static readonly IRandomSource SharedRandom = new UnityRandomSource();

        [SerializeField] private GameDatabase database;
        [SerializeField] private GameClockDriver clock;

        [Tooltip("A preset hero. Leave empty to use a faction entity.")]
        [SerializeField] private HeroData hero;

        [Tooltip("A faction entity, used when no hero is set.")]
        [SerializeField] private EntityData factionEntity;

        [SerializeField] private Team team = Team.Enemy;

        [Tooltip("Mission ranks above D: 0 at D, 4 at S, 5 at S2. Only faction entities use it.")]
        [SerializeField, Min(0)] private int difficultyLevel;

        [Tooltip("Stand-in for module slots until Milestone 4: these only grant resources for now.")]
        [SerializeField] private List<ModuleData> modules = new List<ModuleData>();

        /// <summary>Null until Awake, or if the setup is invalid.</summary>
        public Entity Entity { get; private set; }

        /// <summary>Raised when the entity is created or replaced by <see cref="ResetEntity"/>.</summary>
        public event Action<Entity> OnEntityCreated;

        /// <summary>Replaces the entity with a fresh one built from the same setup.</summary>
        public void ResetEntity()
        {
            CreateEntity();
        }

        private void Awake()
        {
            CreateEntity();
        }

        private void Update()
        {
            if (Entity == null)
            {
                return;
            }

            // Clamped like Time.deltaTime, so a hitch or a breakpoint doesn't become one huge step.
            Entity.Tick(clock.Clock, Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime));
        }

        private void CreateEntity()
        {
            if (database == null || clock == null)
            {
                Debug.LogError($"{name}: {nameof(EntityBehaviour)} needs a Game Database and a Game Clock Driver.", this);
                enabled = false;
                return;
            }

            EntitySetup setup;
            if (hero != null)
            {
                setup = EntitySetup.ForHero(hero, team);
            }
            else if (factionEntity != null)
            {
                setup = EntitySetup.ForFactionEntity(factionEntity, team, difficultyLevel, database.EntityConstants);
            }
            else
            {
                Debug.LogError($"{name}: {nameof(EntityBehaviour)} needs a hero or a faction entity.", this);
                enabled = false;
                return;
            }

            foreach (ModuleData module in modules)
            {
                if (module != null)
                {
                    setup.Modules.Add(module);
                }
            }

            Entity = new Entity(setup, EntityRules.From(database), SharedRandom);
            OnEntityCreated?.Invoke(Entity);
        }
    }
}
