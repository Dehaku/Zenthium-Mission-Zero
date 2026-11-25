using UnityEngine;
using Zenthium.Combat;
using Zenthium.Entities;
using Zenthium.Statuses;

namespace Zenthium.Modules
{
    /// <summary>
    /// Deals this entity's damage-over-time ticks, such as Burn, through the damage pipeline as DoT hits: Armor and
    /// Damage Reduction don't apply, nobody dodges them, and a dragged entity takes nothing (statuses.md).
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour), typeof(EntityModules))]
    public sealed class DamageOverTimeTicks : MonoBehaviour
    {
        private EntityBehaviour entityBehaviour;
        private EntityModules modules;
        private Entity entity;

        private void Awake()
        {
            entityBehaviour = GetComponent<EntityBehaviour>();
            modules = GetComponent<EntityModules>();
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
            entity.Statuses.OnDamageOverTimeTick += DealTick;
        }

        private void Unbind()
        {
            if (entity != null)
            {
                entity.Statuses.OnDamageOverTimeTick -= DealTick;
                entity = null;
            }
        }

        private void DealTick(DamageOverTimeTick tick)
        {
            ModuleWorld world = modules.World;
            if (entity == null || world == null || world.CombatRules.Pipeline == null)
            {
                return;
            }

            world.CombatRules.Pipeline.Apply(new Hit(tick.Source, tick.Damage, tick.DamageType, isDamageOverTime: true),
                entity);
        }
    }
}
