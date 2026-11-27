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

        [Tooltip("A preset hero, with its kit. Leave empty to use a faction entity.")]
        [SerializeField] private HeroData hero;

        [Tooltip("A faction entity, used when no hero is set.")]
        [SerializeField] private EntityData factionEntity;

        [SerializeField] private Team team = Team.Enemy;

        [Tooltip("Mission ranks above D: 0 at D, 4 at S, 5 at S2. Only faction entities use it.")]
        [SerializeField, Min(0)] private int difficultyLevel;

        [Tooltip("Testing: replaces a faction entity's module list from entities.csv, each in the next slot of its type. "
            + "Empty uses the data's. Heroes use their kit.")]
        [SerializeField] private List<ModuleData> modules = new List<ModuleData>();

        [Tooltip("Entity tags added to the ones from entities.csv and factions.csv, such as Immobile for a Heavy Turret.")]
        [SerializeField] private List<EntityTag> tags = new List<EntityTag>();

        [Tooltip("Testing: slots of each type unlocked for a hero. 0 uses the preset's starting slots.")]
        [SerializeField, Min(0)] private int unlockedSlots;

        [Tooltip("Testing: replaces the module in the last slot of its type, or fills the first empty one.")]
        [SerializeField] private ModuleData testModule;

        private Entity summoner;

        /// <summary>Null until Awake, or if the setup is invalid.</summary>
        public Entity Entity { get; private set; }

        /// <summary>The scene's clock, which this entity's other components run on.</summary>
        public GameClockDriver ClockDriver => clock;

        /// <summary>The faction entity it's built from; null for a hero.</summary>
        public EntityData FactionEntity => hero == null ? factionEntity : null;

        /// <summary>Mission ranks above D it's built for (0 at D).</summary>
        public int DifficultyLevel => difficultyLevel;

        /// <summary>Raised when the entity is created or replaced by <see cref="ResetEntity"/>.</summary>
        public event Action<Entity> OnEntityCreated;

        /// <summary>Replaces the entity with a fresh one built from the same setup.</summary>
        public void ResetEntity()
        {
            CreateEntity();
        }

        /// <summary>Switches to another hero and rebuilds the entity (the sandbox's hero select).</summary>
        /// <param name="newUnlockedSlots">Slots of each type unlocked; 0 uses the preset's starting slots.</param>
        public void SetHero(HeroData newHero, int newUnlockedSlots, ModuleData newTestModule)
        {
            hero = newHero;
            unlockedSlots = newUnlockedSlots;
            testModule = newTestModule;
            CreateEntity();
        }

        /// <summary>
        /// Sets up a summoned faction entity in code, since a spawned prefab can't hold scene references. Call it before
        /// the object activates (spawn it under an inactive parent), so Awake builds the entity from this.
        /// </summary>
        public void ConfigureSummon(GameDatabase newDatabase, GameClockDriver newClock, EntityData newEntity,
            Team newTeam, Entity newSummoner, IReadOnlyList<ModuleData> newModules)
        {
            database = newDatabase;
            clock = newClock;
            hero = null;
            factionEntity = newEntity;
            team = newTeam;
            summoner = newSummoner;
            modules.Clear();
            modules.AddRange(newModules);
        }

        /// <summary>
        /// Sets up a spawned faction entity in code, with its modules and tags from the data, such as backup from
        /// Call for Backup. Call it before the object activates, as with <see cref="ConfigureSummon"/>.
        /// </summary>
        public void ConfigureFactionEntity(GameDatabase newDatabase, GameClockDriver newClock, EntityData newEntity,
            Team newTeam, int newDifficultyLevel)
        {
            database = newDatabase;
            clock = newClock;
            hero = null;
            factionEntity = newEntity;
            team = newTeam;
            difficultyLevel = Mathf.Max(0, newDifficultyLevel);
            summoner = null;
            modules.Clear();
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
                int unlocked = unlockedSlots > 0 ? unlockedSlots : database.ModuleConstants.PresetSlotsUnlocked;
                setup = EntitySetup.ForHero(hero, team, unlocked);
            }
            else if (factionEntity != null)
            {
                setup = EntitySetup.ForFactionEntity(factionEntity, team, difficultyLevel, database.EntityConstants);
                setup.Summoner = summoner;

                // A list set here replaces the data's, such as a sandbox goon's test kit or a summon's attack.
                if (modules.Count > 0)
                {
                    setup.Modules.Clear();
                    foreach (ModuleData module in modules)
                    {
                        if (module != null)
                        {
                            setup.Modules.Add(module);
                        }
                    }

                    setup.MinimumSlotsPerType = EntitySetup.MostOfOneType(setup.Modules);
                }
            }
            else
            {
                Debug.LogError($"{name}: {nameof(EntityBehaviour)} needs a hero or a faction entity.", this);
                enabled = false;
                return;
            }

            setup.Tags.AddRange(tags);
            AddTestModule(setup);

            try
            {
                Entity = new Entity(setup, EntityRules.From(database), SharedRandom);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"{name}: {exception.Message}", this);
                enabled = false;
                return;
            }

            enabled = true;
            OnEntityCreated?.Invoke(Entity);
        }

        private void AddTestModule(EntitySetup setup)
        {
            if (testModule == null || setup.Modules.Contains(testModule)
                || !testModule.TryGetSlotType(out SlotType testType))
            {
                return;
            }

            int count = 0;
            int last = -1;
            for (int i = 0; i < setup.Modules.Count; i++)
            {
                if (setup.Modules[i] != null && setup.Modules[i].TryGetSlotType(out SlotType type) && type == testType)
                {
                    count++;
                    last = i;
                }
            }

            if (count >= database.ModuleConstants.SlotsPerType)
            {
                setup.Modules[last] = testModule;
            }
            else
            {
                setup.Modules.Add(testModule);
            }
        }
    }
}
