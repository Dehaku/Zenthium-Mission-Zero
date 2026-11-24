using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Movement;

namespace Zenthium.Modules
{
    /// <summary>
    /// Runs an entity's modules: Kick, always available, and a behavior for each equipped slot. It ticks their
    /// cooldowns in real seconds and their timers on the entity's game time, passes the entity's trigger events to
    /// them, and switches a module off while its slot is disabled. Players and AI use modules through it.
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour), typeof(Rigidbody))]
    public sealed class EntityModules : MonoBehaviour
    {
        private static readonly int SlotTypeCount = Enum.GetValues(typeof(SlotType)).Length;

        [SerializeField] private ModuleWorld world;

        [Tooltip("Degrees either side of the aim direction that melee modules reach. Their range is the module's range tier.")]
        [SerializeField, Range(0f, 180f)] private float meleeArcHalfAngle = 60f;

        private readonly List<ModuleBehaviour> all = new List<ModuleBehaviour>();
        private readonly HashSet<string> reportedGaps = new HashSet<string>(StringComparer.Ordinal);
        private ModuleBehaviour[] slotted = new ModuleBehaviour[0];
        private EntityBehaviour entityBehaviour;
        private Rigidbody body;
        private Entity entity;
        private ModuleContext context;
        private ModuleAim aim = new ModuleAim(Vector3.forward);

        /// <summary>Null until the entity exists.</summary>
        public Entity Entity => entity;

        public ModuleWorld World => world;

        /// <summary>Null if the game data has no Kick.</summary>
        public ModuleBehaviour Kick { get; private set; }

        /// <summary>Null if the slot is empty, locked, or its module has no behavior yet.</summary>
        public ModuleBehaviour GetSlotted(SlotType type, int index)
        {
            int i = SlotIndex(type, index);
            return i >= 0 && i < slotted.Length ? slotted[i] : null;
        }

        /// <summary>Equipped, not disabled, and for actives, not Silenced.</summary>
        public bool IsSlotUsable(SlotType type, int index)
        {
            return entity != null && index < entity.Slots.SlotsPerType && entity.Slots.IsUsable(type, index)
                && !(type == SlotType.Active && entity.Statuses.IsSilenced);
        }

        /// <summary>Where the entity is aiming now. Whoever controls it (input, AI) sets it every frame.</summary>
        public void SetAim(ModuleAim newAim)
        {
            aim = newAim;
            if (context != null)
            {
                context.Aim = newAim;
            }
        }

        /// <summary>For entities spawned in code (summons): set before the object activates.</summary>
        public void SetWorld(ModuleWorld newWorld)
        {
            world = newWorld;
        }

        public bool TryKick(ModuleAim aim)
        {
            return isActiveAndEnabled && Kick != null && Kick.TryActivate(aim, true);
        }

        /// <summary>Uses an active or gear active in its slot (Power 1-4, Gear 1-4).</summary>
        public bool TryActivateSlot(SlotType type, int index, ModuleAim aim)
        {
            ModuleBehaviour module = GetSlotted(type, index);
            return isActiveAndEnabled && module != null && module.Activation != null
                && module.TryActivate(aim, IsSlotUsable(type, index));
        }

        private void Awake()
        {
            entityBehaviour = GetComponent<EntityBehaviour>();
            body = GetComponent<Rigidbody>();
            if (world == null)
            {
                Debug.LogError($"{name}: {nameof(EntityModules)} needs the scene's Module World.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            // The entity may run Awake before or after this, so build now and on every (re)creation.
            entityBehaviour.OnEntityCreated += Build;
            if (entityBehaviour.Entity != null)
            {
                Build(entityBehaviour.Entity);
            }
        }

        private void OnDisable()
        {
            entityBehaviour.OnEntityCreated -= Build;
            Teardown();
        }

        private void Update()
        {
            if (entity == null || entityBehaviour.ClockDriver == null)
            {
                return;
            }

            // Clamped like Time.deltaTime, so a hitch doesn't skip a whole cooldown at once.
            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            float cooldownDelta = entityBehaviour.ClockDriver.Clock.CooldownDelta(realDelta);
            float gameDelta = entityBehaviour.ClockDriver.Clock.GameDelta(realDelta, entity.IsExemptFromTimePowers);
            for (int i = 0; i < all.Count; i++)
            {
                ModuleBehaviour module = all[i];
                if (module.Activation != null)
                {
                    module.Activation.Tick(cooldownDelta);
                }

                if (module.IsEnabled && !entity.IsIncapacitated)
                {
                    module.Tick(gameDelta);
                }
            }
        }

        private void Build(Entity newEntity)
        {
            Teardown();
            entity = newEntity;
            context = new ModuleContext(entityBehaviour, entity, world, body,
                GetComponent<EntityMotor>(), meleeArcHalfAngle) { Aim = aim };
            world.Register(entity, entityBehaviour);

            if (world.Database.Modules.TryGet(DataIds.Modules.Kick, out ModuleData kickData))
            {
                Kick = Create(kickData, context);
            }

            ModuleSlots slots = entity.Slots;
            slotted = new ModuleBehaviour[SlotTypeCount * slots.SlotsPerType];
            for (int type = 0; type < SlotTypeCount; type++)
            {
                for (int index = 0; index < slots.SlotsPerType; index++)
                {
                    if (slots.IsEquipped((SlotType)type, index))
                    {
                        slotted[SlotIndex((SlotType)type, index)] = Create(slots.Get((SlotType)type, index), context);
                    }
                }
            }

            entity.OnDamaged += ForwardDamageTaken;
            entity.OnHitDealt += ForwardHitDealt;
            entity.OnDefianceProc += ForwardDefianceProc;
            entity.OnDefeatedTarget += ForwardDefeatedTarget;
            slots.OnSlotsChanged += RefreshEnabled;
            RefreshEnabled();
        }

        private ModuleBehaviour Create(ModuleData data, ModuleContext moduleContext)
        {
            if (!ModuleCatalog.TryCreate(data, world.Database, out ModuleBehaviour module, out string problem))
            {
                // Once per module: kits include modules built in later milestones.
                if (reportedGaps.Add(data.Id))
                {
                    Debug.LogWarning($"{name}: {data.Id} doesn't work yet: {problem}.", this);
                }

                return null;
            }

            module.Attach(moduleContext);
            all.Add(module);
            return module;
        }

        private void RefreshEnabled()
        {
            if (Kick != null)
            {
                Kick.SetEnabled(true);
            }

            for (int type = 0; type < SlotTypeCount; type++)
            {
                for (int index = 0; index < entity.Slots.SlotsPerType; index++)
                {
                    ModuleBehaviour module = slotted[SlotIndex((SlotType)type, index)];
                    if (module != null)
                    {
                        module.SetEnabled(entity.Slots.IsUsable((SlotType)type, index));
                    }
                }
            }
        }

        private void Teardown()
        {
            if (entity != null)
            {
                if (world != null)
                {
                    world.Unregister(entity);
                }

                entity.OnDamaged -= ForwardDamageTaken;
                entity.OnHitDealt -= ForwardHitDealt;
                entity.OnDefianceProc -= ForwardDefianceProc;
                entity.OnDefeatedTarget -= ForwardDefeatedTarget;
                entity.Slots.OnSlotsChanged -= RefreshEnabled;
            }

            foreach (ModuleBehaviour module in all)
            {
                module.Detach();
            }

            all.Clear();
            slotted = new ModuleBehaviour[0];
            Kick = null;
            entity = null;
            context = null;
        }

        private void ForwardDamageTaken(DamageEvent damage)
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].IsEnabled)
                {
                    all[i].OnDamageTaken(damage);
                }
            }
        }

        private void ForwardHitDealt(DamageEvent hit)
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].IsEnabled)
                {
                    all[i].OnHitDealt(hit);
                }
            }
        }

        private void ForwardDefianceProc()
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].IsEnabled)
                {
                    all[i].OnDefianceProc();
                }
            }
        }

        private void ForwardDefeatedTarget(Entity target)
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].IsEnabled)
                {
                    all[i].OnDefeatedTarget(target);
                }
            }
        }

        private int SlotIndex(SlotType type, int index)
        {
            return entity == null ? -1 : (int)type * entity.Slots.SlotsPerType + index;
        }
    }
}
